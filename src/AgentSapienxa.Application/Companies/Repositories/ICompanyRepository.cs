using AgentSapienxa.Domain.Companies;

namespace AgentSapienxa.Application.Companies.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Company?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<Company>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Company company, CancellationToken ct = default);
    Task UpdateAsync(Company company, CancellationToken ct = default);
}
