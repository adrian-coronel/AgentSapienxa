using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/payment-validations")]
[Authorize]
public class PaymentValidationsAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PaymentValidationsAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken ct)
    {
        var query = _db.PaymentValidations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(v => v.Status == status);

        var validations = await query
            .OrderByDescending(v => v.RequestedAt)
            .Select(v => new
            {
                v.Id, v.EnrollmentId, v.VoucherDetail, v.VoucherUrl,
                v.Status, v.RequestedBy, v.ResolvedBy, v.Observation,
                v.RequestedAt, v.ResolvedAt
            })
            .ToListAsync(ct);

        return Ok(validations);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var v = await _db.PaymentValidations.FindAsync([id], ct);
        if (v is null) return NotFound();

        var enrollment = await _db.Enrollments.FindAsync([v.EnrollmentId], ct);
        var lead = enrollment is not null ? await _db.Leads.FindAsync([enrollment.LeadId], ct) : null;
        var course = enrollment is not null ? await _db.CatalogItems.FindAsync([enrollment.CatalogItemId], ct) : null;

        return Ok(new
        {
            v.Id, v.EnrollmentId, v.VoucherDetail, v.VoucherUrl,
            v.Status, v.RequestedBy, v.ResolvedBy, v.Observation,
            v.RequestedAt, v.ResolvedAt,
            Enrollment = enrollment is null ? null : new
            {
                enrollment.Id,
                LeadName = lead?.LeadName,
                LeadPhone = lead?.PhoneNumber,
                CourseTitle = course?.Title,
                enrollment.TotalCost
            }
        });
    }
}
