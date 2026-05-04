using MediatR;

namespace AgentSapienxa.Application.Enrollments.Commands.EscalateToHuman;

public record EscalateToHumanCommand(
    string PhoneNumber,
    Guid EnrollmentId,
    string? Observation) : IRequest<EscalateToHumanResult>;

public record EscalateToHumanResult(string Message, string? SalesAgentEmail);
