namespace AgentSapienxa.Application.Common.Abstractions;

public record ChunkDraft(string Content, string? HeaderPath, int TokenCount);

public interface ISemanticChunker
{
    Task<IReadOnlyList<ChunkDraft>> ChunkAsync(string markdown, CancellationToken ct = default);
}
