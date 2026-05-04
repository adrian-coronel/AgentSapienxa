using AgentSapienxa.Application.Conversations.Repositories;
using AgentSapienxa.Domain.Conversations;
using Microsoft.EntityFrameworkCore;

namespace AgentSapienxa.Infrastructure.Persistence.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly ApplicationDbContext _db;
    public ConversationRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConversationMessage>> GetHistoryAsync(string sessionId, int windowSize, CancellationToken ct) =>
        await _db.ConversationMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(windowSize)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(ConversationMessage message, CancellationToken ct)
    {
        _db.ConversationMessages.Add(message);
        await _db.SaveChangesAsync(ct);
    }
}
