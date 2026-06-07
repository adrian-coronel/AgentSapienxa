using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace AgentSapienxa.Infrastructure.Documents.Chunking;

public class HybridSemanticChunker : ISemanticChunker
{
    private readonly IEmbeddingProvider _embedding;
    private readonly ILogger<HybridSemanticChunker> _logger;
    private const int MaxTokensPerChunk = 800;
    private const int TargetTokensPerChunk = 600;
    private const float SimilarityThreshold = 0.6f;
    private const float OverlapRatio = 0.15f;

    public HybridSemanticChunker(IEmbeddingProvider embedding, ILogger<HybridSemanticChunker> logger)
    {
        _embedding = embedding;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChunkDraft>> ChunkAsync(string markdown, CancellationToken ct = default)
    {
        var sections = SplitByHeaders(markdown);
        _logger.LogInformation("[Chunker] {Count} sections detected", sections.Count);
        var result = new List<ChunkDraft>();

        for (int si = 0; si < sections.Count; si++)
        {
            var (headerPath, content) = sections[si];
            var tokens = EstimateTokens(content);
            _logger.LogInformation("[Chunker] Section {N}/{Total} — '{Header}' — {Tokens} tokens",
                si + 1, sections.Count, string.IsNullOrEmpty(headerPath) ? "(sin header)" : headerPath, tokens);

            if (tokens <= MaxTokensPerChunk)
            {
                result.Add(new ChunkDraft(content.Trim(), headerPath, tokens));
                continue;
            }

            // Large section: split by paragraphs with embedding-based boundary detection
            var paragraphs = content
                .Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (paragraphs.Count <= 1)
            {
                // Single huge paragraph — hard split by sentences
                _logger.LogInformation("[Chunker] Section {N} — single large paragraph, hard-splitting by sentences", si + 1);
                var subChunks = HardSplitBySentences(content, headerPath, MaxTokensPerChunk);
                result.AddRange(subChunks);
                continue;
            }

            _logger.LogInformation("[Chunker] Section {N} — {Paragraphs} paragraphs, calling embedding API for semantic boundaries...",
                si + 1, paragraphs.Count);
            var subChunksFromParagraphs = await SplitParagraphsBySemanticBoundariesAsync(
                paragraphs, headerPath, ct);
            _logger.LogInformation("[Chunker] Section {N} — semantic split done, {Chunks} sub-chunks", si + 1, subChunksFromParagraphs.Count);
            result.AddRange(subChunksFromParagraphs);
        }

        return result.Where(c => !string.IsNullOrWhiteSpace(c.Content)).ToList();
    }

    private static List<(string HeaderPath, string Content)> SplitByHeaders(string markdown)
    {
        var sections = new List<(string HeaderPath, string Content)>();
        var headerPattern = new Regex(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline);
        var matches = headerPattern.Matches(markdown);

        if (matches.Count == 0)
        {
            sections.Add((string.Empty, markdown));
            return sections;
        }

        var headerStack = new Stack<(int Level, string Title)>();

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            int level = match.Groups[1].Value.Length;
            string title = match.Groups[2].Value.Trim();

            // Build header path
            while (headerStack.Count > 0 && headerStack.Peek().Level >= level)
                headerStack.Pop();
            headerStack.Push((level, title));

            var headerPath = string.Join(" > ", headerStack.Reverse().Select(h => h.Title));

            int contentStart = match.Index + match.Length;
            int contentEnd = i + 1 < matches.Count ? matches[i + 1].Index : markdown.Length;
            string content = markdown[contentStart..contentEnd].Trim();

            if (!string.IsNullOrWhiteSpace(content))
                sections.Add((headerPath, content));
        }

        // Content before first header
        if (matches.Count > 0 && matches[0].Index > 0)
        {
            var preHeader = markdown[..matches[0].Index].Trim();
            if (!string.IsNullOrWhiteSpace(preHeader))
                sections.Insert(0, (string.Empty, preHeader));
        }

        return sections;
    }

    private async Task<List<ChunkDraft>> SplitParagraphsBySemanticBoundariesAsync(
        List<string> paragraphs,
        string headerPath,
        CancellationToken ct)
    {
        // Embed each paragraph
        var embeddings = await _embedding.EmbedBatchAsync(paragraphs, ct);

        var chunks = new List<ChunkDraft>();
        var currentChunk = new StringBuilder();
        int currentTokens = 0;
        float[]? prevEmbedding = null;

        for (int i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            int paraTokens = EstimateTokens(paragraph);
            var curEmbedding = embeddings[i];

            bool semanticBreak = prevEmbedding is not null &&
                CosineSimilarity(prevEmbedding, curEmbedding) < SimilarityThreshold;

            bool sizeBreak = currentTokens + paraTokens > TargetTokensPerChunk;

            if ((semanticBreak || sizeBreak) && currentChunk.Length > 0)
            {
                chunks.Add(new ChunkDraft(currentChunk.ToString().Trim(), headerPath, currentTokens));

                // Overlap: keep last N tokens worth of content
                int overlapTokens = (int)(currentTokens * OverlapRatio);
                var overlapText = GetLastNTokens(currentChunk.ToString(), overlapTokens);
                currentChunk.Clear();
                if (!string.IsNullOrWhiteSpace(overlapText))
                {
                    currentChunk.Append(overlapText);
                    currentTokens = EstimateTokens(overlapText);
                }
                else
                {
                    currentTokens = 0;
                }
            }

            if (currentChunk.Length > 0) currentChunk.AppendLine();
            currentChunk.Append(paragraph);
            currentTokens += paraTokens;
            prevEmbedding = curEmbedding;
        }

        if (currentChunk.Length > 0)
            chunks.Add(new ChunkDraft(currentChunk.ToString().Trim(), headerPath, currentTokens));

        return chunks;
    }

    private static List<ChunkDraft> HardSplitBySentences(string content, string headerPath, int maxTokens)
    {
        var sentences = Regex.Split(content, @"(?<=[.!?])\s+");
        var chunks = new List<ChunkDraft>();
        var current = new StringBuilder();
        int tokens = 0;

        foreach (var sentence in sentences)
        {
            int sentTokens = EstimateTokens(sentence);
            if (tokens + sentTokens > maxTokens && current.Length > 0)
            {
                chunks.Add(new ChunkDraft(current.ToString().Trim(), headerPath, tokens));
                current.Clear();
                tokens = 0;
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(sentence);
            tokens += sentTokens;
        }

        if (current.Length > 0)
            chunks.Add(new ChunkDraft(current.ToString().Trim(), headerPath, tokens));

        return chunks;
    }

    private static int EstimateTokens(string text) => Math.Max(1, text.Length / 4);

    private static string GetLastNTokens(string text, int approxTokens)
    {
        int chars = approxTokens * 4;
        if (chars >= text.Length) return text;
        return text[^chars..];
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length && i < b.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return (normA == 0 || normB == 0) ? 0f : dot / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
