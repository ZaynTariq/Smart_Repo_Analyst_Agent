using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

namespace Analyst.Agent.Host.Services;

/// <summary>
/// Simple evaluation scoring using the LLM to rate the answer.
/// </summary>
public class EvaluationService
{
    private readonly AzureOpenAIClient _openAiClient;
    private readonly IConfiguration _config;

    public EvaluationService(AzureOpenAIClient openAiClient, IConfiguration config)
    {
        _openAiClient = openAiClient;
        _config = config;
    }

    public async Task<(int score, string reason)> EvaluateAsync(string answer)
    {
        var prompt = $"Rate the following answer from 1 to 10 based on:\n* Accuracy\n* Completeness\n* Clarity\n\nAlso provide a short reason.\n\nAnswer:\n{answer}";

        var chatDeployment = _config.GetValue<string>("AzureOpenAI:ChatDeployment");
        var chatOptions = new ChatCompletionOptions();

        var client = _openAiClient.GetChatClient(chatDeployment);
        var messages = new List<ChatMessage>()
        {
            new SystemChatMessage(prompt)
        };
        var response = await client.CompleteChatAsync(messages);
        var content = response.Value.Content.FirstOrDefault()!.Text ?? string.Empty;

        // Try to parse leading score from content
        var m = System.Text.RegularExpressions.Regex.Match(content, "(\\d{1,2})");
        int score = 0;
        if (m.Success && int.TryParse(m.Groups[1].Value, out var s)) score = Math.Clamp(s, 1, 10);

        return (score, content);
    }
}
