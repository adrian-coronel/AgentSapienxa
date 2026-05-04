namespace AgentSapienxa.Application.Common.Abstractions;

public record LlmMessage(string Role, string Content, string? ToolCalls = null);

public record LlmToolCall(string Id, string Name, string Arguments);

public record LlmResponse(
    string Content,
    IReadOnlyList<LlmToolCall> ToolCalls,
    int? TokensIn,
    int? TokensOut,
    string? Model);

public interface ILlmProvider
{
    Task<LlmResponse> CompleteAsync(
        string model,
        decimal temperature,
        string systemPrompt,
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition>? tools = null,
        CancellationToken ct = default);
}

public record LlmToolDefinition(string Name, string Description, string JsonSchema);
