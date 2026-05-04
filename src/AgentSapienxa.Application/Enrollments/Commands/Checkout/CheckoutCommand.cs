using MediatR;

namespace AgentSapienxa.Application.Enrollments.Commands.Checkout;

public record CheckoutCommand(
    string PhoneNumber,
    Guid CatalogItemId,
    string PaymentMethodName) : IRequest<CheckoutResult>;

public record CheckoutResult(
    string CourseTitle,
    decimal Amount,
    string PaymentMethodName,
    string? PaymentMethodDescription,
    string? PaymentMethodImage);
