using AgentSapienxa.API.Filters;
using AgentSapienxa.Application.Payments.Commands.RequestPaymentValidation;
using AgentSapienxa.Application.Payments.Commands.ResolvePaymentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgentSapienxa.API.Controllers;

[ApiController]
[Route("api/payments")]
[PaymentsFeature]
[ServiceFilter(typeof(PaymentsFeatureGate))]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Solicita validación de un voucher de pago (asíncrono, responde 202).</summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Validate([FromBody] ValidateRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new RequestPaymentValidationCommand(req.PhoneNumber, req.EnrollmentId, req.VoucherDetail, req.VoucherUrl), ct);
        return Accepted(new { status = "in_progress", result.ValidationId, result.Message });
    }

    /// <summary>Endpoint para que el agente humano resuelva la validación.</summary>
    [HttpPost("validate/decision")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Decision([FromBody] DecisionRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ResolvePaymentValidationCommand(req.ValidationId, req.Decision, req.ResolvedBy, req.Observation), ct);
        return Ok(new { status = "ok", result.EnrollmentStatus, result.Message });
    }
}

public record ValidateRequest(string PhoneNumber, Guid EnrollmentId, string? VoucherDetail, string? VoucherUrl);
public record DecisionRequest(Guid ValidationId, string Decision, string ResolvedBy, string? Observation);
