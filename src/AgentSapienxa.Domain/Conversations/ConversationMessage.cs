using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Conversations;

public class ConversationMessage : Entity
{
    public string SessionId { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? ToolCalls { get; private set; }
    public int? TokensIn { get; private set; }
    public int? TokensOut { get; private set; }
    public string? Model { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private ConversationMessage() { }

    public static ConversationMessage Create(
        string sessionId,
        string role,
        string content,
        string? toolCalls = null,
        int? tokensIn = null,
        int? tokensOut = null,
        string? model = null)
    {
        return new ConversationMessage
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            ToolCalls = toolCalls,
            TokensIn = tokensIn,
            TokensOut = tokensOut,
            Model = model,
            CreatedAt = DateTime.UtcNow
        };
    }
}
