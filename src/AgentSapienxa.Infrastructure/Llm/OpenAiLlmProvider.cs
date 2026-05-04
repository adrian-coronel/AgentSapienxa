using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace AgentSapienxa.Infrastructure.Llm;

public class OpenAiLlmProvider : ILlmProvider
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAiLlmProvider> _logger;

    public OpenAiLlmProvider(OpenAIClient client, ILogger<OpenAiLlmProvider> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<LlmResponse> CompleteAsync(
        string model,
        decimal temperature,
        string systemPrompt,
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition>? tools = null,
        CancellationToken ct = default)
    {
        var chatClient = _client.GetChatClient(model);

        var chatMessages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };
        foreach (var msg in messages)
            chatMessages.Add(ToChatMessage(msg));

        var options = new ChatCompletionOptions { Temperature = (float)temperature };

        if (tools is { Count: > 0 })
        {
            foreach (var t in tools)
                options.Tools.Add(ChatTool.CreateFunctionTool(t.Name, t.Description, BinaryData.FromString(t.JsonSchema)));
        }

        var response = await chatClient.CompleteChatAsync(chatMessages, options, ct);
        var completion = response.Value;

        var toolCalls = completion.ToolCalls
            .Select(tc => new LlmToolCall(tc.Id, tc.FunctionName, tc.FunctionArguments.ToString()))
            .ToList();

        _logger.LogInformation("[LLM] {Model} → in:{In} out:{Out} tools:{Tools}",
            model, completion.Usage?.InputTokenCount, completion.Usage?.OutputTokenCount, toolCalls.Count);

        return new LlmResponse(
            Content: completion.Content.FirstOrDefault()?.Text ?? string.Empty,
            ToolCalls: toolCalls,
            TokensIn: (int?)completion.Usage?.InputTokenCount,
            TokensOut: (int?)completion.Usage?.OutputTokenCount,
            Model: model);
    }

    private static ChatMessage ToChatMessage(LlmMessage msg)
    {
        if (msg.Role == "user")
            return new UserChatMessage(msg.Content);

        if (msg.Role == "tool")
            return new ToolChatMessage(msg.ToolCalls ?? string.Empty, msg.Content);

        if (msg.Role == "assistant")
        {
            if (string.IsNullOrEmpty(msg.ToolCalls))
                return new AssistantChatMessage(msg.Content);

            var parsed = JsonSerializer.Deserialize<List<StoredToolCall>>(msg.ToolCalls);
            if (parsed is null or { Count: 0 })
                return new AssistantChatMessage(msg.Content);

            var chatToolCalls = parsed
                .Select(tc => ChatToolCall.CreateFunctionToolCall(tc.Id, tc.Name, BinaryData.FromString(tc.Arguments)))
                .ToList();
            return new AssistantChatMessage(chatToolCalls);
        }

        return new UserChatMessage(msg.Content);
    }

    private sealed record StoredToolCall(string Id, string Name, string Arguments);
}
