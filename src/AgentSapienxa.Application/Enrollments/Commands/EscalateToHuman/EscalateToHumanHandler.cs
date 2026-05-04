using AgentSapienxa.Application.Enrollments.Repositories;
using AgentSapienxa.Application.Leads.Repositories;
using MediatR;

namespace AgentSapienxa.Application.Enrollments.Commands.EscalateToHuman;

public class EscalateToHumanHandler : IRequestHandler<EscalateToHumanCommand, EscalateToHumanResult>
{
    private readonly ILeadRepository _leads;
    private readonly IEnrollmentRepository _enrollments;
    private readonly ISalesAgentRepository _agents;

    public EscalateToHumanHandler(ILeadRepository leads, IEnrollmentRepository enrollments, ISalesAgentRepository agents)
    {
        _leads = leads;
        _enrollments = enrollments;
        _agents = agents;
    }

    public async Task<EscalateToHumanResult> Handle(EscalateToHumanCommand cmd, CancellationToken ct)
    {
        var lead = await _leads.GetByPhoneNumberAsync(cmd.PhoneNumber, ct)
            ?? throw new KeyNotFoundException("Lead no encontrado.");

        var enrollment = await _enrollments.GetByIdAsync(cmd.EnrollmentId, ct)
            ?? throw new KeyNotFoundException("Enrollment no encontrado.");

        var agentId = lead.SalesAgentId;
        if (agentId is null)
        {
            var agent = await _agents.GetFirstAvailableAsync(ct)
                ?? throw new InvalidOperationException("No hay agentes de ventas disponibles.");
            agentId = agent.Id;
            lead.AssignAgent(agent.Id);
            await _leads.UpdateAsync(lead, ct);
        }

        enrollment.Escalate(agentId.Value, cmd.Observation);
        await _enrollments.UpdateAsync(enrollment, ct);

        var salesAgent = await _agents.GetByIdAsync(agentId.Value, ct);
        return new EscalateToHumanResult("Escalado a agente humano correctamente.", salesAgent?.Email);
    }
}
