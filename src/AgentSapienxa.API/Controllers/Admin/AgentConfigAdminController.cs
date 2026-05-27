using AgentSapienxa.Domain.Agents;
using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/agents")]
[Authorize]
public class AgentConfigAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AgentConfigAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var agents = await _db.AgentConfigs
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.Id, a.AgentKey, a.Name, a.Description, a.Model,
                a.Temperature, a.MaxTokens, a.MemoryWindow, a.IsActive,
                a.CreatedAt, a.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(agents);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var a = await _db.AgentConfigs.FindAsync([id], ct);
        if (a is null) return NotFound();

        return Ok(new
        {
            a.Id, a.AgentKey, a.Name, a.Description, a.SystemPrompt,
            a.Model, a.Temperature, a.MaxTokens, a.MemoryWindow, a.IsActive,
            a.CreatedAt, a.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAgentConfigRequest req, CancellationToken ct)
    {
        var config = AgentConfig.Create(
            req.AgentKey, req.Name, req.SystemPrompt, req.Model,
            req.Temperature, req.MemoryWindow, req.MaxTokens, req.Description);

        _db.AgentConfigs.Add(config);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = config.Id }, new { config.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentConfigRequest req, CancellationToken ct)
    {
        var config = await _db.AgentConfigs.FindAsync([id], ct);
        if (config is null) return NotFound();

        config.Update(req.Name, req.Description, req.SystemPrompt, req.Model,
            req.Temperature, req.MemoryWindow, req.MaxTokens, req.IsActive);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var config = await _db.AgentConfigs.FindAsync([id], ct);
        if (config is null) return NotFound();

        _db.AgentConfigs.Remove(config);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record CreateAgentConfigRequest(
    string AgentKey, string Name, string SystemPrompt, string Model,
    decimal Temperature = 0.2m, int MemoryWindow = 10, int? MaxTokens = null, string? Description = null);

public record UpdateAgentConfigRequest(
    string Name, string SystemPrompt, string Model,
    decimal Temperature, int MemoryWindow, bool IsActive, int? MaxTokens = null, string? Description = null);
