using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Leads.Commands.CaptureLead;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentSapienxa.Application.Agents.Tools;

public class CaptureLeadTool : IAgentTool
{
    private readonly IMediator _mediator;
    public CaptureLeadTool(IMediator mediator) => _mediator = mediator;

    public string Name => "capture_lead";
    public string Description => "Registra o actualiza el nombre y email del usuario. Úsala cuando el usuario te proporcione su nombre o email.";
    public string JsonSchema => """
        {
          "type": "object",
          "properties": {
            "name": { "type": "string", "description": "Nombre completo del usuario" },
            "email": { "type": "string", "description": "Email del usuario (opcional)" }
          }
        }
        """;

    public async Task<string> ExecuteAsync(string arguments, AgentContext context, CancellationToken ct)
    {
        Args? args;
        try { args = JsonSerializer.Deserialize<Args>(arguments); }
        catch { args = null; }

        var result = await _mediator.Send(
            new CaptureLeadCommand(context.PhoneE164, args?.Name ?? context.ContactName, args?.Email, "WhatsApp"), ct);

        return JsonSerializer.Serialize(new { lead_id = result.LeadId, is_new = result.IsNew, message = result.Message });
    }

    private record Args(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("email")] string? Email);
}
