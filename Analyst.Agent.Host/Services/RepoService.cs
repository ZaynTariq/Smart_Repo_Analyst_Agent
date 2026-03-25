using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Analyst.Agent.Host.Models;

namespace Analyst.Agent.Host.Services;

/// <summary>
/// Fetches repository files from GitHub using the Contents API.
/// Recurses into directories to find README.md, .cs and .csproj files.
/// Respects maxFiles limit and supports GITHUB_TOKEN environment variable for auth.
/// </summary>
public class RepoService
{
    private readonly HttpClient _http;

    public RepoService(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("SmartRepoAnalystAgent");

        // Optional: support GitHub token via environment variable to avoid rate limits and access private repos.
        var token = "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";
        if (!string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private (string owner, string repo) ParseRepoUrl(string url)
    {
        var m = Regex.Match(url, @"github.com[:/](?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git|/|$)", RegexOptions.IgnoreCase);
        if (!m.Success) throw new ArgumentException("Invalid GitHub repository URL");
        return (m.Groups["owner"].Value, m.Groups["repo"].Value);
    }

    /// <summary>
    /// Fetch README and up to maxFiles of .cs/.csproj files recursively.
    /// </summary>
    public async Task<RepoContent> FetchRepositoryAsync(string repoUrl, int maxFiles = 5)
    {
        var (owner, repo) = ParseRepoUrl(repoUrl);
        var rootApi = $"https://api.github.com/repos/{owner}/{repo}/contents";

        var files = new List<(string path, string downloadUrl)>();
        string readme = string.Empty;

        var toVisit = new Queue<string>();
        toVisit.Enqueue(rootApi);

        while (toVisit.Count > 0 && files.Count < maxFiles)
        {
            var api = toVisit.Dequeue();
            using var resp = await _http.GetAsync(api);
            if (!resp.IsSuccessStatusCode)
            {
                // skip on errors, continue
                continue;
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // The API returns either an array (directory) or an object (file)
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    var type = item.GetProperty("type").GetString();
                    var name = item.GetProperty("name").GetString() ?? string.Empty;

                    if (type == "dir")
                    {
                        var url = item.GetProperty("url").GetString();
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            toVisit.Enqueue(url);
                        }
                    }
                    else if (type == "file")
                    {
                        // prefer README anywhere
                        var downloadUrl = item.GetProperty("download_url").GetString();
                        if (name.Equals("README.md", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(downloadUrl))
                        {
                            // if README is large, fetch later once
                            if (string.IsNullOrEmpty(readme))
                            {
                                try
                                {
                                    readme = await _http.GetStringAsync(downloadUrl);
                                }
                                catch { /* ignore fetch errors and continue */ }
                            }
                        }
                        else if ((name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                                  name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) &&
                                  !string.IsNullOrWhiteSpace(downloadUrl))
                        {
                            files.Add((item.GetProperty("path").GetString() ?? name, downloadUrl));
                            if (files.Count >= maxFiles) break;
                        }
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // single file
                var type = root.GetProperty("type").GetString();
                var name = root.GetProperty("name").GetString() ?? string.Empty;
                var downloadUrl = root.TryGetProperty("download_url", out var d) ? d.GetString() : null;
                if (type == "file" && !string.IsNullOrWhiteSpace(downloadUrl))
                {
                    if (name.Equals("README.md", StringComparison.OrdinalIgnoreCase))
                    {
                        readme = await _http.GetStringAsync(downloadUrl);
                    }
                    else if (name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        files.Add((root.GetProperty("path").GetString() ?? name, downloadUrl));
                    }
                }
            }
        }

        // Fetch file contents concurrently but limited to avoid bursts
        var results = new List<string>();
        var sem = new SemaphoreSlim(6); // tune concurrency; 6 is reasonable
        var tasks = files.Select(async f =>
        {
            await sem.WaitAsync();
            try
            {
                return await _http.GetStringAsync(f.downloadUrl);
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                sem.Release();
            }
        }).ToArray();

        var contents = await Task.WhenAll(tasks);
        results.AddRange(contents.Where(c => !string.IsNullOrEmpty(c)));

        var combined = (readme ?? string.Empty) + "\n\n" + string.Join("\n\n", results);

        return new RepoContent
        {
            Owner = owner,
            Name = repo,
            Readme = readme ?? string.Empty,
            Files = results.ToList(),
            Combined = combined
        };
    }
}