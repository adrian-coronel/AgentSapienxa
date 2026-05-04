using AgentSapienxa.Application.Common.Abstractions;

namespace AgentSapienxa.Infrastructure.Time;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
