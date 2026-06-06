using MediatR;

namespace AgentSapienxa.Application.Documents.Commands.ResumeDocument;

public record ResumeDocumentCommand(Guid DocumentId, Guid CompanyId) : IRequest<bool>;
