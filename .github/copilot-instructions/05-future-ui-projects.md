# Future UI Projects - Agent Interaction Patterns

## Project Structure (When Adding UI)
```
src/
├── MafEvaluation.Web/
│   ├── Components/          # Razor/Blazor components
│   │   ├── AgentChat/      # Chat UI components
│   │   ├── Evaluation/     # Evaluation display
│   │   └── Shared/         # Shared UI elements
│   ├── Services/            # UI-specific services
│   ├── Models/              # View models
│   └── Program.cs           # Aspire + Web configuration
```

## UI Technology Options

### Option 1: Blazor Server (Recommended for Aspire)
Real-time, stateful, integrated with SignalR.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add API client
builder.Services.AddHttpClient<IAgentApiClient, AgentApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://api"); // Service discovery
});

var app = builder.Build();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
```

### Option 2: Blazor WebAssembly
Client-side, works well for demos.

### Option 3: React/Vue + .NET Backend
Separate frontend calling .NET API.

## Agent Chat Component Pattern

```razor
@* Components/AgentChat/ChatInterface.razor *@
@inject IAgentApiClient ApiClient

<div class="chat-container">
    <div class="messages">
        @foreach (var msg in Messages)
        {
            <ChatMessage Message="@msg" />
        }
        @if (IsProcessing)
        {
            <div class="loading">Agent is thinking...</div>
        }
    </div>
    
    <div class="input-area">
        <input @bind="CurrentQuery" 
               @onkeypress="HandleKeyPress"
               placeholder="Ask the agent..." />
        <button @onclick="SendQuery" disabled="@IsProcessing">
            Send
        </button>
    </div>
</div>

@code {
    private List<ChatMessageModel> Messages = new();
    private string CurrentQuery = "";
    private bool IsProcessing = false;
    
    private async Task SendQuery()
    {
        if (string.IsNullOrWhiteSpace(CurrentQuery)) return;
        
        Messages.Add(new ChatMessageModel 
        { 
            Role = "user", 
            Content = CurrentQuery 
        });
        
        IsProcessing = true;
        var query = CurrentQuery;
        CurrentQuery = "";
        
        try
        {
            var response = await ApiClient.QueryAgentAsync(query);
            
            Messages.Add(new ChatMessageModel
            {
                Role = "assistant",
                Content = response.Response,
                ToolCalls = response.ToolCalls
            });
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    private async Task HandleKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendQuery();
        }
    }
}
```

## Streaming Response Pattern

```razor
@* Real-time streaming of agent responses *@
@code {
    private async Task SendStreamingQuery()
    {
        var assistantMessage = new ChatMessageModel 
        { 
            Role = "assistant", 
            Content = "" 
        };
        Messages.Add(assistantMessage);
        
        await foreach (var chunk in ApiClient.QueryAgentStreamAsync(query))
        {
            assistantMessage.Content += chunk.Text;
            StateHasChanged(); // Update UI
        }
    }
}
```

## Evaluation Display Component

```razor
@* Components/Evaluation/EvaluationResults.razor *@
<div class="evaluation-panel">
    <h3>Evaluation Results</h3>
    
    <div class="metrics">
        <MetricCard 
            Name="Tool Call Accuracy" 
            Result="@Evaluation.ToolCallAccuracy" />
        <MetricCard 
            Name="Intent Resolution" 
            Result="@Evaluation.IntentResolution" />
        <MetricCard 
            Name="Task Adherence" 
            Result="@Evaluation.TaskAdherence" />
        <MetricCard 
            Name="Response Completeness" 
            Result="@Evaluation.ResponseCompleteness" />
    </div>
    
    <div class="overall-score">
        Overall: @Evaluation.AverageScore.ToString("F2") / 5.0
        @if (Evaluation.AllPassed)
        {
            <span class="badge success">✓ Passed</span>
        }
        else
        {
            <span class="badge warning">⚠ Review Needed</span>
        }
    </div>
</div>

@code {
    [Parameter] public EvaluationResults Evaluation { get; set; }
}
```

## Tool Call Visualization

```razor
@* Components/AgentChat/ToolCallDisplay.razor *@
<div class="tool-calls">
    @foreach (var toolCall in ToolCalls)
    {
        <div class="tool-call">
            <div class="tool-header">
                <span class="tool-icon">🔧</span>
                <strong>@toolCall.Name</strong>
            </div>
            <div class="tool-args">
                <code>@JsonSerializer.Serialize(toolCall.Arguments)</code>
            </div>
            @if (!string.IsNullOrEmpty(toolCall.Result))
            {
                <div class="tool-result">
                    Result: <code>@toolCall.Result</code>
                </div>
            }
        </div>
    }
</div>

@code {
    [Parameter] public List<ToolCallData> ToolCalls { get; set; }
}
```

## Agent Selector Component

```razor
@* For multi-agent scenarios *@
<div class="agent-selector">
    <label>Select Agent:</label>
    <select @bind="SelectedAgent">
        <option value="weather">Weather Agent</option>
        <option value="customer">Customer Service Agent</option>
        <option value="product">Product Agent</option>
    </select>
</div>

@code {
    [Parameter] public string SelectedAgent { get; set; } = "weather";
    [Parameter] public EventCallback<string> SelectedAgentChanged { get; set; }
}
```

## AppHost Configuration for UI

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.MafEvaluation_Api>("api")
    .WithExternalHttpEndpoints();

var web = builder.AddProject<Projects.MafEvaluation_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api);  // Service discovery

builder.Build().Run();
```

## API Client Service

```csharp
public interface IAgentApiClient
{
    Task<AgentResponse> QueryAgentAsync(string query, string agentName = "weather");
    IAsyncEnumerable<StreamChunk> QueryAgentStreamAsync(string query);
    Task<EvaluationResults> EvaluateAsync(string query, string response);
}

public class AgentApiClient : IAgentApiClient
{
    private readonly HttpClient _httpClient;
    
    public AgentApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<AgentResponse> QueryAgentAsync(string query, string agentName = "weather")
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/agents/{agentName}/query",
            new { query }
        );
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentResponse>();
    }
}
```

## Evaluation Toggle Feature

```razor
@* Allow users to enable/disable evaluation *@
<div class="settings">
    <label>
        <input type="checkbox" @bind="IncludeEvaluation" />
        Show Evaluation Results
    </label>
</div>

@code {
    private bool IncludeEvaluation = false;
    
    private async Task SendQuery()
    {
        var response = await ApiClient.QueryAgentAsync(
            CurrentQuery, 
            includeEvaluation: IncludeEvaluation
        );
        
        // Display response
        // Optionally display evaluation if included
    }
}
```

## Best Practices
- Use Blazor Server for real-time agent interactions
- Implement streaming for long-running agent queries
- Show tool calls transparently to users
- Display evaluation results in development/testing mode
- Use service discovery (e.g., `https+http://api`) for API calls
- Handle errors gracefully with user-friendly messages
- Add loading states during agent processing
- Implement conversation history management
- Use SignalR for real-time updates
- Add export functionality for conversations
- Implement retry logic for failed requests
