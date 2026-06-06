using AgentSapienxa.Application.Companies.Repositories;
using AgentSapienxa.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _db;

    public CompanyRepository(ApplicationDbContext db) => _db = db;

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Company?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _db.Companies.FirstOrDefaultAsync(c => c.Slug == slug.ToLowerInvariant(), ct);

    public Task<List<Company>> GetAllAsync(CancellationToken ct = default) =>
        _db.Companies.OrderBy(c => c.Name).ToListAsync(ct);

    public async Task AddAsync(Company company, CancellationToken ct = default)
    {
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Company company, CancellationToken ct = default)
    {
        _db.Companies.Update(company);
        await _db.SaveChangesAsync(ct);
    }
}
