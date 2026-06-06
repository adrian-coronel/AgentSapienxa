using MediatR;

namespace AgentSapienxa.Application.Documents.Queries.ListDocuments;

public record ListDocumentsQuery(Guid CompanyId, int Page = 1, int PageSize = 20) : IRequest<ListDocumentsResult>;

public record DocumentSummary(
    Guid Id,
    string FileName,
    long FileSize,
    string ContentType,
    string Status,
    int ProgressPct,
    string? ErrorMessage,
    int? ChunkCount,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public record ListDocumentsResult(IReadOnlyList<DocumentSummary> Items, int Total, int Page, int PageSize);
