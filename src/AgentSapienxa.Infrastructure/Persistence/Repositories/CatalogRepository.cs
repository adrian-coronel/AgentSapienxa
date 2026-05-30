using AgentSapienxa.Application.Catalog.Repositories;
using AgentSapienxa.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class CatalogRepository : ICatalogRepository
{
    private readonly ApplicationDbContext _db;
    public CatalogRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CatalogItem>> GetAllAsync(bool onlyAvailable = false, CancellationToken ct = default)
    {
        var query = _db.CatalogItems.Include(c => c.Instructor).AsQueryable();
        if (onlyAvailable)
            query = query.Where(c => c.AvailablePlaces != null && c.AvailablePlaces > 0);
        return await query.ToListAsync(ct);
    }

    public Task<CatalogItem?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.CatalogItems.Include(c => c.Instructor).FirstOrDefaultAsync(c => c.Id == id, ct);
}
