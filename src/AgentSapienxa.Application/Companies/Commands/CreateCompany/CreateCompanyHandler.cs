using AgentSapienxa.Application.Companies.Repositories;
using AgentSapienxa.Domain.Companies;
using MediatR;

namespace AgentSapienxa.Application.Companies.Commands.CreateCompany;

public class CreateCompanyHandler : IRequestHandler<CreateCompanyCommand, Guid>
{
    private readonly ICompanyRepository _companies;

    public CreateCompanyHandler(ICompanyRepository companies) => _companies = companies;

    public async Task<Guid> Handle(CreateCompanyCommand cmd, CancellationToken ct)
    {
        var company = Company.Create(cmd.Name, cmd.Slug);
        await _companies.AddAsync(company, ct);
        return company.Id;
    }
}
