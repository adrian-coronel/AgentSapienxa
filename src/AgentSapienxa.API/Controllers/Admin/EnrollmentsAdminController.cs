using AgentSapienxa.Domain.Enrollments;
using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/enrollments")]
[Authorize]
public class EnrollmentsAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public EnrollmentsAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken ct)
    {
        var query = from e in _db.Enrollments
                    join l in _db.Leads on e.LeadId equals l.Id into leads
                    from l in leads.DefaultIfEmpty()
                    join c in _db.CatalogItems on e.CatalogItemId equals c.Id into courses
                    from c in courses.DefaultIfEmpty()
                    join pm in _db.PaymentMethods on e.PaymentMethodId equals pm.Id into methods
                    from pm in methods.DefaultIfEmpty()
                    select new
                    {
                        e.Id, e.Status, e.TotalCost, e.Voucher, e.Observation,
                        LeadId = e.LeadId,
                        LeadName = l != null ? l.LeadName : null,
                        LeadPhone = l != null ? l.PhoneNumber : null,
                        CatalogItemId = e.CatalogItemId,
                        CourseTitle = c != null ? c.Title : null,
                        CourseCost = c != null ? (decimal?)c.Cost : null,
                        PaymentMethodId = e.PaymentMethodId,
                        PaymentMethodName = pm != null ? pm.Name : null
                    };

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        var enrollments = await query.OrderByDescending(e => e.Id).ToListAsync(ct);
        return Ok(enrollments);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var e = await _db.Enrollments.FindAsync([id], ct);
        if (e is null) return NotFound();

        var lead = e.LeadId != Guid.Empty ? await _db.Leads.FindAsync([e.LeadId], ct) : null;
        var course = await _db.CatalogItems.FindAsync([e.CatalogItemId], ct);
        var instructor = course?.InstructorId.HasValue == true
            ? await _db.Instructors.FindAsync([course.InstructorId!.Value], ct)
            : null;
        var paymentMethod = e.PaymentMethodId.HasValue
            ? await _db.PaymentMethods.FindAsync([e.PaymentMethodId.Value], ct)
            : null;

        return Ok(new
        {
            e.Id, e.Status, e.TotalCost, e.Voucher, e.Observation,
            Lead = lead is null ? null : new { lead.Id, Name = lead.LeadName, lead.PhoneNumber, lead.Email },
            CatalogItem = course is null ? null : new
            {
                course.Id, course.Title, course.Cost,
                InstructorName = instructor?.InstructorName
            },
            PaymentMethod = paymentMethod is null ? null : new { paymentMethod.Id, paymentMethod.Name }
        });
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateEnrollmentStatusRequest req, CancellationToken ct)
    {
        var enrollment = await _db.Enrollments.FindAsync([id], ct);
        if (enrollment is null) return NotFound();

        var result = enrollment.Transition(req.Status);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record UpdateEnrollmentStatusRequest(string Status);
