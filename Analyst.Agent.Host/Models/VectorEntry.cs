namespace Analyst.Agent.Host.Models;

public class VectorEntry
{
    public string Text { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
}
