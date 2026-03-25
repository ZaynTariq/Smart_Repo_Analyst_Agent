using Analyst.Agent.Host.Models;

namespace Analyst.Agent.Host.Services;

/// <summary>
/// Agent orchestration: runs the overall workflow.
/// 1. Fetch repo
/// 2. Chunk
/// 3. Embed
/// 4. Store
/// 5. RAG -> answer
/// 6. Reflect -> improved
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

    public async Task<AgentResult> AnalyzeRepositoryAsync(string repoUrl)
    {
        // 1. fetch
        var repo = await _repoService.FetchRepositoryAsync(repoUrl);

        // 2. chunk
        var chunks = _chunker.ChunkText(repo.Combined ?? string.Empty);

        // 3. embeddings
        _vectorStore.Clear();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunks);
        for (int i = 0; i < chunks.Count; i++)
        {
            _vectorStore.Add(chunks[i], embeddings[i]);
        }

        // 4. run rag
        var answer = await _ragService.GenerateAnswerAsync("Analyze this repository", topK: 3);

        // 5. reflection
        var improved = await _reflectionService.ReflectAsync(answer);

        // 6. evaluation
        var (score, reason) = await _evaluationService.EvaluateAsync(improved);

        return new AgentResult
        {
            OriginalAnswer = answer,
            ImprovedAnswer = improved,
            Score = score,
            EvaluationReason = reason
        };
    }
}
