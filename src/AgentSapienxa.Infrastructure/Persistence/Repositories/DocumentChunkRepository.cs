using AgentSapienxa.Application.Documents.Repositories;
using AgentSapienxa.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly ApplicationDbContext _db;

    public DocumentChunkRepository(ApplicationDbContext db) => _db = db;

    public async Task AddBatchAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken ct = default)
    {
        _db.DocumentChunks.AddRange(chunks);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteByDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        await _db.DocumentChunks.IgnoreQueryFilters()
            .Where(c => c.DocumentId == documentId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<ChunkSearchResult>> SearchByVectorAsync(
        Guid companyId, float[] queryVector, int topK = 5, CancellationToken ct = default)
    {
        var vectorLiteral = "[" + string.Join(",", queryVector) + "]";

        var sql = $@"
            SELECT
                id               AS ""Id"",
                document_id      AS ""DocumentId"",
                company_id       AS ""CompanyId"",
                chunk_index      AS ""ChunkIndex"",
                header_path      AS ""HeaderPath"",
                content          AS ""Content"",
                token_count      AS ""TokenCount"",
                1 - (embedding <=> '{vectorLiteral}'::vector) AS ""Similarity""
            FROM document_chunks
            WHERE company_id = @companyId
            ORDER BY embedding <=> '{vectorLiteral}'::vector
            LIMIT @topK";

        var companyParam = new NpgsqlParameter("companyId", companyId);
        var topKParam = new NpgsqlParameter("topK", topK);

        var rows = await _db.Database
            .SqlQueryRaw<ChunkSearchRow>(sql, companyParam, topKParam)
            .ToListAsync(ct);

        return rows.Select(r => new ChunkSearchResult(
            r.Id, r.DocumentId, r.HeaderPath, r.Content, r.Similarity)).ToList();
    }

    // Projection type for raw SQL
    private record ChunkSearchRow(
        Guid Id,
        Guid DocumentId,
        Guid CompanyId,
        int ChunkIndex,
        string? HeaderPath,
        string Content,
        int TokenCount,
        double Similarity);
}
