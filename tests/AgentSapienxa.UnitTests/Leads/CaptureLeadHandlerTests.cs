using AgentSapienxa.Application.Leads.Commands.CaptureLead;
using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Domain.Leads;
using FluentAssertions;
using Moq;

namespace AgentSapienxa.UnitTests.Leads;

public class CaptureLeadHandlerTests
{
    private readonly Mock<ILeadRepository> _leads = new();
    private readonly Mock<ISalesAgentRepository> _agents = new();
    private readonly CaptureLeadHandler _handler;

    public CaptureLeadHandlerTests()
    {
        _handler = new CaptureLeadHandler(_leads.Object, _agents.Object);
    }

    [Fact]
    public async Task Handle_NewLead_CreatesAndAssignsAgent()
    {
        _leads.Setup(r => r.GetByPhoneNumberAsync("+51987654321", default)).ReturnsAsync((Lead?)null);

        var agent = new SalesAgentBuilder().Build();
        _agents.Setup(r => r.GetFirstAvailableAsync(default)).ReturnsAsync(agent);

        var result = await _handler.Handle(new CaptureLeadCommand("+51987654321", "Juan", "juan@test.com", "WhatsApp"), default);

        result.IsNew.Should().BeTrue();
        result.LeadId.Should().NotBeEmpty();
        _leads.Verify(r => r.AddAsync(It.Is<Lead>(l => l.SalesAgentId == agent.Id), default), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingLead_UpdatesInfo()
    {
        var existing = Lead.Create("+51987654321", "Viejo Nombre", "viejo@test.com", "WhatsApp");
        _leads.Setup(r => r.GetByPhoneNumberAsync("+51987654321", default)).ReturnsAsync(existing);

        var result = await _handler.Handle(new CaptureLeadCommand("+51987654321", "Nuevo Nombre", "nuevo@test.com", null), default);

        result.IsNew.Should().BeFalse();
        result.LeadId.Should().Be(existing.Id);
        _leads.Verify(r => r.UpdateAsync(existing, default), Times.Once);
        _leads.Verify(r => r.AddAsync(It.IsAny<Lead>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_NewLeadWithoutAgents_StillCreatesLead()
    {
        _leads.Setup(r => r.GetByPhoneNumberAsync("+51987654321", default)).ReturnsAsync((Lead?)null);
        _agents.Setup(r => r.GetFirstAvailableAsync(default)).ReturnsAsync((SalesAgent?)null);

        var result = await _handler.Handle(new CaptureLeadCommand("+51987654321", null, null, null), default);

        result.IsNew.Should().BeTrue();
        _leads.Verify(r => r.AddAsync(It.Is<Lead>(l => l.SalesAgentId == null), default), Times.Once);
    }
}

file class SalesAgentBuilder
{
    public SalesAgent Build()
    {
        var type = typeof(SalesAgent);
        var agent = (SalesAgent)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(type)!;
        type.GetProperty("Id")!.SetValue(agent, Guid.NewGuid());
        type.GetProperty("AgentName")!.SetValue(agent, "Agente Test");
        type.GetProperty("Email")!.SetValue(agent, "agente@test.com");
        return agent;
    }
}
