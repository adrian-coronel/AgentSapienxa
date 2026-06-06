using MediatR;

namespace AgentSapienxa.Application.Companies.Queries.ListCompanies;

public record ListCompaniesQuery : IRequest<IReadOnlyList<CompanyDto>>;

public record CompanyDto(Guid Id, string Name, string Slug, bool IsActive, DateTime CreatedAt);
