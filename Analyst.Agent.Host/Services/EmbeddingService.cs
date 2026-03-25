using Azure.AI.OpenAI;
using Analyst.Agent.Host.Models;
using OpenAI;

namespace Analyst.Agent.Host.Services;

/// <summary>
/// Generates embeddings using Azure OpenAI's embedding endpoint via OpenAIClient.
/// </summary>
public class EmbeddingService
{
    private readonly AzureOpenAIClient _openAiClient;
    private readonly IConfiguration _config;

    public EmbeddingService(AzureOpenAIClient openAiClient, IConfiguration config)
    {
        _openAiClient = openAiClient;
        _config = config;
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> chunks)
    {
        var results = new List<float[]>();
        var embedDeployment = _config.GetValue<string>("AzureOpenAI:EmbeddingDeployment");
        var client = _openAiClient.GetEmbeddingClient(embedDeployment);
        var response = await client.GenerateEmbeddingsAsync(chunks);
        var data = response.Value;
        if (data != null)
        {
            results.AddRange(data.Select(x=> x.ToFloats().ToArray()));
        }
        else
        {
            results.Add(Array.Empty<float>());
        }
        return results;
    }
}
