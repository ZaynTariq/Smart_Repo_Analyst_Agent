using Analyst.Agent.Host.Models;

namespace Analyst.Agent.Host.Services;

/// <summary>
/// Agent orchestration: runs the overall workflow.
/// 1. Fetch repo
/// 2. Chunk
/// 3. Embed
/// 4. Store
/// 5. RAG -> answer
/// 6. Reflect -> improved (loop until score >= 8)
/// 7. Evaluate -> score
/// </summary>
public class AgentService
{
    private readonly RepoService _repoService;
    private readonly TextChunker _chunker;
    private readonly EmbeddingService _embeddingService;
    private readonly VectorStore _vectorStore;
    private readonly RagService _ragService;
    private readonly ReflectionService _reflectionService;
    private readonly EvaluationService _evaluationService;

    public AgentService(RepoService repoService, TextChunker chunker, EmbeddingService embeddingService, VectorStore vectorStore, RagService ragService, ReflectionService reflectionService, EvaluationService evaluationService)
    {
        _repoService = repoService;
        _chunker = chunker;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _ragService = ragService;
        _reflectionService = reflectionService;
        _evaluationService = evaluationService;
    }

    /// <summary>
    /// Analyze repository. statusCallback receives progress messages. githubToken optional for private repos.
    /// </summary>
    public async Task<AgentResult> AnalyzeRepositoryAsync(string repoUrl, Action<string>? statusCallback = null, string? githubToken = null)
    {
        statusCallback?.Invoke("Starting repository fetch...");
        var repo = await _repoService.FetchRepositoryAsync(repoUrl, maxFiles: 20, githubToken: githubToken);

        statusCallback?.Invoke("Chunking repository content...");
        var chunks = _chunker.ChunkText(repo.Combined ?? string.Empty);
        statusCallback?.Invoke($"Created {chunks.Count} chunks.");

        statusCallback?.Invoke("Generating embeddings (batched)...");
        _vectorStore.Clear();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunks);
        for (int i = 0; i < chunks.Count; i++)
        {
            var emb = (i < embeddings.Count) ? embeddings[i] : Array.Empty<float>();
            _vectorStore.Add(chunks[i], emb);
        }
        statusCallback?.Invoke("Embeddings stored in vector store.");

        statusCallback?.Invoke("Running initial RAG to produce answer...");
        var originalAnswer = await _ragService.GenerateAnswerAsync("Analyze this repository", topK: 3);

        statusCallback?.Invoke("Evaluating initial answer...");
        var (score, reason) = await _evaluation_service_EvaluateAsync(originalAnswer);

        // Loop: reflect and re-evaluate until score >= 8 or max iterations reached
        int iter = 0;
        const int maxIter = 5;
        var currentAnswer = originalAnswer;
        while (score < 8 && iter < maxIter)
        {
            iter++;
            statusCallback?.Invoke($"Reflection iteration {iter}: improving answer...");
            currentAnswer = await _reflectionService.ReflectAsync(currentAnswer);

            statusCallback?.Invoke($"Re-evaluating after reflection (iteration {iter})...");
            (score, reason) = await _evaluation_service_EvaluateAsync(currentAnswer);
            statusCallback?.Invoke($"Score after iteration {iter}: {score}");
        }

        statusCallback?.Invoke("Finished processing.");

        return new AgentResult
        {
            OriginalAnswer = originalAnswer,
            ImprovedAnswer = currentAnswer,
            Score = score,
            EvaluationReason = reason
        };

        // Local helper to call evaluation service and handle exceptions
        async Task<(int, string)> _evaluation_service_EvaluateAsync(string ans)
        {
            try
            {
                return await _evaluationService.EvaluateAsync(ans);
            }
            catch (Exception ex)
            {
                statusCallback?.Invoke($"Evaluation failed: {ex.Message}");
                return (0, $"Evaluation failed: {ex.Message}");
            }
        }
    }
}
