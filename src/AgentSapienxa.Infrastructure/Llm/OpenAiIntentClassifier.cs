using AgentSapienxa.Application.Agents.Repositories;
using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Domain.Agents;
using Microsoft.Extensions.Logging;

namespace AgentSapienxa.Infrastructure.Llm;

public class OpenAiIntentClassifier : IIntentClassifier
{
    private readonly IAgentConfigRepository _configs;
    private readonly ILlmProvider _llm;
    private readonly ILogger<OpenAiIntentClassifier> _logger;

    public OpenAiIntentClassifier(IAgentConfigRepository configs, ILlmProvider llm, ILogger<OpenAiIntentClassifier> logger)
    {
        _configs = configs;
        _llm = llm;
        _logger = logger;
    }

    public async Task<string> ClassifyAsync(string userMessage, CancellationToken ct = default)
    {
        var config = await _configs.GetByKeyAsync(AgentKey.CursosIntent.Value, ct);
        if (config is null)
        {
            _logger.LogWarning("[Intent] AgentConfig '{Key}' not found — defaulting to 'general'", AgentKey.CursosIntent.Value);
            return Intent.General;
        }

        var response = await _llm.CompleteAsync(
            config.Model,
            temperature: 0m,
            config.SystemPrompt,
            messages: [new LlmMessage("user", userMessage)],
            tools: null,
            ct);

        var intent = response.Content.Trim().ToLowerInvariant();

        _logger.LogInformation("[Intent] '{Preview}' → '{Intent}'",
            userMessage[..Math.Min(60, userMessage.Length)], intent);

        return intent switch
        {
            Intent.Enrollment => Intent.Enrollment,
            Intent.Payment => Intent.Payment,
            Intent.Escalate => Intent.Escalate,
            _ => Intent.General
        };
    }
}
