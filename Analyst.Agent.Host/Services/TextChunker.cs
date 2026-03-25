using System.Text;
using System.Text.RegularExpressions;

namespace Analyst.Agent.Host.Services;

/// <summary>
/// Faster text chunker: splits text into sentences and accumulates sentences
/// until approximately targetWordCount is reached. This reduces memory churn
/// compared to splitting the entire text into words first.
/// </summary>
public class TextChunker
{
    /// <summary>
    /// Split text into chunks of approximately targetWordCount words with overlapCount words overlap.
    /// Uses sentence-based accumulation for better performance and smaller intermediate allocations.
    /// </summary>
    public List<string> ChunkText(string text, int targetWordCount = 400, int overlapCount = 50)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        // Split on sentence boundaries (very simple heuristic).
        // Keep punctuation with sentence to avoid joining broken fragments.
        var sentences = Regex.Split(text, @"(?<=[\.!\?])\s+")
                             .Where(s => !string.IsNullOrWhiteSpace(s))
                             .ToArray();

        var chunks = new List<string>();
        var current = new StringBuilder();
        int currentWords = 0;

        // Helper to count words in a short sentence (cheap)
        static int CountWords(string s) => s.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        for (int i = 0; i < sentences.Length; i++)
        {
            var sent = sentences[i];
            var w = CountWords(sent);

            // If a sentence is huge (e.g. generated code without punctuation), split it by approximate word windows.
            if (w > targetWordCount * 2)
            {
                var words = sent.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int si = 0;
                while (si < words.Length)
                {
                    int take = Math.Min(targetWordCount - currentWords, words.Length - si);
                    if (take <= 0)
                    {
                        // flush current
                        if (currentWords > 0)
                        {
                            chunks.Add(current.ToString());
                            // prepare overlap
                            var lastWords = GetLastNWords(current.ToString(), overlapCount);
                            current.Clear();
                            current.Append(lastWords);
                            currentWords = CountWords(lastWords);
                        }
                        take = Math.Min(targetWordCount, words.Length - si);
                    }

                    // append words[si..si+take]
                    for (int j = 0; j < take; j++)
                    {
                        if (current.Length > 0 && !char.IsWhiteSpace(current[^1])) current.Append(' ');
                        current.Append(words[si + j]);
                    }

                    currentWords += take;
                    si += take;

                    if (currentWords >= targetWordCount)
                    {
                        chunks.Add(current.ToString());
                        var lastWords = GetLastNWords(current.ToString(), overlapCount);
                        current.Clear();
                        current.Append(lastWords);
                        currentWords = CountWords(lastWords);
                    }
                }

                continue;
            }

            // normal sentence append
            if (current.Length > 0 && !char.IsWhiteSpace(current[^1])) current.Append(' ');
            current.Append(sent);
            currentWords += w;

            if (currentWords >= targetWordCount)
            {
                // flush
                chunks.Add(current.ToString());
                var lastWords = GetLastNWords(current.ToString(), overlapCount);
                current.Clear();
                current.Append(lastWords);
                currentWords = CountWords(lastWords);
            }
        }

        if (current.Length > 0)
        {
            chunks.Add(current.ToString());
        }

        return chunks;

        static string GetLastNWords(string s, int n)
        {
            if (string.IsNullOrWhiteSpace(s) || n <= 0) return string.Empty;
            var words = s.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (n >= words.Length) return string.Join(' ', words);
            return string.Join(' ', words.Skip(words.Length - n));
        }
    }
}