# Future API Projects - Agentic Patterns

## Project Structure (When Adding APIs)
```
src/
├── MafEvaluation.Api/
│   ├── Agents/              # MAF agent implementations
│   ├── Controllers/         # API endpoints
│   ├── Services/            # Business logic
│   ├── Models/              # DTOs and domain models
│   └── Program.cs           # Aspire + API configuration
```

## Agentic API Patterns

### Pattern 1: Agent-Per-Endpoint
Each API endpoint uses a specialized agent.

```csharp
[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IChatClient _chatClient;
    
    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] WeatherQuery request)
    {
        // Create specialized agent for this request
        var agent = _chatClient.AsIChatClient().CreateAIAgent(
            instructions: "You are a weather information assistant.",
            name: "WeatherAgent",
            tools: [
                AIFunctionFactory.Create(GetCurrentWeather),
                AIFunctionFactory.Create(GetForecast)
            ]
        );
        
        var executionData = await ExecuteAgentAsync(agent, request.Query);
        
        return Ok(new
        {
            response = executionData.Response,
            toolCalls = executionData.ToolCalls
        });
    }
}
```

### Pattern 2: Singleton Agent Service
Reusable agent registered as a service.

```csharp
public interface IWeatherAgent
{
    Task<AgentResponse> QueryAsync(string query);
}

public class WeatherAgentService : IWeatherAgent
{
    private readonly AIAgent _agent;
    
    public WeatherAgentService(IChatClient chatClient)
    {
        _agent = chatClient.AsIChatClient().CreateAIAgent(
            instructions: "...",
            name: "WeatherAgent",
            tools: [...]
        );
    }
    
    public async Task<AgentResponse> QueryAsync(string query)
    {
        // Execute and return
    }
}

// Registration in Program.cs
builder.Services.AddSingleton<IWeatherAgent, WeatherAgentService>();
```

### Pattern 3: Multi-Agent Orchestration
Coordinator agent delegates to specialized agents.

```csharp
public class OrchestratorAgent
{
    private readonly IWeatherAgent _weatherAgent;
    private readonly IProductAgent _productAgent;
    private readonly ICustomerAgent _customerAgent;
    
    public async Task<AgentResponse> RouteQueryAsync(string query)
    {
        // Determine which agent to use
        var intent = await AnalyzeIntentAsync(query);
        
        return intent.Type switch
        {
            "weather" => await _weatherAgent.QueryAsync(query),
            "product" => await _productAgent.QueryAsync(query),
            "customer" => await _customerAgent.QueryAsync(query),
            _ => new AgentResponse { Error = "Unknown intent" }
        };
    }
}
```

## API Configuration with Aspire

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add Azure OpenAI
var endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"];
var deployment = builder.Configuration["AZURE_OPENAI_DEPLOYMENT"];
var azureClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
var chatClient = azureClient.GetChatClient(deployment);

// Register IChatClient
builder.Services.AddSingleton<IChatClient>(chatClient.AsIChatClient());

// Register agent services
builder.Services.AddSingleton<IWeatherAgent, WeatherAgentService>();

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints(); // Aspire health checks
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

await app.RunAsync();
```

## RESTful Agent Endpoints

### Query Endpoint
```csharp
POST /api/agents/{agentName}/query
{
  "query": "What's the weather in Seattle?",
  "context": { ... },  // Optional conversation context
  "evaluationLevel": "full"  // none | basic | full
}

Response:
{
  "response": "The weather in Seattle is...",
  "toolCalls": [...],
  "evaluation": { ... },  // If evaluationLevel != none
  "executionTime": "1.2s"
}
```

### Batch Endpoint
```csharp
POST /api/agents/{agentName}/batch
{
  "queries": [
    "Query 1",
    "Query 2"
  ],
  "parallel": true
}

Response:
{
  "results": [
    { "query": "Query 1", "response": "...", ... },
    { "query": "Query 2", "response": "...", ... }
  ]
}
```

## gRPC Agent Service Pattern

```csharp
public class AgentGrpcService : AgentService.AgentServiceBase
{
    private readonly IWeatherAgent _agent;
    
    public override async Task<AgentResponse> Query(
        AgentRequest request, 
        ServerCallContext context)
    {
        var result = await _agent.QueryAsync(request.Query);
        
        return new AgentResponse
        {
            Response = result.Response,
            ToolCalls = { result.ToolCalls }
        };
    }
    
    public override async Task QueryStream(
        AgentRequest request,
        IServerStreamWriter<StreamChunk> responseStream,
        ServerCallContext context)
    {
        await foreach (var chunk in _agent.QueryStreamAsync(request.Query))
        {
            await responseStream.WriteAsync(new StreamChunk { Text = chunk });
        }
    }
}
```

## API Evaluation Integration

```csharp
[HttpPost("query-with-eval")]
public async Task<IActionResult> QueryWithEvaluation([FromBody] AgentQuery request)
{
    var executionData = await ExecuteAgentAsync(request.Query);
    
    // Optionally evaluate
    if (request.IncludeEvaluation)
    {
        var evaluation = await _evaluationService.EvaluateAsync(executionData);
        
        return Ok(new
        {
            response = executionData.Response,
            toolCalls = executionData.ToolCalls,
            evaluation = evaluation,
            passed = evaluation.AllPassed
        });
    }
    
    return Ok(new { response = executionData.Response });
}
```

## AppHost Configuration

```csharp
var api = builder.AddProject<Projects.MafEvaluation_Api>("api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", builder.Configuration["AZURE_OPENAI_ENDPOINT"])
    .WithEnvironment("AZURE_OPENAI_DEPLOYMENT", builder.Configuration["AZURE_OPENAI_DEPLOYMENT"]);
```

## Best Practices
- Use dependency injection for agents and chat clients
- Implement proper error handling and timeout policies
- Add request validation and sanitization
- Include evaluation endpoints for testing
- Use Aspire dashboard to monitor agent performance
- Implement rate limiting for production
- Version your API (e.g., `/api/v1/agents/...`)
- Document agent capabilities in Swagger/OpenAPI
