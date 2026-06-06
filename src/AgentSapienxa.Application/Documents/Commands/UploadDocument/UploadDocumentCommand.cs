using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.UploadDocument;

public record UploadDocumentCommand(
    Guid CompanyId,
    string FileName,
    long FileSize,
    string ContentType,
    Stream FileContent,
    Guid? UploadedBy
) : IRequest<UploadDocumentResult>;

public record UploadDocumentResult(Guid Id, string Status);
