using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Conversations;

public class ConversationMessage : Entity
{
    public Guid CompanyId { get; private set; }
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
        string? model = null,
        Guid? companyId = null)
    {
        return new ConversationMessage
        {
            CompanyId = companyId ?? Guid.Empty,
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

    public void AssignToCompany(Guid companyId) => CompanyId = companyId;
}
