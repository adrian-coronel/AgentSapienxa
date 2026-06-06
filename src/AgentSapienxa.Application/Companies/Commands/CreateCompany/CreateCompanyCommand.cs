using MediatR;

namespace AgentSapienxa.Application.Companies.Commands.CreateCompany;

public record CreateCompanyCommand(string Name, string Slug) : IRequest<Guid>;
