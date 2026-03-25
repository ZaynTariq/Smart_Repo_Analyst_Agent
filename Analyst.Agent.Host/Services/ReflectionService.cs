using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

namespace Analyst.Agent.Host.Services;

/// <summary>
/// Reflection: asks the model to review the previous answer and improve it if necessary.
/// </summary>
public class ReflectionService
{
    private readonly AzureOpenAIClient _openAiClient;
    private readonly IConfiguration _config;

    public ReflectionService(AzureOpenAIClient openAiClient, IConfiguration config)
    {
        _openAiClient = openAiClient;
        _config = config;
    }

    public async Task<string> ReflectAsync(string originalAnswer)
    {
        var prompt = $"You are reviewing your previous answer.\n\nCheck:\n* Is the answer accurate?\n* Is anything important missing?\n* Is the explanation clear?\n\nIf improvements are needed, rewrite the answer.\nOtherwise, return the original answer.\n\nOriginal Answer:\n{originalAnswer}";

        var chatDeployment = _config.GetValue<string>("AzureOpenAI:ChatDeployment");
        var chatOptions = new ChatCompletionOptions();

        var chatMessages = new List<ChatMessage>()
        {
            new SystemChatMessage(prompt)
        };
        var chatClient = _openAiClient.GetChatClient(chatDeployment);

        var response = await chatClient.CompleteChatAsync(chatMessages);
        
        var choice = response.Value;

        return choice?.Content.FirstOrDefault()!.Text?.ToString()!;
    }
}
