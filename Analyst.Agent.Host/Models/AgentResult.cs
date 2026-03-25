namespace Analyst.Agent.Host.Models;

public class AgentResult
{
    public string OriginalAnswer { get; set; } = string.Empty;
    public string ImprovedAnswer { get; set; } = string.Empty;
    public int Score { get; set; }
    public string EvaluationReason { get; set; } = string.Empty;
}
