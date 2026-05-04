using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Enrollments.Commands.EscalateToHuman;
using AgentSapienxa.Application.Enrollments.Repositories;
using AgentSapienxa.Application.Leads.Repositories;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentSapienxa.Application.Agents.Tools;

public class EscalateToHumanTool : IAgentTool
{
    private readonly IMediator _mediator;
    private readonly ILeadRepository _leads;
    private readonly IEnrollmentRepository _enrollments;

    public EscalateToHumanTool(IMediator mediator, ILeadRepository leads, IEnrollmentRepository enrollments)
    {
        _mediator = mediator;
        _leads = leads;
        _enrollments = enrollments;
    }

    public string Name => "escalate_to_human";
    public string Description => "Escala la conversación a un asesor humano. Úsala cuando el usuario lo pida explícitamente, o cuando no puedas resolver su consulta.";
    public string JsonSchema => """
        {
          "type": "object",
          "properties": {
            "reason": {
              "type": "string",
              "description": "Motivo del escalamiento"
            }
          },
          "required": ["reason"]
        }
        """;

    public async Task<string> ExecuteAsync(string arguments, AgentContext context, CancellationToken ct)
    {
        Args? args;
        try { args = JsonSerializer.Deserialize<Args>(arguments); }
        catch { args = null; }

        var lead = await _leads.GetByPhoneNumberAsync(context.PhoneE164, ct);
        if (lead is null)
            return "{\"message\": \"Un asesor se comunicará contigo pronto.\"}";

        var enrollments = await _enrollments.GetActiveByLeadAsync(lead.Id, ct);
        var enrollment = enrollments.FirstOrDefault();

        if (enrollment is null)
            return "{\"message\": \"Un asesor se comunicará contigo pronto.\"}";

        var result = await _mediator.Send(
            new EscalateToHumanCommand(context.PhoneE164, enrollment.Id, args?.Reason), ct);

        return JsonSerializer.Serialize(new { message = result.Message, agent_email = result.SalesAgentEmail });
    }

    private record Args([property: JsonPropertyName("reason")] string? Reason);
}
