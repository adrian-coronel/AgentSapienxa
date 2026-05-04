using MediatR;

namespace AgentSapienxa.Application.Payments.Commands.RequestPaymentValidation;

public record RequestPaymentValidationCommand(
    string PhoneNumber,
    Guid EnrollmentId,
    string? VoucherDetail,
    string? VoucherUrl) : IRequest<RequestPaymentValidationResult>;

public record RequestPaymentValidationResult(Guid ValidationId, string Message);
