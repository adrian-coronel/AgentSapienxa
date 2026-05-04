using MediatR;

namespace AgentSapienxa.Application.Payments.Commands.ResolvePaymentValidation;

public record ResolvePaymentValidationCommand(
    Guid ValidationId,
    string Decision,
    string ResolvedBy,
    string? Observation) : IRequest<ResolvePaymentValidationResult>;

public record ResolvePaymentValidationResult(string EnrollmentStatus, string Message);
