using AgentSapienxa.Application.Catalog.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AgentSapienxa.API.Controllers;

[ApiController]
[Route("api/instructors")]
public class InstructorsController : ControllerBase
{
    private readonly IInstructorRepository _instructors;
    public InstructorsController(IInstructorRepository instructors) => _instructors = instructors;

    /// <summary>Detalle de un instructor.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var instructor = await _instructors.GetByIdAsync(id, ct);
        if (instructor is null) return NotFound();

        return Ok(new
        {
            instructor.Id,
            Name = instructor.InstructorName,
            instructor.Email,
            instructor.PhoneNumber,
            instructor.ProfilePicture,
            instructor.Expertise,
            Summary = instructor.InstructorSummary
        });
    }
}
