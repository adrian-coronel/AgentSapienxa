using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;

namespace AgentSapienxa.Infrastructure.Documents.Embedding;

public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly EmbeddingClient _client;
    private const int BatchSize = 100;

    public OpenAiEmbeddingProvider(IConfiguration config)
    {
        var apiKey = config["OpenAI:EmbeddingApiKey"] ?? "ollama";
        var model = config["OpenAI:EmbeddingModel"] ?? "nomic-embed-text";
        var baseUrl = config["OpenAI:EmbeddingBaseUrl"];

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(baseUrl))
            clientOptions.Endpoint = new Uri(baseUrl);

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        _client = client.GetEmbeddingClient(model);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var response = await _client.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return response.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var results = new List<float[]>(texts.Count);

        for (int i = 0; i < texts.Count; i += BatchSize)
        {
            var batch = texts.Skip(i).Take(BatchSize).ToList();
            var response = await _client.GenerateEmbeddingsAsync(batch, cancellationToken: ct);
            results.AddRange(response.Value.Select(e => e.ToFloats().ToArray()));
        }

        return results;
    }
}
