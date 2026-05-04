using MediatR;

namespace AgentSapienxa.Application.Webhooks;

public record ProcessIncomingMessageCommand(IncomingMessage Message) : IRequest<Unit>;
