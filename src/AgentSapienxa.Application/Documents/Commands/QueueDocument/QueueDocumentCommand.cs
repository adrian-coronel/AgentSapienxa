using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.QueueDocument;

public record QueueDocumentCommand(Guid DocumentId, Guid CompanyId) : IRequest<bool>;
