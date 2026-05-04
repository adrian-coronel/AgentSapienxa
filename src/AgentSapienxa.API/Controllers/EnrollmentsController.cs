using AgentSapienxa.API.Filters;
using AgentSapienxa.Application.Enrollments.Commands.CreateEnrollment;
using AgentSapienxa.Application.Enrollments.Commands.EscalateToHuman;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgentSapienxa.API.Controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public EnrollmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Registra interés de un lead en un catálogo de cursos.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateEnrollmentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateEnrollmentCommand(req.PhoneNumber, req.CatalogItemId), ct);
        return Ok(new { status = "ok", result.EnrollmentId, result.AlreadyEnrolled, result.Message });
    }

    /// <summary>Escala un enrollment a un agente humano.</summary>
    [HttpPost("escalate")]
    [PaymentsFeature]
    [ServiceFilter(typeof(PaymentsFeatureGate))]
    [ProducesResponseType(typeof(EscalateToHumanResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Escalate([FromBody] EscalateRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new EscalateToHumanCommand(req.PhoneNumber, req.EnrollmentId, req.Observation), ct);
        return Ok(new { status = "ok", result.Message, result.SalesAgentEmail });
    }
}

public record CreateEnrollmentRequest(string PhoneNumber, Guid CatalogItemId);
public record EscalateRequest(string PhoneNumber, Guid EnrollmentId, string? Observation);
