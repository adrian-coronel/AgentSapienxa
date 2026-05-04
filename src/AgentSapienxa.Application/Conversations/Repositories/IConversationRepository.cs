using AgentSapienxa.Domain.Conversations;

namespace AgentSapienxa.Application.Conversations.Repositories;

public interface IConversationRepository
{
    Task<IReadOnlyList<ConversationMessage>> GetHistoryAsync(string sessionId, int windowSize, CancellationToken ct = default);
    Task AddAsync(ConversationMessage message, CancellationToken ct = default);
}
