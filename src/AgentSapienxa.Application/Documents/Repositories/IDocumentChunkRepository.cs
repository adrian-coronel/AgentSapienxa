using AgentSapienxa.Domain.Documents;

namespace AgentSapienxa.Application.Documents.Repositories;

public record ChunkSearchResult(Guid ChunkId, Guid DocumentId, string? HeaderPath, string Content, double Similarity);

public interface IDocumentChunkRepository
{
    Task AddBatchAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken ct = default);
    Task DeleteByDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<IReadOnlyList<ChunkSearchResult>> SearchByVectorAsync(Guid companyId, float[] queryVector, int topK = 5, CancellationToken ct = default);
}
