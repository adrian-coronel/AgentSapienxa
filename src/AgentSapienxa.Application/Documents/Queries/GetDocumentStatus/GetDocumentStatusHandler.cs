using AgentSapienxa.Application.Documents.Repositories;
using MediatR;

namespace AgentSapienxa.Application.Documents.Queries.GetDocumentStatus;

public class GetDocumentStatusHandler : IRequestHandler<GetDocumentStatusQuery, DocumentStatusResult?>
{
    private readonly IDocumentUploadRepository _uploads;

    public GetDocumentStatusHandler(IDocumentUploadRepository uploads) => _uploads = uploads;

    public async Task<DocumentStatusResult?> Handle(GetDocumentStatusQuery query, CancellationToken ct)
    {
        var doc = await _uploads.GetByIdAsync(query.DocumentId, ct);
        if (doc is null || doc.CompanyId != query.CompanyId)
            return null;

        return new DocumentStatusResult(
            doc.Id,
            doc.FileName,
            doc.Status,
            doc.ProgressPct,
            doc.ErrorMessage,
            doc.ChunkCount,
            doc.TotalTokens,
            doc.CreatedAt,
            doc.CompletedAt);
    }
}
