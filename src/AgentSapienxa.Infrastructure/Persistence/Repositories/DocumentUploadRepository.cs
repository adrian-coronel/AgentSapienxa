using AgentSapienxa.Application.Documents.Repositories;
using AgentSapienxa.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class DocumentUploadRepository : IDocumentUploadRepository
{
    private readonly ApplicationDbContext _db;

    public DocumentUploadRepository(ApplicationDbContext db) => _db = db;

    public Task<DocumentUpload?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.DocumentUploads.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<DocumentUpload?> GetByCompanyAndSha256Async(Guid companyId, string sha256, CancellationToken ct = default) =>
        _db.DocumentUploads.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.CompanyId == companyId && d.Sha256 == sha256, ct);

    public Task<DocumentUpload?> GetNextQueuedAsync(CancellationToken ct = default) =>
        _db.DocumentUploads
            .IgnoreQueryFilters()
            .Where(d => d.Status == DocumentStatus.Queued)
            .OrderBy(d => d.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task ResetStuckDocumentsAsync(CancellationToken ct = default)
    {
        // Docs que quedaron a medias por un restart: los vuelve a Pending para que el usuario los procese manualmente
        var stuckStatuses = new[] { DocumentStatus.Queued, DocumentStatus.Parsing, DocumentStatus.Chunking, DocumentStatus.Embedding };
        await _db.DocumentUploads.IgnoreQueryFilters()
            .Where(d => stuckStatuses.Contains(d.Status))
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, DocumentStatus.Pending)
                .SetProperty(d => d.ProgressPct, 0)
                .SetProperty(d => d.StartedAt, (DateTime?)null),
                ct);
    }

    public async Task<int> QueueAllPendingByCompanyAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _db.DocumentUploads
            .IgnoreQueryFilters()
            .Where(d => d.CompanyId == companyId &&
                        (d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.Failed || d.Status == DocumentStatus.Paused))
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, DocumentStatus.Queued)
                .SetProperty(d => d.ProgressPct, 0)
                .SetProperty(d => d.RetryCount, 0)
                .SetProperty(d => d.ErrorMessage, (string?)null)
                .SetProperty(d => d.StartedAt, (DateTime?)null),
                ct);
    }

    public Task<List<DocumentUpload>> GetByCompanyAsync(Guid companyId, int page, int pageSize, CancellationToken ct = default) =>
        _db.DocumentUploads.IgnoreQueryFilters()
            .Where(d => d.CompanyId == companyId)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        _db.DocumentUploads.IgnoreQueryFilters()
            .CountAsync(d => d.CompanyId == companyId, ct);

    public async Task AddAsync(DocumentUpload upload, CancellationToken ct = default)
    {
        _db.DocumentUploads.Add(upload);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DocumentUpload upload, CancellationToken ct = default)
    {
        _db.DocumentUploads.Update(upload);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(DocumentUpload upload, CancellationToken ct = default)
    {
        _db.DocumentUploads.Remove(upload);
        await _db.SaveChangesAsync(ct);
    }
}
