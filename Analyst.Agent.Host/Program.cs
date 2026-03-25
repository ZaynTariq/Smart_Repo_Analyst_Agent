using Analyst.Agent.Host.Components;
using Analyst.Agent.Host.Services;
using Azure;
using Azure.AI.OpenAI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

// Load Azure OpenAI settings
var openAiSection = config.GetSection("AzureOpenAI");
var endpoint = openAiSection.GetValue<string>("Endpoint");
var apiKey = openAiSection.GetValue<string>("ApiKey");
var chatDeployment = openAiSection.GetValue<string>("ChatDeployment");
var embedDeployment = openAiSection.GetValue<string>("EmbeddingDeployment");

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    // Allow app to start but services that need OpenAI will throw with clearer message.
}

// Configure Azure OpenAI client and register with DI
if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
{
    var client = new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey));
    services.AddSingleton(client);
}

services.AddSingleton<IConfiguration>(config);
services.AddHttpClient<RepoService>();
services.AddSingleton<TextChunker>();
services.AddSingleton<VectorStore>();
services.AddSingleton<EmbeddingService>();
services.AddSingleton<RagService>();
services.AddSingleton<ReflectionService>();
services.AddSingleton<EvaluationService>();
services.AddSingleton<AgentService>();

// Blazor components registration
services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
