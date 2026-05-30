using AgentSapienxa.Domain.Catalog;
using AgentSapienxa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/catalog")]
[Authorize]
public class CatalogAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CatalogAdminController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _db.CatalogItems
            .Include(c => c.Instructor)
            .OrderBy(c => c.Title)
            .Select(c => new
            {
                c.Id, c.Code, c.Title, c.ShortDescription, c.Cost,
                c.Places, c.AvailablePlaces, c.StartDate, c.Link,
                c.Features, c.Details, c.Syllabus, c.Projects,
                InstructorId = c.InstructorId,
                InstructorName = c.Instructor != null ? c.Instructor.InstructorName : null
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _db.CatalogItems
            .Include(c => c.Instructor)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (item is null) return NotFound();

        return Ok(new
        {
            item.Id, item.Code, item.Title, item.ShortDescription, item.Cost,
            item.Places, item.AvailablePlaces, item.StartDate, item.Link,
            item.Features, item.Details, item.Syllabus, item.Projects,
            item.InstructorId,
            Instructor = item.Instructor is null ? null : new
            {
                item.Instructor.Id,
                item.Instructor.InstructorName,
                item.Instructor.Expertise
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertCatalogItemRequest req, CancellationToken ct)
    {
        var item = CatalogItem.Create(
            req.Title, req.Cost, req.Code, req.ShortDescription,
            req.Features, req.Details, req.Syllabus, req.Projects,
            req.Link, req.InstructorId, req.Places, req.AvailablePlaces, req.StartDate);

        _db.CatalogItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, new { item.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertCatalogItemRequest req, CancellationToken ct)
    {
        var item = await _db.CatalogItems.FindAsync([id], ct);
        if (item is null) return NotFound();

        _db.Entry(item).CurrentValues.SetValues(new
        {
            Code = req.Code,
            Title = req.Title,
            ShortDescription = req.ShortDescription,
            Features = req.Features,
            Details = req.Details,
            Syllabus = req.Syllabus,
            Projects = req.Projects,
            Link = req.Link,
            InstructorId = req.InstructorId,
            Cost = req.Cost,
            Places = req.Places,
            AvailablePlaces = req.AvailablePlaces,
            StartDate = req.StartDate
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await _db.CatalogItems.FindAsync([id], ct);
        if (item is null) return NotFound();

        _db.CatalogItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record UpsertCatalogItemRequest(
    string Title,
    decimal Cost,
    string? Code,
    string? ShortDescription,
    string? Features,
    string? Details,
    string? Syllabus,
    string? Projects,
    string? Link,
    Guid? InstructorId,
    int? Places,
    int? AvailablePlaces,
    DateOnly? StartDate);
