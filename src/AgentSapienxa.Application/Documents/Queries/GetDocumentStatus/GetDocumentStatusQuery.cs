using MediatR;

namespace AgentSapienxa.Application.Documents.Queries.GetDocumentStatus;

public record GetDocumentStatusQuery(Guid DocumentId, Guid CompanyId) : IRequest<DocumentStatusResult?>;

public record DocumentStatusResult(
    Guid Id,
    string FileName,
    string Status,
    int ProgressPct,
    string? ErrorMessage,
    int? ChunkCount,
    int? TotalTokens,
    DateTime CreatedAt,
    DateTime? CompletedAt);
