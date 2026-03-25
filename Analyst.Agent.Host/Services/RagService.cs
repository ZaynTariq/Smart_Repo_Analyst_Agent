using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using System.IO;
using OpenAI;
using OpenAI.Chat;
using Analyst.Agent.Host.Models;

namespace Analyst.Agent.Host.Services;

/// <summary>
/// RAG service: retrieves top chunks and asks the LLM to produce an analysis.
/// </summary>
public class RagService
{
    private readonly AzureOpenAIClient _openAiClient;
    private readonly VectorStore _vectorStore;
    private readonly string _promptTemplate;
    private readonly IConfiguration _config;

    public RagService(AzureOpenAIClient openAiClient, VectorStore vectorStore, IWebHostEnvironment env, IConfiguration config)
    {
        _openAiClient = openAiClient;
        _vectorStore = vectorStore;
        _config = config;
        // Load prompt from file
        var p = Path.Combine(env.ContentRootPath, "Prompts", "AnalysisPrompt.txt");
        _promptTemplate = File.Exists(p) ? File.ReadAllText(p) : "";
    }

    public async Task<string> GenerateAnswerAsync(string userQuery, int topK = 3)
    {
        // Convert query to embedding using OpenAIClient embeddings
        var embedDeployment = _config.GetValue<string>("AzureOpenAI:EmbeddingDeployment");
        var embeddingCLient = _openAiClient.GetEmbeddingClient(embedDeployment);
        var qResp = await embeddingCLient.GenerateEmbeddingAsync(userQuery);
        var q = qResp.Value;
 
        var entries = _vectorStore.GetTopKSimilar(q.ToFloats().ToArray(), topK);

        var context = string.Join("\n\n---\n\n", entries.Select(e => e.Text));

        var prompt = _promptTemplate.Replace("{{context}}", context);

        // Use chat completions
        var chatDeployment = _config.GetValue<string>("AzureOpenAI:ChatDeployment");
        var chatOptions = new ChatCompletionOptions();

        var client = _openAiClient.GetChatClient(chatDeployment);
        var chatMessages = new List<ChatMessage>()
        {
            new SystemChatMessage(prompt),
            new UserChatMessage(userQuery)
        };
        var response = await client.CompleteChatAsync(chatMessages);

        return response.Value?.Content.FirstOrDefault().Text ?? string.Empty;
    }
}
