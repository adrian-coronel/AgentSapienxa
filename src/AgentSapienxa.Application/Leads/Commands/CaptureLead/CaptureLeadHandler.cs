using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Domain.Leads;
using MediatR;

namespace AgentSapienxa.Application.Leads.Commands.CaptureLead;

public class CaptureLeadHandler : IRequestHandler<CaptureLeadCommand, CaptureLeadResult>
{
    private readonly ILeadRepository _leads;
    private readonly ISalesAgentRepository _agents;

    public CaptureLeadHandler(ILeadRepository leads, ISalesAgentRepository agents)
    {
        _leads = leads;
        _agents = agents;
    }

    public async Task<CaptureLeadResult> Handle(CaptureLeadCommand cmd, CancellationToken ct)
    {
        var existing = await _leads.GetByPhoneNumberAsync(cmd.PhoneNumber, ct);
        if (existing is not null)
        {
            existing.UpdateInfo(cmd.Name, cmd.Email);
            await _leads.UpdateAsync(existing, ct);
            return new CaptureLeadResult(existing.Id, false, "Lead actualizado correctamente.");
        }

        var lead = Lead.Create(cmd.PhoneNumber, cmd.Name, cmd.Email, cmd.ContactMethod);

        var agent = await _agents.GetFirstAvailableAsync(ct);
        if (agent is not null)
            lead.AssignAgent(agent.Id);

        await _leads.AddAsync(lead, ct);
        return new CaptureLeadResult(lead.Id, true, "Lead capturado correctamente.");
    }
}
