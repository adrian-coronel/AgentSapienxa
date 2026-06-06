namespace AgentSapienxa.Domain.Documents;

public static class DocumentStatus
{
    public const string Pending = "Pending";   // Subido, esperando que el usuario dispare
    public const string Queued = "Queued";     // Usuario disparó, worker lo recogerá
    public const string Parsing = "Parsing";
    public const string Chunking = "Chunking";
    public const string Embedding = "Embedding";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Paused = "Paused";
}
