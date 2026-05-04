using AgentSapienxa.Application.Catalog.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AgentSapienxa.API.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICatalogRepository _catalog;
    public CoursesController(ICatalogRepository catalog) => _catalog = catalog;

    /// <summary>Lista todos los cursos disponibles.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyAvailable = false, CancellationToken ct = default)
    {
        var items = await _catalog.GetAllAsync(onlyAvailable, ct);
        var result = items.Select(c => new
        {
            c.Id,
            c.Code,
            c.Title,
            c.ShortDescription,
            c.Cost,
            c.AvailablePlaces,
            c.StartDate,
            c.Link,
            InstructorId = c.InstructorId
        });
        return Ok(result);
    }

    /// <summary>Detalle de un curso.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _catalog.GetByIdAsync(id, ct);
        if (item is null) return NotFound();

        return Ok(new
        {
            item.Id,
            item.Code,
            item.Title,
            item.ShortDescription,
            item.Features,
            item.Details,
            item.Syllabus,
            item.Projects,
            item.Cost,
            item.AvailablePlaces,
            item.Places,
            item.StartDate,
            item.Link,
            Instructor = item.Instructor is null ? null : new
            {
                item.Instructor.Id,
                item.Instructor.InstructorName,
                item.Instructor.Expertise,
                item.Instructor.InstructorSummary
            }
        });
    }
}
