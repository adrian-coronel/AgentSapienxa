using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.DeleteDocument;

public record DeleteDocumentCommand(Guid DocumentId, Guid CompanyId) : IRequest;
