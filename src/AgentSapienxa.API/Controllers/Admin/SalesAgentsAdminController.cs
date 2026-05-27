using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/sales-agents")]
[Authorize]
public class SalesAgentsAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SalesAgentsAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var agents = await _db.SalesAgents
            .OrderBy(a => a.AgentName)
            .Select(a => new { a.Id, Name = a.AgentName, a.Email, a.PhoneNumber, a.LeadClassificationSummary })
            .ToListAsync(ct);

        return Ok(agents);
    }
}
