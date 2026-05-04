using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Enrollments.Commands.Checkout;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentSapienxa.Application.Agents.Tools;

public class CheckoutTool : IAgentTool
{
    private readonly IMediator _mediator;
    public CheckoutTool(IMediator mediator) => _mediator = mediator;

    public string Name => "checkout";
    public string Description => "Inicia el proceso de pago: valida monto vs. límite del método de pago y mueve el enrollment a PendientePago. Úsala cuando el usuario elija cómo pagar.";
    public string JsonSchema => """
        {
          "type": "object",
          "properties": {
            "catalog_item_id": {
              "type": "string",
              "description": "ID del curso a pagar"
            },
            "payment_method_name": {
              "type": "string",
              "description": "Nombre del método de pago (ej: 'Yape', 'BCP', 'Interbank')"
            }
          },
          "required": ["catalog_item_id", "payment_method_name"]
        }
        """;

    public async Task<string> ExecuteAsync(string arguments, AgentContext context, CancellationToken ct)
    {
        Args? args;
        try { args = JsonSerializer.Deserialize<Args>(arguments); }
        catch { return "{\"error\": \"Argumentos inválidos\"}"; }

        if (!Guid.TryParse(args?.CatalogItemId, out var catalogItemId))
            return "{\"error\": \"catalog_item_id inválido\"}";

        try
        {
            var result = await _mediator.Send(
                new CheckoutCommand(context.PhoneE164, catalogItemId, args?.PaymentMethodName ?? ""), ct);

            return JsonSerializer.Serialize(new
            {
                course = result.CourseTitle,
                amount = result.Amount,
                payment_method = result.PaymentMethodName,
                instructions = result.PaymentMethodDescription,
                image_url = result.PaymentMethodImage
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private record Args(
        [property: JsonPropertyName("catalog_item_id")] string? CatalogItemId,
        [property: JsonPropertyName("payment_method_name")] string? PaymentMethodName);
}
