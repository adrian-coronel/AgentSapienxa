using AgentSapienxa.Domain.Catalog;
using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/instructors")]
[Authorize]
public class InstructorsAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public InstructorsAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var instructors = await _db.Instructors
            .OrderBy(i => i.InstructorName)
            .Select(i => new
            {
                i.Id,
                Name = i.InstructorName,
                i.Email,
                i.PhoneNumber,
                i.ProfilePicture,
                i.Expertise,
                Summary = i.InstructorSummary
            })
            .ToListAsync(ct);

        return Ok(instructors);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var i = await _db.Instructors.FindAsync([id], ct);
        if (i is null) return NotFound();

        return Ok(new
        {
            i.Id,
            Name = i.InstructorName,
            i.Email,
            i.PhoneNumber,
            i.ProfilePicture,
            i.Expertise,
            Summary = i.InstructorSummary
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertInstructorRequest req, CancellationToken ct)
    {
        var instructor = Instructor.Create(req.Name, req.Email, req.PhoneNumber,
            req.ProfilePicture, req.Expertise, req.Summary);

        _db.Instructors.Add(instructor);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = instructor.Id }, new { instructor.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertInstructorRequest req, CancellationToken ct)
    {
        var instructor = await _db.Instructors.FindAsync([id], ct);
        if (instructor is null) return NotFound();

        _db.Entry(instructor).CurrentValues.SetValues(new
        {
            InstructorName = req.Name,
            Email = req.Email,
            PhoneNumber = req.PhoneNumber,
            ProfilePicture = req.ProfilePicture,
            Expertise = req.Expertise,
            InstructorSummary = req.Summary
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var instructor = await _db.Instructors.FindAsync([id], ct);
        if (instructor is null) return NotFound();

        _db.Instructors.Remove(instructor);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record UpsertInstructorRequest(
    string Name,
    string? Email,
    string? PhoneNumber,
    string? ProfilePicture,
    string? Expertise,
    string? Summary);
