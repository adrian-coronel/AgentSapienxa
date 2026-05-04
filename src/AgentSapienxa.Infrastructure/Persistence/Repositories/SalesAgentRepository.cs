using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Domain.Leads;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class SalesAgentRepository : ISalesAgentRepository
{
    private readonly ApplicationDbContext _db;
    public SalesAgentRepository(ApplicationDbContext db) => _db = db;

    public Task<SalesAgent?> GetFirstAvailableAsync(CancellationToken ct) =>
        _db.SalesAgents.OrderBy(a => a.AgentName).FirstOrDefaultAsync(ct);

    public Task<SalesAgent?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.SalesAgents.FirstOrDefaultAsync(a => a.Id == id, ct);
}
