using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Documents;

public class DocumentUpload : Entity
{
    public Guid CompanyId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public string Sha256 { get; private set; } = string.Empty;
    public string Status { get; private set; } = DocumentStatus.Pending;
    public int ProgressPct { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public int? ChunkCount { get; private set; }
    public int? TotalTokens { get; private set; }
    public Guid? UploadedBy { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private DocumentUpload() { }

    public static DocumentUpload Create(
        Guid companyId,
        string fileName,
        long fileSize,
        string contentType,
        string storagePath,
        string sha256,
        Guid? uploadedBy = null)
    {
        return new DocumentUpload
        {
            CompanyId = companyId,
            FileName = fileName,
            FileSize = fileSize,
            ContentType = contentType,
            StoragePath = storagePath,
            Sha256 = sha256,
            UploadedBy = uploadedBy,
            Status = DocumentStatus.Pending,
            ProgressPct = 0,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsQueued()
    {
        Status = DocumentStatus.Queued;
        ProgressPct = 0;
        ErrorMessage = null;
        RetryCount = 0;
        StartedAt = null;
    }

    public void MarkAsParsing()
    {
        Status = DocumentStatus.Parsing;
        StartedAt = DateTime.UtcNow;
        ErrorMessage = null;
        UpdateProgress(10);
    }

    public void MarkAsChunking() => Status = DocumentStatus.Chunking;

    public void MarkAsEmbedding() => Status = DocumentStatus.Embedding;

    public void MarkAsCompleted(int chunkCount, int totalTokens)
    {
        Status = DocumentStatus.Completed;
        ChunkCount = chunkCount;
        TotalTokens = totalTokens;
        ProgressPct = 100;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsPaused()
    {
        Status = DocumentStatus.Paused;
    }

    public void MarkAsResumed()
    {
        // Resume va directo a Queued — el worker lo recoge sin que el usuario tenga que hacer otro click
        Status = DocumentStatus.Queued;
        ProgressPct = 0;
        StartedAt = null;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        // Con el flujo manual el worker no auto-reintenta — siempre queda Failed
        // y el usuario decide si reintenta desde la UI
        Status = DocumentStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        ProgressPct = 0;
        CompletedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(int pct) => ProgressPct = Math.Clamp(pct, 0, 100);
}
