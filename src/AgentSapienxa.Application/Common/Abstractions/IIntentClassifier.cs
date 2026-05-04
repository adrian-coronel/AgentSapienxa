namespace AgentSapienxa.Application.Common.Abstractions;

public interface IIntentClassifier
{
    Task<string> ClassifyAsync(string userMessage, CancellationToken ct = default);
}

public static class Intent
{
    public const string General = "general";
    public const string Enrollment = "enrollment";
    public const string Payment = "payment";
    public const string Escalate = "escalate";
}
