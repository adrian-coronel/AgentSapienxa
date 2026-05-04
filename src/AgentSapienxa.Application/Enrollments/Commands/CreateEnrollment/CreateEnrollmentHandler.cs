using AgentSapienxa.Application.Enrollments.Repositories;
using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Domain.Enrollments;
using MediatR;

namespace AgentSapienxa.Application.Enrollments.Commands.CreateEnrollment;

public class CreateEnrollmentHandler : IRequestHandler<CreateEnrollmentCommand, CreateEnrollmentResult>
{
    private readonly ILeadRepository _leads;
    private readonly IEnrollmentRepository _enrollments;

    public CreateEnrollmentHandler(ILeadRepository leads, IEnrollmentRepository enrollments)
    {
        _leads = leads;
        _enrollments = enrollments;
    }

    public async Task<CreateEnrollmentResult> Handle(CreateEnrollmentCommand cmd, CancellationToken ct)
    {
        var lead = await _leads.GetByPhoneNumberAsync(cmd.PhoneNumber, ct)
            ?? throw new InvalidOperationException("Lead no encontrado. Usa capture_lead primero.");

        var existing = await _enrollments.GetActiveAsync(lead.Id, cmd.CatalogItemId, ct);
        if (existing is not null)
            return new CreateEnrollmentResult(existing.Id, true, "Ya estás registrado en este curso.");

        var enrollment = Enrollment.Create(lead.Id, cmd.CatalogItemId);
        await _enrollments.AddAsync(enrollment, ct);
        return new CreateEnrollmentResult(enrollment.Id, false, "Inscripción registrada correctamente.");
    }
}
