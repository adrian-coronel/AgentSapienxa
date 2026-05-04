using AgentSapienxa.Application.Agents.Repositories;
using AgentSapienxa.Domain.Agents;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class AgentConfigRepository : IAgentConfigRepository
{
    private readonly ApplicationDbContext _db;
    public AgentConfigRepository(ApplicationDbContext db) => _db = db;

    public Task<AgentConfig?> GetByKeyAsync(string key, CancellationToken ct) =>
        _db.AgentConfigs.FirstOrDefaultAsync(a => a.AgentKey == key && a.IsActive, ct);
}
