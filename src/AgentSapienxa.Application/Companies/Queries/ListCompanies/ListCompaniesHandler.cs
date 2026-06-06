using AgentSapienxa.Application.Companies.Repositories;
using MediatR;

namespace AgentSapienxa.Application.Companies.Queries.ListCompanies;

public class ListCompaniesHandler : IRequestHandler<ListCompaniesQuery, IReadOnlyList<CompanyDto>>
{
    private readonly ICompanyRepository _companies;

    public ListCompaniesHandler(ICompanyRepository companies) => _companies = companies;

    public async Task<IReadOnlyList<CompanyDto>> Handle(ListCompaniesQuery query, CancellationToken ct)
    {
        var companies = await _companies.GetAllAsync(ct);
        return companies.Select(c => new CompanyDto(c.Id, c.Name, c.Slug, c.IsActive, c.CreatedAt)).ToList();
    }
}
