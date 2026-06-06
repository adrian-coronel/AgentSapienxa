using AgentSapienxa.Application.Agents;
using AgentSapienxa.Application.Agents.Repositories;
using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Companies.Repositories;
using AgentSapienxa.Application.Conversations.Repositories;
using AgentSapienxa.Application.Leads.Commands.CaptureLead;
using AgentSapienxa.Domain.Agents;
using AgentSapienxa.Domain.Conversations;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AgentSapienxa.Application.Webhooks;

public class ProcessIncomingMessageHandler : IRequestHandler<ProcessIncomingMessageCommand, Unit>
{
    private readonly IMediator _mediator;
    private readonly IAgentConfigRepository _agentConfigs;
    private readonly IConversationRepository _conversations;
    private readonly IIntentClassifier _intentClassifier;
    private readonly IAgentRouter _router;
    private readonly IMessagingChannel _messaging;
    private readonly ICurrentCompanyAccessor _companyAccessor;
    private readonly ICompanyRepository _companies;
    private readonly ILogger<ProcessIncomingMessageHandler> _logger;

    public ProcessIncomingMessageHandler(
        IMediator mediator,
        IAgentConfigRepository agentConfigs,
        IConversationRepository conversations,
        IIntentClassifier intentClassifier,
        IAgentRouter router,
        IMessagingChannel messaging,
        ICurrentCompanyAccessor companyAccessor,
        ICompanyRepository companies,
        ILogger<ProcessIncomingMessageHandler> logger)
    {
        _mediator = mediator;
        _agentConfigs = agentConfigs;
        _conversations = conversations;
        _intentClassifier = intentClassifier;
        _router = router;
        _messaging = messaging;
        _companyAccessor = companyAccessor;
        _companies = companies;
        _logger = logger;
    }

    public async Task<Unit> Handle(ProcessIncomingMessageCommand cmd, CancellationToken ct)
    {
        var msg = cmd.Message;
        var userText = msg.Text ?? msg.Caption ?? "[media sin texto]";

        _logger.LogInformation("[Orchestrator] {Session} → {Text}", msg.SessionId, userText[..Math.Min(80, userText.Length)]);

        // 1. Ensure lead exists (creates if new, updates name if known); resolve company context
        var captureResult = await _mediator.Send(
            new CaptureLeadCommand(msg.PhoneE164, msg.ContactName, null, "WhatsApp"), ct);

        Guid? resolvedCompanyId = captureResult.CompanyId != Guid.Empty
            ? captureResult.CompanyId
            : _companyAccessor.CompanyId;

        // Fallback: si el lead no tiene empresa y el JWT tampoco, usar la primera empresa activa (simulación / onboarding)
        if (resolvedCompanyId is null || resolvedCompanyId.Value == Guid.Empty)
        {
            var companies = await _companies.GetAllAsync(ct);
            var defaultCompany = companies.FirstOrDefault();
            if (defaultCompany is not null)
            {
                resolvedCompanyId = defaultCompany.Id;
                _logger.LogDebug("[Orchestrator] No company in lead/JWT — using default company {Id}", resolvedCompanyId);
            }
        }

        if (resolvedCompanyId is { } cid && cid != Guid.Empty)
            _companyAccessor.CompanyId = cid;

        // 2. Load general agent config (used as default)
        var generalConfig = await _agentConfigs.GetByKeyAsync(AgentKey.CursosGeneral.Value, ct);
        if (generalConfig is null)
        {
            _logger.LogError("[Orchestrator] AgentConfig '{Key}' not found in DB. Run the seeder.", AgentKey.CursosGeneral.Value);
            return Unit.Value;
        }

        // 3. Load conversation history (last N messages)
        var dbHistory = await _conversations.GetHistoryAsync(msg.SessionId, generalConfig.MemoryWindow, ct);
        var history = dbHistory
            .Select(m => new LlmMessage(m.Role, m.Content, m.ToolCalls))
            .ToList();

        // 4. Add current user message to in-memory history
        history.Add(new LlmMessage(ConversationRole.User, userText));

        // 5. Persist user message
        await _conversations.AddAsync(
            ConversationMessage.Create(msg.SessionId, ConversationRole.User, userText), ct);

        // 6. Classify intent
        var intent = await _intentClassifier.ClassifyAsync(userText, ct);

        // 7. Pick agent config (payment agent has its own prompt)
        AgentConfig agentConfig = generalConfig;
        if (intent == Intent.Payment)
        {
            var paymentConfig = await _agentConfigs.GetByKeyAsync(AgentKey.CursosPagos.Value, ct);
            if (paymentConfig is not null) agentConfig = paymentConfig;
        }

        // 8. Route and run agent
        var context = new AgentContext
        {
            SessionId = msg.SessionId,
            ContactName = msg.ContactName,
            CompanyId = resolvedCompanyId ?? Guid.Empty
        };
        var agent = _router.Route(intent);
        var response = await agent.RunAsync(context, history, agentConfig, ct);

        // 9. Persist assistant response
        await _conversations.AddAsync(
            ConversationMessage.Create(msg.SessionId, ConversationRole.Assistant, response), ct);

        // 10. Send reply to user
        await _messaging.SendTextAsync(msg.SessionId, response, ct);

        return Unit.Value;
    }
}
