using AgentSapienxa.Domain.Catalog;

namespace AgentSapienxa.Application.Catalog.Repositories;

public interface ICatalogRepository
{
    Task<IReadOnlyList<CatalogItem>> GetAllAsync(bool onlyAvailable = false, CancellationToken ct = default);
    Task<CatalogItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
