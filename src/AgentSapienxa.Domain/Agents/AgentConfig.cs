using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Agents;

public class AgentConfig : Entity
{
    public string AgentKey { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string SystemPrompt { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public decimal Temperature { get; private set; } = 0.2m;
    public int? MaxTokens { get; private set; }
    public int MemoryWindow { get; private set; } = 10;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private AgentConfig() { }

    public static AgentConfig Create(
        string key, string name, string systemPrompt, string model,
        decimal temperature = 0.2m, int memoryWindow = 10, int? maxTokens = null,
        string? description = null)
    {
        return new AgentConfig
        {
            AgentKey = key,
            Name = name,
            Description = description,
            SystemPrompt = systemPrompt,
            Model = model,
            Temperature = temperature,
            MemoryWindow = memoryWindow,
            MaxTokens = maxTokens,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdatePrompt(string systemPrompt)
    {
        SystemPrompt = systemPrompt;
        UpdatedAt = DateTime.UtcNow;
    }
}
