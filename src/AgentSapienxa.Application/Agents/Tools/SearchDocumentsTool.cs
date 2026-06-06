using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Documents.Repositories;
using System.Text.Json;

namespace AgentSapienxa.Application.Agents.Tools;

public class SearchDocumentsTool : IAgentTool
{
    private readonly IDocumentChunkRepository _chunks;
    private readonly IEmbeddingProvider _embedding;

    public SearchDocumentsTool(IDocumentChunkRepository chunks, IEmbeddingProvider embedding)
    {
        _chunks = chunks;
        _embedding = embedding;
    }

    public string Name => "search_documents";
    public string Description => "Busca en los documentos de la empresa información relevante. Úsala cuando el usuario pregunte algo que podría estar en documentos internos, políticas, guías o material adicional.";
    public string JsonSchema => """
        {
          "type": "object",
          "properties": {
            "query": {
              "type": "string",
              "description": "La pregunta o tema a buscar en los documentos"
            }
          },
          "required": ["query"]
        }
        """;

    public async Task<string> ExecuteAsync(string arguments, AgentContext context, CancellationToken ct)
    {
        if (context.CompanyId == Guid.Empty)
            return """{"error": "No hay empresa activa para buscar documentos."}""";

        Args? args = null;
        try { args = JsonSerializer.Deserialize<Args>(arguments, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); } catch { }

        var query = args?.Query;
        if (string.IsNullOrWhiteSpace(query))
            return """{"error": "El parámetro 'query' es requerido."}""";

        const int topK = 5;

        var vector = await _embedding.EmbedAsync(query, ct);
        var results = await _chunks.SearchByVectorAsync(context.CompanyId, vector, topK, ct);

        if (results.Count == 0)
            return """{"results": [], "message": "No se encontraron documentos relevantes."}""";

        var payload = results.Select(r => new
        {
            document_id = r.DocumentId,
            header_path = r.HeaderPath,
            content = r.Content,
            similarity = Math.Round(r.Similarity, 4)
        });

        return JsonSerializer.Serialize(new { results = payload });
    }

    private record Args(string? Query);
}
