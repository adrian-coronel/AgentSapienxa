using AgentSapienxa.Application.Enrollments.Repositories;
using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Application.Payments.Repositories;
using AgentSapienxa.Domain.Payments;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgentSapienxa.Application.Payments.Commands.RequestPaymentValidation;

public class RequestPaymentValidationHandler : IRequestHandler<RequestPaymentValidationCommand, RequestPaymentValidationResult>
{
    private readonly ILeadRepository _leads;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IPaymentValidationRepository _validations;
    private readonly ILogger<RequestPaymentValidationHandler> _logger;

    public RequestPaymentValidationHandler(
        ILeadRepository leads,
        IEnrollmentRepository enrollments,
        IPaymentValidationRepository validations,
        ILogger<RequestPaymentValidationHandler> logger)
    {
        _leads = leads;
        _enrollments = enrollments;
        _validations = validations;
        _logger = logger;
    }

    public async Task<RequestPaymentValidationResult> Handle(RequestPaymentValidationCommand cmd, CancellationToken ct)
    {
        var lead = await _leads.GetByPhoneNumberAsync(cmd.PhoneNumber, ct)
            ?? throw new KeyNotFoundException("Lead no encontrado.");

        var enrollment = await _enrollments.GetByIdAsync(cmd.EnrollmentId, ct)
            ?? throw new KeyNotFoundException("Enrollment no encontrado.");

        if (enrollment.LeadId != lead.Id)
            throw new UnauthorizedAccessException("El enrollment no pertenece a este lead.");

        var validation = PaymentValidation.Create(enrollment.Id, cmd.PhoneNumber, cmd.VoucherDetail, cmd.VoucherUrl);
        await _validations.AddAsync(validation, ct);

        _logger.LogInformation(
            "Validación de pago creada {ValidationId} para enrollment {EnrollmentId} de {Phone}",
            validation.Id, enrollment.Id, cmd.PhoneNumber);

        return new RequestPaymentValidationResult(validation.Id, "Validación enviada al equipo de ventas. En breve recibirás una respuesta.");
    }
}
