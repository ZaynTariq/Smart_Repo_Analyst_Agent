# Smart Repository Analyst Agent

A minimal Blazor Server application that analyzes a GitHub repository using a Retrieval-Augmented Generation (RAG) pipeline, Azure OpenAI (chat + embeddings), Semantic Kernel concepts, and an in-memory vector store. The system demonstrates tool-calling (GitHub fetcher), RAG, autonomous reasoning, reflection (self-correction) loops, and simple evaluation scoring.

This project is intentionally minimal and focused on demonstrating the pipeline end-to-end with .NET 8 + Blazor Server.

---

## Highlights

- Retrieval-Augmented Generation (RAG) using repository text chunks
- In-memory vector store with cosine similarity
- Embedding generation via Azure OpenAI embedding model (batched)
- Chat completions via Azure OpenAI for analysis, reflection and evaluation
- Iterative reflection loop: repeat reflection until evaluation score >= 8 (internal)
- UI: Blazor Server page with progress/status streaming and Markdown result rendering
- Private repo support via GitHub Personal Access Token (PAT) input in UI

---

## Tech stack

- .NET 8, Blazor Server
- Microsoft.SemanticKernel (concepts used; integration simplified)
- Azure.AI.OpenAI (chat + embeddings)
- Markdig (Markdown → HTML rendering)
- HttpClient for GitHub Contents API
- In-memory VectorStore (no external DB)

---

## Project layout (important files)

- /Components/Pages/Index.razor — Main UI (input, PAT option, status log, result)
- /Services/RepoService.cs — GitHub fetcher (recursive), supports public & private repos
- /Services/TextChunker.cs — sentence-accumulation chunker (fast)
- /Services/EmbeddingService.cs — batched embedding requests via Azure OpenAI
- /Services/VectorStore.cs — in-memory vector DB, cosine similarity, GetTopKSimilar
- /Services/RagService.cs — builds prompt context and calls the chat model
- /Services/ReflectionService.cs — asks LLM to improve previous answer
- /Services/EvaluationService.cs — asks LLM to score answer (1–10)
- /Services/AgentService.cs — orchestration: fetch → chunk → embed → store → RAG → reflect loop → evaluate
- /Prompts/AnalysisPrompt.txt — analysis prompt used for RAG

---

---

## Setup

1. Clone the repository:



## Architecture (flow)

User → Repo Fetch (GitHub API) → Chunking → Embedding (Azure OpenAI) → VectorStore → RAG (chat) → Reflection loop → Evaluation → Final Output (Markdown)

Mermaid (paste into a viewer that supports mermaid):


2. Configure Azure OpenAI
- Open `appsettings.json` and set the `AzureOpenAI` section:
  ```json
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_KEY",
    "ChatDeployment": "your-chat-deployment-name",
    "EmbeddingDeployment": "your-embedding-deployment-name"
  }
  ```
- Deployments:
  - `ChatDeployment` should reference the deployed chat model (e.g. gpt-4o, gpt-4o-mini or your custom deployment name).
  - `EmbeddingDeployment` should reference the embedding model deployment (e.g. text-embedding-3-small).

3. (Optional) Configure a default GitHub token (NOT required if you will paste PAT in the UI):
- Set environment variable:
  - Windows (PowerShell): `setx GITHUB_TOKEN "ghp_xxx"`
  - Or add to `appsettings.json` as `GithubToken` entry (not recommended for secrets).

4. Restore & build:



5. Run:



- Open the browser at the URL printed by the run command (typically https://localhost:5001).

---

## Usage

1. Open the app in your browser.
2. Enter a GitHub repository URL (public) or select Private and paste a PAT (personal access token) for private repos.
3. Click "Analyze Repository".
4. The right pane shows a status log with backend steps (fetching, chunking, embedding, RAG, reflection iterations, evaluation).
5. Once processing completes and the internal evaluation score reaches the threshold (≥ 8) the final analysis is rendered as Markdown.

Notes:
- The app fetches README and up to a limited number of code/text files (to keep processing small). Default limit = 20 files.
- Embeddings are batched to reduce round trips.
- Reflection and evaluation are internal: you will only see the final improved Markdown output.

---

## Implementation notes

- Chunking: sentence-based accumulation for performance and smaller allocations; overlap retained between chunks.
- VectorStore: simple in-memory list of (text, vector). Cosine similarity ranking retrieves top-K chunks for RAG context.
- Embedding batch size and HTTP concurrency are tunable in `EmbeddingService` and `RepoService` (SemaphoreSlim).
- The reflection loop continues up to a safe max iterations (default 5) until evaluation score >= 8.

---

## Security & production

- PAT (GitHub token) is used in-memory for a request only when provided from the UI. Do NOT store tokens in source control.
- For production, store secrets in a secure vault (Azure Key Vault) and avoid reading tokens from UI in plain text.
- Be mindful of Azure OpenAI usage quotas and GitHub API rate limits.

---

## Troubleshooting

- 401/403 from Azure OpenAI: check `Endpoint`, `ApiKey`, and deployment names in appsettings.json.
- GitHub 404 or empty content:
- Ensure repo URL is correct.
- For private repos provide a PAT with `repo` scope.
- Slow performance:
- Most time is spent in embedding requests; increase embedding batch size or concurrency carefully.
- Reduce maxFiles or chunk size for quicker results.

---

## Extending

- Swap in a persistent vector DB (Redis, Milvus, Pinecone) for larger repositories.
- Add user authentication and secure token storage.
- Improve prompts and prompt templating for better analysis quality.
- Add export options (PDF, markdown file).

---

## License

MIT — adapt and use as you like.

---

If you want, I can:
- Generate an `architecture.mmd` file for the repo.
- Add environment sample file (`appsettings.sample.json`) and example commands.
- Tweak the UI styles (Tailwind or Bootstrap) or add a step-level progress indicator.






