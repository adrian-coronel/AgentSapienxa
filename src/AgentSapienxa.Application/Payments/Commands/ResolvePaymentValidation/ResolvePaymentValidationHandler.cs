using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Enrollments.Repositories;
using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Application.Payments.Repositories;
using AgentSapienxa.Domain.Enrollments;
using AgentSapienxa.Domain.Payments;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgentSapienxa.Application.Payments.Commands.ResolvePaymentValidation;

public class ResolvePaymentValidationHandler : IRequestHandler<ResolvePaymentValidationCommand, ResolvePaymentValidationResult>
{
    private readonly IPaymentValidationRepository _validations;
    private readonly IEnrollmentRepository _enrollments;
    private readonly ILeadRepository _leads;
    private readonly IMessagingChannel _messaging;
    private readonly ILogger<ResolvePaymentValidationHandler> _logger;

    public ResolvePaymentValidationHandler(
        IPaymentValidationRepository validations,
        IEnrollmentRepository enrollments,
        ILeadRepository leads,
        IMessagingChannel messaging,
        ILogger<ResolvePaymentValidationHandler> logger)
    {
        _validations = validations;
        _enrollments = enrollments;
        _leads = leads;
        _messaging = messaging;
        _logger = logger;
    }

    public async Task<ResolvePaymentValidationResult> Handle(ResolvePaymentValidationCommand cmd, CancellationToken ct)
    {
        var validation = await _validations.GetByIdAsync(cmd.ValidationId, ct)
            ?? throw new KeyNotFoundException("Validación no encontrada.");

        var result = validation.Resolve(cmd.Decision, cmd.ResolvedBy, cmd.Observation);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error);

        await _validations.UpdateAsync(validation, ct);

        var enrollment = await _enrollments.GetByIdAsync(validation.EnrollmentId, ct)!;

        if (cmd.Decision == PaymentValidationStatus.Aprobado)
        {
            enrollment!.Transition(EnrollmentStatus.Pagado);
            await _enrollments.UpdateAsync(enrollment, ct);

            var lead = await _leads.GetByIdAsync(enrollment.LeadId, ct);
            if (lead is not null)
                await _messaging.SendTextAsync(lead.PhoneNumber, "¡Tu pago fue aprobado! Ya estás inscrito en el curso. 🎉", ct);
        }
        else
        {
            var lead = enrollment is not null ? await _leads.GetByIdAsync(enrollment.LeadId, ct) : null;
            if (lead is not null)
                await _messaging.SendTextAsync(lead.PhoneNumber,
                    "Tu voucher de pago fue revisado y no pudo ser validado. Por favor envía un comprobante válido o contacta a soporte.", ct);
        }

        _logger.LogInformation("Validación {ValidationId} resuelta como {Decision} por {ResolvedBy}", cmd.ValidationId, cmd.Decision, cmd.ResolvedBy);

        return new ResolvePaymentValidationResult(enrollment?.Status ?? "Desconocido", $"Validación resuelta: {cmd.Decision}");
    }
}
