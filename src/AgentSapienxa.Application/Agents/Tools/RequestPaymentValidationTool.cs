using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Enrollments.Repositories;
using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Application.Payments.Commands.RequestPaymentValidation;
using AgentSapienxa.Domain.Enrollments;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentSapienxa.Application.Agents.Tools;

public class RequestPaymentValidationTool : IAgentTool
{
    private readonly IMediator _mediator;
    private readonly ILeadRepository _leads;
    private readonly IEnrollmentRepository _enrollments;

    public RequestPaymentValidationTool(IMediator mediator, ILeadRepository leads, IEnrollmentRepository enrollments)
    {
        _mediator = mediator;
        _leads = leads;
        _enrollments = enrollments;
    }

    public string Name => "request_payment_validation";
    public string Description => "Envía el voucher de pago al equipo de ventas para validación manual. Úsala cuando el usuario comparta los datos o imagen de su transferencia.";
    public string JsonSchema => """
        {
          "type": "object",
          "properties": {
            "voucher_detail": {
              "type": "string",
              "description": "Detalle del comprobante: monto, banco, número de operación, fecha"
            },
            "voucher_url": {
              "type": "string",
              "description": "URL de la imagen del comprobante (si fue enviada como imagen)"
            }
          },
          "required": ["voucher_detail"]
        }
        """;

    public async Task<string> ExecuteAsync(string arguments, AgentContext context, CancellationToken ct)
    {
        Args? args;
        try { args = JsonSerializer.Deserialize<Args>(arguments); }
        catch { return "{\"error\": \"Argumentos inválidos\"}"; }

        var lead = await _leads.GetByPhoneNumberAsync(context.PhoneE164, ct);
        if (lead is null) return "{\"error\": \"Lead no encontrado. Llama a capture_lead primero.\"}";

        var enrollments = await _enrollments.GetActiveByLeadAsync(lead.Id, ct);
        var pending = enrollments.FirstOrDefault(e => e.Status == EnrollmentStatus.PendientePago);
        if (pending is null) return "{\"error\": \"No hay inscripción en estado PendientePago. Usa checkout primero.\"}";

        var result = await _mediator.Send(
            new RequestPaymentValidationCommand(context.PhoneE164, pending.Id, args?.VoucherDetail ?? "", args?.VoucherUrl), ct);

        return JsonSerializer.Serialize(new { validation_id = result.ValidationId, message = result.Message });
    }

    private record Args(
        [property: JsonPropertyName("voucher_detail")] string? VoucherDetail,
        [property: JsonPropertyName("voucher_url")] string? VoucherUrl);
}
