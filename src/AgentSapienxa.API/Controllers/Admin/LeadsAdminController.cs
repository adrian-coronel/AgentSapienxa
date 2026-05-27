using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/leads")]
[Authorize]
public class LeadsAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public LeadsAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken ct)
    {
        var query = from l in _db.Leads
                    join sa in _db.SalesAgents on l.SalesAgentId equals sa.Id into agents
                    from sa in agents.DefaultIfEmpty()
                    select new
                    {
                        l.Id,
                        Name = l.LeadName,
                        l.Email,
                        l.PhoneNumber,
                        l.ContactMethod,
                        l.Status,
                        l.SalesAgentId,
                        SalesAgentName = sa != null ? sa.AgentName : null
                    };

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(l => l.Status == status);

        var leads = await query.OrderByDescending(l => l.Id).ToListAsync(ct);
        return Ok(leads);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var lead = await _db.Leads.FindAsync([id], ct);
        if (lead is null) return NotFound();

        var salesAgent = lead.SalesAgentId.HasValue
            ? await _db.SalesAgents.FindAsync([lead.SalesAgentId.Value], ct)
            : null;

        var enrollments = await (from e in _db.Enrollments
                                 join c in _db.CatalogItems on e.CatalogItemId equals c.Id into courses
                                 from c in courses.DefaultIfEmpty()
                                 where e.LeadId == id
                                 select new
                                 {
                                     e.Id, e.Status, e.TotalCost, e.Voucher, e.Observation,
                                     CatalogItemId = e.CatalogItemId,
                                     CourseTitle = c != null ? c.Title : null
                                 }).ToListAsync(ct);

        return Ok(new
        {
            lead.Id,
            Name = lead.LeadName,
            lead.Email,
            lead.PhoneNumber,
            lead.ContactMethod,
            lead.Status,
            SalesAgent = salesAgent is null ? null : new
            {
                salesAgent.Id,
                salesAgent.AgentName,
                salesAgent.Email
            },
            Enrollments = enrollments
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadAdminRequest req, CancellationToken ct)
    {
        var lead = await _db.Leads.FindAsync([id], ct);
        if (lead is null) return NotFound();

        lead.UpdateInfo(req.Name, req.Email);

        if (req.SalesAgentId.HasValue)
            lead.AssignAgent(req.SalesAgentId.Value);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record UpdateLeadAdminRequest(string? Name, string? Email, Guid? SalesAgentId);
