using AgentSapienxa.Application.Catalog.Repositories;
using AgentSapienxa.Application.Enrollments.Repositories;
using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Application.Payments.Repositories;
using AgentSapienxa.Domain.Enrollments;
using MediatR;

namespace AgentSapienxa.Application.Enrollments.Commands.Checkout;

public class CheckoutHandler : IRequestHandler<CheckoutCommand, CheckoutResult>
{
    private readonly ILeadRepository _leads;
    private readonly IEnrollmentRepository _enrollments;
    private readonly ICatalogRepository _catalog;
    private readonly IPaymentMethodRepository _paymentMethods;

    public CheckoutHandler(
        ILeadRepository leads,
        IEnrollmentRepository enrollments,
        ICatalogRepository catalog,
        IPaymentMethodRepository paymentMethods)
    {
        _leads = leads;
        _enrollments = enrollments;
        _catalog = catalog;
        _paymentMethods = paymentMethods;
    }

    public async Task<CheckoutResult> Handle(CheckoutCommand cmd, CancellationToken ct)
    {
        var lead = await _leads.GetByPhoneNumberAsync(cmd.PhoneNumber, ct)
            ?? throw new KeyNotFoundException("Lead no encontrado.");

        var enrollment = await _enrollments.GetActiveAsync(lead.Id, cmd.CatalogItemId, ct)
            ?? throw new KeyNotFoundException("No tienes una inscripción activa para este curso.");

        if (!EnrollmentStatus.CanTransitionTo(enrollment.Status, EnrollmentStatus.PendientePago))
            throw new InvalidOperationException($"El enrollment en estado '{enrollment.Status}' no puede pasar a 'Pendiente Pago'.");

        var item = await _catalog.GetByIdAsync(cmd.CatalogItemId, ct)
            ?? throw new KeyNotFoundException("Curso no encontrado.");

        var method = await _paymentMethods.GetByNameAsync(cmd.PaymentMethodName, ct)
            ?? throw new KeyNotFoundException($"Método de pago '{cmd.PaymentMethodName}' no encontrado.");

        if (item.Cost > method.LimitAmount)
            throw new InvalidOperationException(
                $"El costo del curso ({item.Cost:C}) supera el límite del método {method.Name} ({method.LimitAmount:C}). Usa otro método de pago.");

        enrollment.SetPayment(method.Id, item.Cost);
        enrollment.Transition(EnrollmentStatus.PendientePago);
        await _enrollments.UpdateAsync(enrollment, ct);

        return new CheckoutResult(item.Title, item.Cost, method.Name, method.Description, method.Image);
    }
}
