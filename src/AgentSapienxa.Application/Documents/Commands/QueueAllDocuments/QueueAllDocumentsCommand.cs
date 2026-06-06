using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.QueueAllDocuments;

public record QueueAllDocumentsCommand(Guid CompanyId) : IRequest<int>;
