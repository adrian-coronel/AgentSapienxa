using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Enrollments.Commands.CreateEnrollment;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentSapienxa.Application.Agents.Tools;

public class RegisterEnrollmentTool : IAgentTool
{
    private readonly IMediator _mediator;
    public RegisterEnrollmentTool(IMediator mediator) => _mediator = mediator;

    public string Name => "register_enrollment";
    public string Description => "Registra el interés del usuario en un curso (estado: Interesado). Llama a capture_lead antes si aún no tienes el nombre. Úsala cuando el usuario confirme que quiere inscribirse.";
    public string JsonSchema => """
        {
          "type": "object",
          "properties": {
            "catalog_item_id": {
              "type": "string",
              "description": "ID del curso en el que se quiere inscribir"
            }
          },
          "required": ["catalog_item_id"]
        }
        """;

    public async Task<string> ExecuteAsync(string arguments, AgentContext context, CancellationToken ct)
    {
        Args? args;
        try { args = JsonSerializer.Deserialize<Args>(arguments); }
        catch { return "{\"error\": \"Argumentos inválidos\"}"; }

        if (!Guid.TryParse(args?.CatalogItemId, out var catalogItemId))
            return "{\"error\": \"catalog_item_id inválido\"}";

        var result = await _mediator.Send(new CreateEnrollmentCommand(context.PhoneE164, catalogItemId), ct);

        return JsonSerializer.Serialize(new
        {
            enrollment_id = result.EnrollmentId,
            already_enrolled = result.AlreadyEnrolled,
            message = result.Message
        });
    }

    private record Args([property: JsonPropertyName("catalog_item_id")] string? CatalogItemId);
}
