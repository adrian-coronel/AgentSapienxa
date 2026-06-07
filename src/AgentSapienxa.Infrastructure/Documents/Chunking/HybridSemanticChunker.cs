using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace AgentSapienxa.Infrastructure.Documents.Chunking;

public class HybridSemanticChunker : ISemanticChunker
{
    private readonly ILogger<HybridSemanticChunker> _logger;
    private const int MaxTokensPerChunk = 800;
    private const int TargetTokensPerChunk = 600;

    public HybridSemanticChunker(ILogger<HybridSemanticChunker> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<ChunkDraft>> ChunkAsync(string markdown, CancellationToken ct = default)
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

            var paragraphs = content
                .Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (paragraphs.Count <= 1)
            {
                _logger.LogInformation("[Chunker] Section {N} — single large paragraph, hard-splitting by sentences", si + 1);
                result.AddRange(HardSplitBySentences(content, headerPath, MaxTokensPerChunk));
                continue;
            }

            _logger.LogInformation("[Chunker] Section {N} — {Paragraphs} paragraphs, grouping by token size", si + 1, paragraphs.Count);
            result.AddRange(GroupParagraphsByTokens(paragraphs, headerPath));
        }

        IReadOnlyList<ChunkDraft> final = result.Where(c => !string.IsNullOrWhiteSpace(c.Content)).ToList();
        _logger.LogInformation("[Chunker] Total chunks produced: {Count}", final.Count);
        return Task.FromResult(final);
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

        if (matches.Count > 0 && matches[0].Index > 0)
        {
            var preHeader = markdown[..matches[0].Index].Trim();
            if (!string.IsNullOrWhiteSpace(preHeader))
                sections.Insert(0, (string.Empty, preHeader));
        }

        return sections;
    }

    private static List<ChunkDraft> GroupParagraphsByTokens(List<string> paragraphs, string headerPath)
    {
        var chunks = new List<ChunkDraft>();
        var current = new StringBuilder();
        int currentTokens = 0;

        foreach (var paragraph in paragraphs)
        {
            int paraTokens = EstimateTokens(paragraph);

            if (currentTokens + paraTokens > TargetTokensPerChunk && current.Length > 0)
            {
                chunks.Add(new ChunkDraft(current.ToString().Trim(), headerPath, currentTokens));
                current.Clear();
                currentTokens = 0;
            }

            if (current.Length > 0) current.AppendLine();
            current.Append(paragraph);
            currentTokens += paraTokens;
        }

        if (current.Length > 0)
            chunks.Add(new ChunkDraft(current.ToString().Trim(), headerPath, currentTokens));

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
}
