using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Documents.Repositories;
using AgentSapienxa.Domain.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgentSapienxa.Infrastructure.Documents.Processing;

public class DocumentProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DocumentProcessingWorker> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public DocumentProcessingWorker(IServiceProvider services, ILogger<DocumentProcessingWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[DocWorker] Starting document processing worker.");

        // Resetear docs que quedaron colgados de una sesión anterior (Queued/Parsing/Chunking/Embedding → Pending)
        // para que el usuario los procese manualmente en el nuevo flujo
        try
        {
            using var startupScope = _services.CreateScope();
            var startupUploads = startupScope.ServiceProvider.GetRequiredService<IDocumentUploadRepository>();
            await startupUploads.ResetStuckDocumentsAsync(stoppingToken);
            _logger.LogInformation("[DocWorker] Stuck documents reset to Pending.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DocWorker] Could not reset stuck documents on startup.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[DocWorker] Unexpected error in processing loop.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessNextAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var uploads = scope.ServiceProvider.GetRequiredService<IDocumentUploadRepository>();

        var upload = await uploads.GetNextQueuedAsync(ct);
        if (upload is null) return;

        _logger.LogInformation("[DocWorker] Picked up doc {Id} ({FileName}) — DB status: {Status}", upload.Id, upload.FileName, upload.Status);

        if (upload.Status != DocumentStatus.Queued)
        {
            _logger.LogWarning("[DocWorker] Expected Queued but got {Status} — skipping doc {Id}", upload.Status, upload.Id);
            return;
        }

        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var converter = scope.ServiceProvider.GetRequiredService<IDocumentToMarkdownConverter>();
        var chunker = scope.ServiceProvider.GetRequiredService<ISemanticChunker>();
        var embedding = scope.ServiceProvider.GetRequiredService<IEmbeddingProvider>();
        var chunks = scope.ServiceProvider.GetRequiredService<IDocumentChunkRepository>();

        try
        {
            // Step 1: Parse to Markdown
            upload.MarkAsParsing();
            await uploads.UpdateAsync(upload, ct);

            await using var fileStream = await storage.OpenAsync(upload.StoragePath, ct);
            var markdown = await converter.ConvertAsync(fileStream, upload.ContentType, ct);
            upload.UpdateProgress(40);
            await uploads.UpdateAsync(upload, ct);

            if (await ShouldStopAsync(uploads, upload.Id, ct)) return;

            // Step 2: Chunk
            upload.MarkAsChunking();
            await uploads.UpdateAsync(upload, ct);

            var chunkDrafts = await chunker.ChunkAsync(markdown, ct);
            upload.UpdateProgress(60);
            await uploads.UpdateAsync(upload, ct);

            if (await ShouldStopAsync(uploads, upload.Id, ct)) return;

            // Step 3: Embed
            upload.MarkAsEmbedding();
            await uploads.UpdateAsync(upload, ct);

            // Filter out any empty chunks before embedding (PDF pages without text, etc.)
            var validChunks = chunkDrafts
                .Where(c => !string.IsNullOrWhiteSpace(c.Content))
                .ToList();

            if (validChunks.Count == 0)
                throw new InvalidOperationException(
                    "No se encontró texto en el documento. Si es un PDF escaneado o contiene solo imágenes, necesita OCR para poder procesarlo.");

            var texts = validChunks.Select(c => c.Content).ToList();
            var embeddings = await embedding.EmbedBatchAsync(texts, ct);

            int totalTokens = validChunks.Sum(c => c.TokenCount);
            int progress = 60;
            int progressPerBatch = validChunks.Count > 0 ? 35 / Math.Max(1, validChunks.Count / 10) : 35;

            var documentChunks = validChunks.Select((draft, i) => DocumentChunk.Create(
                documentId: upload.Id,
                companyId: upload.CompanyId,
                chunkIndex: i,
                content: draft.Content,
                tokenCount: draft.TokenCount,
                embedding: embeddings[i],
                headerPath: draft.HeaderPath)).ToList();

            // Step 4: Persist all chunks in batch
            await chunks.AddBatchAsync(documentChunks, ct);

            progress = 95;
            upload.UpdateProgress(progress);
            await uploads.UpdateAsync(upload, ct);

            // Step 5: Mark complete
            upload.MarkAsCompleted(documentChunks.Count, totalTokens);
            await uploads.UpdateAsync(upload, ct);

            _logger.LogInformation("[DocWorker] Document {Id} completed: {Chunks} chunks, {Tokens} tokens",
                upload.Id, documentChunks.Count, totalTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DocWorker] Failed to process document {Id}", upload.Id);
            upload.MarkAsFailed(ex.Message[..Math.Min(500, ex.Message.Length)]);
            await uploads.UpdateAsync(upload, ct);
        }
    }

    // Retorna true si el worker debe abandonar limpiamente (pausado o eliminado)
    private static async Task<bool> ShouldStopAsync(IDocumentUploadRepository uploads, Guid id, CancellationToken ct)
    {
        var current = await uploads.GetByIdAsync(id, ct);
        return current is null || current.Status == DocumentStatus.Paused;
    }
}
