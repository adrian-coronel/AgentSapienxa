using AgentSapienxa.Domain.Enrollments;
using AgentSapienxa.Domain.Payments;
using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
public class DashboardAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DashboardAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var totalLeads = await _db.Leads.CountAsync(ct);
        var totalEnrollments = await _db.Enrollments.CountAsync(ct);
        var convertedEnrollments = await _db.Enrollments
            .CountAsync(e => e.Status == EnrollmentStatus.Pagado, ct);
        var pendingValidations = await _db.PaymentValidations
            .CountAsync(v => v.Status == PaymentValidationStatus.Pendiente, ct);
        var totalRevenue = await _db.Enrollments
            .Where(e => e.TotalCost != null && e.Status == EnrollmentStatus.Pagado)
            .SumAsync(e => e.TotalCost ?? 0, ct);

        var topCourses = await (from e in _db.Enrollments
                                join c in _db.CatalogItems on e.CatalogItemId equals c.Id
                                group e by new { e.CatalogItemId, c.Title } into g
                                orderby g.Count() descending
                                select new
                                {
                                    CatalogItemId = g.Key.CatalogItemId,
                                    Title = g.Key.Title,
                                    EnrollmentCount = g.Count(),
                                    Revenue = g.Sum(x => x.TotalCost ?? 0)
                                })
                               .Take(5)
                               .ToListAsync(ct);

        var recentLeads = await _db.Leads
            .OrderByDescending(l => l.Id)
            .Take(5)
            .Select(l => new { l.Id, Name = l.LeadName, l.PhoneNumber, l.Status, l.ContactMethod })
            .ToListAsync(ct);

        return Ok(new
        {
            TotalLeads = totalLeads,
            TotalEnrollments = totalEnrollments,
            ConvertedEnrollments = convertedEnrollments,
            PendingValidations = pendingValidations,
            TotalRevenue = totalRevenue,
            TopCourses = topCourses,
            RecentLeads = recentLeads
        });
    }
}
