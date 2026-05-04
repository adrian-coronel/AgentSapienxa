using AgentSapienxa.Application.Leads.Commands.CaptureLead;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgentSapienxa.API.Controllers;

[ApiController]
[Route("api/leads")]
public class LeadsController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeadsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Captura o actualiza un lead por número de teléfono.</summary>
    [HttpPost("capture")]
    [ProducesResponseType(typeof(CaptureLeadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Capture([FromBody] CaptureLeadRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new CaptureLeadCommand(req.PhoneNumber, req.Name, req.Email, req.ContactMethod), ct);
        return Ok(new { status = "ok", result.LeadId, result.IsNew, result.Message });
    }
}

public record CaptureLeadRequest(string PhoneNumber, string? Name, string? Email, string? ContactMethod);
