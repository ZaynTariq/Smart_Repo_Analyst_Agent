namespace Analyst.Agent.Host.Models;

public class RepoContent
{
    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Readme { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
    public string Combined { get; set; } = string.Empty;
}
