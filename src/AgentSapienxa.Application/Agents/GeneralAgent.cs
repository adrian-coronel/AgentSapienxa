using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Domain.Agents;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

            if (response.ToolCalls.Count == 0)
            {
                return string.IsNullOrWhiteSpace(response.Content)
                    ? "Lo siento, no pude procesar tu consulta. Por favor intenta de nuevo."
                    : response.Content;
            }

            // Persist assistant turn with tool calls
            var tcJson = JsonSerializer.Serialize(
                response.ToolCalls.Select(tc => new { tc.Id, tc.Name, tc.Arguments }));
            messages.Add(new LlmMessage("assistant", response.Content, tcJson));

            // Execute tools and collect results
            foreach (var toolCall in response.ToolCalls)
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

                // ToolCalls field stores the tool_call_id when role is "tool"
                messages.Add(new LlmMessage("tool", result, toolCall.Id));
            }
        }

        _logger.LogWarning("[Agent] Max iterations reached for session {Session}", context.SessionId);
        return "Lo siento, no pude completar tu solicitud. Un asesor te contactará pronto.";
    }
}
