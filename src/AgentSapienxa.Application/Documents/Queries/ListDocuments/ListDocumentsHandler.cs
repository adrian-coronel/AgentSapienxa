using AgentSapienxa.Application.Documents.Repositories;
using MediatR;

namespace AgentSapienxa.Application.Documents.Queries.ListDocuments;

public class ListDocumentsHandler : IRequestHandler<ListDocumentsQuery, ListDocumentsResult>
{
    private readonly IDocumentUploadRepository _uploads;

    public ListDocumentsHandler(IDocumentUploadRepository uploads) => _uploads = uploads;

    public async Task<ListDocumentsResult> Handle(ListDocumentsQuery query, CancellationToken ct)
    {
        var items = await _uploads.GetByCompanyAsync(query.CompanyId, query.Page, query.PageSize, ct);
        var total = await _uploads.CountByCompanyAsync(query.CompanyId, ct);

        var summaries = items.Select(d => new DocumentSummary(
            d.Id, d.FileName, d.FileSize, d.ContentType,
            d.Status, d.ProgressPct, d.ErrorMessage, d.ChunkCount, d.CreatedAt, d.CompletedAt
        )).ToList();

        return new ListDocumentsResult(summaries, total, query.Page, query.PageSize);
    }
}
