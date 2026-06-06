using AgentSapienxa.Domain.Common;

namespace AgentSapienxa.Domain.Documents;

public class DocumentChunk : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid CompanyId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string? HeaderPath { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int TokenCount { get; private set; }
    public float[] Embedding { get; private set; } = [];
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private DocumentChunk() { }

    public static DocumentChunk Create(
        Guid documentId,
        Guid companyId,
        int chunkIndex,
        string content,
        int tokenCount,
        float[] embedding,
        string? headerPath = null)
    {
        return new DocumentChunk
        {
            DocumentId = documentId,
            CompanyId = companyId,
            ChunkIndex = chunkIndex,
            Content = content,
            TokenCount = tokenCount,
            Embedding = embedding,
            HeaderPath = headerPath,
            CreatedAt = DateTime.UtcNow
        };
    }
}
