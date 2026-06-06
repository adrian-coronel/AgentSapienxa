using AgentSapienxa.Application.Companies.Commands.CreateCompany;
using AgentSapienxa.Application.Companies.Queries.ListCompanies;
using AgentSapienxa.Application.Companies.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentSapienxa.API.Controllers.Admin;

[ApiController]
[Route("api/admin/companies")]
[Authorize]
public class CompaniesAdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICompanyRepository _companies;

    public CompaniesAdminController(IMediator mediator, ICompanyRepository companies)
    {
        _mediator = mediator;
        _companies = companies;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListCompaniesQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest req, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateCompanyCommand(req.Name, req.Slug), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var company = await _companies.GetByIdAsync(id, ct);
        if (company is null) return NotFound();
        return Ok(new { company.Id, company.Name, company.Slug, company.IsActive, company.CreatedAt });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest req, CancellationToken ct)
    {
        var company = await _companies.GetByIdAsync(id, ct);
        if (company is null) return NotFound();
        company.Update(req.Name, req.IsActive);
        await _companies.UpdateAsync(company, ct);
        return NoContent();
    }
}

public record CreateCompanyRequest(string Name, string Slug);
public record UpdateCompanyRequest(string Name, bool IsActive);
