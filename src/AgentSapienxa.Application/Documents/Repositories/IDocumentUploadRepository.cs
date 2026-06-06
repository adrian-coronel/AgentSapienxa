using AgentSapienxa.Domain.Documents;

namespace AgentSapienxa.Application.Documents.Repositories;

public interface IDocumentUploadRepository
{
    Task<DocumentUpload?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DocumentUpload?> GetByCompanyAndSha256Async(Guid companyId, string sha256, CancellationToken ct = default);
    Task<DocumentUpload?> GetNextQueuedAsync(CancellationToken ct = default);
    Task<int> QueueAllPendingByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task ResetStuckDocumentsAsync(CancellationToken ct = default);
    Task<List<DocumentUpload>> GetByCompanyAsync(Guid companyId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task AddAsync(DocumentUpload upload, CancellationToken ct = default);
    Task UpdateAsync(DocumentUpload upload, CancellationToken ct = default);
    Task DeleteAsync(DocumentUpload upload, CancellationToken ct = default);
}
