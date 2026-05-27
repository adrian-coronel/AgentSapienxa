using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Domain.Agents;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgentSapienxa.Application.Agents;

public class GeneralAgent : IAgent
{
    private static readonly HashSet<string> AllowedToolNames =
    [
        "get_catalog", "get_catalog_item", "capture_lead",
        "register_enrollment", "escalate_to_human"
    ];

    private readonly ILlmProvider _llm;
    private readonly IReadOnlyList<IAgentTool> _tools;
    private readonly ILogger<GeneralAgent> _logger;

    public GeneralAgent(ILlmProvider llm, IEnumerable<IAgentTool> tools, ILogger<GeneralAgent> logger)
    {
        _llm = llm;
        _tools = tools.Where(t => IsAllowed(t)).ToList();
        _logger = logger;
    }

    protected virtual bool IsAllowed(IAgentTool tool) => AllowedToolNames.Contains(tool.Name);

    public async Task<string> RunAsync(
        AgentContext context,
        IReadOnlyList<LlmMessage> history,
        AgentConfig config,
        CancellationToken ct = default)
    {
        var messages = new List<LlmMessage>(history);
        var toolDefs = _tools.Select(t => new LlmToolDefinition(t.Name, t.Description, t.JsonSchema)).ToList();
        const int maxIterations = 6;

        for (int i = 0; i < maxIterations; i++)
        {
            var response = await _llm.CompleteAsync(
                config.Model, config.Temperature, config.SystemPrompt, messages, toolDefs, ct);

            // Llama models sometimes embed tool calls in content text instead of tool_calls field.
            // Detect and normalize both formats so tools always execute.
            var toolCalls = response.ToolCalls.Count > 0
                ? response.ToolCalls
                : ExtractNativeToolCalls(response.Content);
            var content = StripFunctionTags(response.Content);

            if (toolCalls.Count == 0)
            {
                return string.IsNullOrWhiteSpace(content)
                    ? "Lo siento, no pude procesar tu consulta. Por favor intenta de nuevo."
                    : content;
            }

            var tcJson = JsonSerializer.Serialize(
                toolCalls.Select(tc => new { tc.Id, tc.Name, tc.Arguments }));
            messages.Add(new LlmMessage("assistant", content, tcJson));

            foreach (var toolCall in toolCalls)
            {
                var tool = _tools.FirstOrDefault(t => t.Name == toolCall.Name);
                string result;
                if (tool is not null)
                {
                    _logger.LogInformation("[Agent] Executing tool {Tool} for {Session}", toolCall.Name, context.SessionId);
                    result = await tool.ExecuteAsync(toolCall.Arguments, context, ct);
                }
                else
                {
                    _logger.LogWarning("[Agent] Unknown tool requested: {Tool}", toolCall.Name);
                    result = $"{{\"error\": \"tool '{toolCall.Name}' not found\"}}";
                }

                messages.Add(new LlmMessage("tool", result, toolCall.Id));
            }
        }

        _logger.LogWarning("[Agent] Max iterations reached for session {Session}", context.SessionId);
        return "Lo siento, no pude completar tu solicitud. Un asesor te contactará pronto.";
    }

    private static readonly Regex NativeFuncRegex =
        new(@"<function=(\w+)>(.*?)</function>", RegexOptions.Singleline | RegexOptions.Compiled);

    private static IReadOnlyList<LlmToolCall> ExtractNativeToolCalls(string content)
    {
        var calls = new List<LlmToolCall>();
        foreach (Match m in NativeFuncRegex.Matches(content))
            calls.Add(new LlmToolCall($"native-{Guid.NewGuid():N}", m.Groups[1].Value, m.Groups[2].Value));
        return calls;
    }

    private static string StripFunctionTags(string content) =>
        NativeFuncRegex.Replace(content, "").Trim();
}
