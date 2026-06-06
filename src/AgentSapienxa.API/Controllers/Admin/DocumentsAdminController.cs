using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Documents.Commands.DeleteDocument;
using AgentSapienxa.Application.Documents.Commands.PauseDocument;
using AgentSapienxa.Application.Documents.Commands.QueueAllDocuments;
using AgentSapienxa.Application.Documents.Commands.QueueDocument;
using AgentSapienxa.Application.Documents.Commands.ResumeDocument;
using AgentSapienxa.Application.Documents.Commands.UploadDocument;
using AgentSapienxa.Application.Documents.Queries.GetDocumentStatus;
using AgentSapienxa.Application.Documents.Queries.ListDocuments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/documents")]
[Authorize]
[RequestSizeLimit(25 * 1024 * 1024)] // 25 MB limit
public class DocumentsAdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentCompanyAccessor _company;

    public DocumentsAdminController(IMediator mediator, ICurrentCompanyAccessor company)
    {
        _mediator = mediator;
        _company = company;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (_company.CompanyId is null) return Forbid();
        var result = await _mediator.Send(new ListDocumentsQuery(_company.CompanyId.Value, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (_company.CompanyId is null) return Forbid();
        if (file is null || file.Length == 0) return BadRequest(new { message = "El archivo es requerido." });

        var uploadedBy = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;

        var contentType = NormalizeContentType(file.ContentType, file.FileName);

        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadDocumentCommand(
            CompanyId: _company.CompanyId.Value,
            FileName: file.FileName,
            FileSize: file.Length,
            ContentType: contentType,
            FileContent: stream,
            UploadedBy: uploadedBy), ct);

        return StatusCode(201, result);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
    {
        if (_company.CompanyId is null) return Forbid();
        var result = await _mediator.Send(new GetDocumentStatusQuery(id, _company.CompanyId.Value), ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (_company.CompanyId is null) return Forbid();
        var result = await _mediator.Send(new GetDocumentStatusQuery(id, _company.CompanyId.Value), ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (_company.CompanyId is null) return Forbid();
        await _mediator.Send(new DeleteDocumentCommand(id, _company.CompanyId.Value), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/queue")]
    public async Task<IActionResult> Queue(Guid id, CancellationToken ct)
    {
        if (_company.CompanyId is null) return Forbid();
        var ok = await _mediator.Send(new QueueDocumentCommand(id, _company.CompanyId.Value), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("queue-all")]
    public async Task<IActionResult> QueueAll(CancellationToken ct)
    {
        if (_company.CompanyId is null) return Forbid();
        var count = await _mediator.Send(new QueueAllDocumentsCommand(_company.CompanyId.Value), ct);
        return Ok(new { count });
    }

    [HttpPost("{id:guid}/pause")]
    public async Task<IActionResult> Pause(Guid id, CancellationToken ct)
    {
        if (_company.CompanyId is null) return Forbid();
        var ok = await _mediator.Send(new PauseDocumentCommand(id, _company.CompanyId.Value), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> Resume(Guid id, CancellationToken ct)
    {
        if (_company.CompanyId is null) return Forbid();
        var ok = await _mediator.Send(new ResumeDocumentCommand(id, _company.CompanyId.Value), ct);
        return ok ? NoContent() : NotFound();
    }

    private static string NormalizeContentType(string contentType, string fileName)
    {
        // Browsers sometimes send wrong content-type; fall back to extension
        if (!string.IsNullOrWhiteSpace(contentType) && contentType != "application/octet-stream")
            return contentType.Split(';')[0].Trim().ToLowerInvariant();

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".md" or ".markdown" => "text/markdown",
            ".txt" => "text/plain",
            _ => contentType
        };
    }
}
