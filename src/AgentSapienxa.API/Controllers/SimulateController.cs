using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Companies.Repositories;
using AgentSapienxa.Application.Webhooks;
using AgentSapienxa.Infrastructure.Messaging;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgentSapienxa.API.Controllers;

[ApiController]
[Route("api/simulate")]
public class SimulateController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMessagingChannel _channel;
    private readonly IMediaTranscriber _transcriber;
    private readonly ICompanyRepository _companies;
    private readonly ICurrentCompanyAccessor _companyAccessor;

    public SimulateController(
        IMediator mediator,
        IMessagingChannel channel,
        IMediaTranscriber transcriber,
        ICompanyRepository companies,
        ICurrentCompanyAccessor companyAccessor)
    {
        _mediator = mediator;
        _channel = channel;
        _transcriber = transcriber;
        _companies = companies;
        _companyAccessor = companyAccessor;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies(CancellationToken ct)
    {
        var all = await _companies.GetAllAsync(ct);
        var active = all
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.Name, c.Slug })
            .ToList();
        return Ok(active);
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] SimulateChatRequest request, CancellationToken ct)
    {
        if (_channel is not SimulationMessagingChannel sim)
            return StatusCode(503, "Simulation mode is only available in Development environment.");

        await ResolveCompanyAsync(request.CompanySlug, ct);

        var message = new IncomingMessage
        {
            SessionId = request.SessionId,
            ContactName = request.ContactName ?? "Simulación",
            Type = IncomingMessageType.Text,
            Text = request.Message,
            RawMessageId = $"sim-{Guid.NewGuid()}"
        };

        await _mediator.Send(new ProcessIncomingMessageCommand(message), ct);

        return Ok(new { responses = sim.GetResponses() });
    }

    [HttpPost("audio")]
    public async Task<IActionResult> Audio([FromForm] SimulateAudioRequest request, CancellationToken ct)
    {
        if (_channel is not SimulationMessagingChannel sim)
            return StatusCode(503, "Simulation mode is only available in Development environment.");

        if (request.Audio is null || request.Audio.Length == 0)
            return BadRequest("No audio file provided.");

        await ResolveCompanyAsync(request.CompanySlug, ct);

        await using var stream = request.Audio.OpenReadStream();
        var mimeType = request.Audio.ContentType ?? "audio/webm";
        var transcription = await _transcriber.TranscribeAsync(stream, mimeType, ct);

        if (string.IsNullOrWhiteSpace(transcription))
            return Ok(new { transcription = "", responses = Array.Empty<string>() });

        var message = new IncomingMessage
        {
            SessionId = request.SessionId,
            ContactName = request.ContactName ?? "Simulación",
            Type = IncomingMessageType.Text,
            Text = transcription,
            RawMessageId = $"sim-audio-{Guid.NewGuid()}"
        };

        await _mediator.Send(new ProcessIncomingMessageCommand(message), ct);

        return Ok(new { transcription, responses = sim.GetResponses() });
    }

    private async Task ResolveCompanyAsync(string? slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug)) return;
        var company = await _companies.GetBySlugAsync(slug, ct);
        if (company is not null)
            _companyAccessor.CompanyId = company.Id;
    }
}

public record SimulateChatRequest(string SessionId, string Message, string? ContactName, string? CompanySlug);
public record SimulateAudioRequest(string SessionId, IFormFile? Audio, string? ContactName, string? CompanySlug);
