using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.PauseDocument;

public record PauseDocumentCommand(Guid DocumentId, Guid CompanyId) : IRequest<bool>;
