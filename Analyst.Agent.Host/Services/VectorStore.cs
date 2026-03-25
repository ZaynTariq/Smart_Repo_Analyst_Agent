using Analyst.Agent.Host.Models;

namespace Analyst.Agent.Host.Services;

/// <summary>
/// In-memory vector store storing text chunks and corresponding embedding vectors.
/// Provides cosine similarity search to retrieve top K similar chunks.
/// </summary>
public class VectorStore
{
    private readonly List<VectorEntry> _entries = new();

    public void Add(string text, float[] embedding)
    {
        _entries.Add(new VectorEntry { Text = text, Embedding = embedding });
    }

    public void Clear() => _entries.Clear();

    /// <summary>
    /// Convert query embedding and return top K chunks by cosine similarity.
    /// </summary>
    public List<VectorEntry> GetTopKSimilar(float[] queryEmbedding, int k = 3)
    {
        var scored = _entries.Select(e => new { Entry = e, Score = CosineSimilarity(e.Embedding, queryEmbedding) })
            .OrderByDescending(x => x.Score)
            .Take(k)
            .Select(x => x.Entry)
            .ToList();
        return scored;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        if (na == 0 || nb == 0) return 0f;
        return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb)));
    }
}
