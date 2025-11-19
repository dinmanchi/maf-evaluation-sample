# Microsoft Agent Framework (MAF) Conventions

## Core Patterns

### AsIChatClient() Bridge Pattern
Always use the bridge to convert Azure OpenAI ChatClient to IChatClient:

```csharp
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

var azureClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
var chatClient = azureClient.GetChatClient(deployment);

// ✅ REQUIRED: Use AsIChatClient() bridge
IChatClient iChatClient = chatClient.AsIChatClient();

// Now create agent
var agent = iChatClient.CreateAIAgent(
    instructions: "...",
    name: "AgentName",
    tools: [AIFunctionFactory.Create(ToolMethod)]
);
```

### Tool Creation
Use AIFunctionFactory for function calling:

```csharp
// Create tools from static methods
var tools = new[] 
{
    AIFunctionFactory.Create(GetWeather),
    AIFunctionFactory.Create(SearchDatabase),
    AIFunctionFactory.Create(SendEmail)
};

// Static method signature
public static string GetWeather(string location) 
{
    // Implementation
}
```

### Data Capture with Streaming API
Always use streaming for complete execution trace:

```csharp
var executionData = new AgentExecutionData();
var messages = new List<ChatMessage> { new(ChatRole.User, query) };

await foreach (var update in iChatClient.CompleteStreamingAsync(messages))
{
    if (update is StreamingChatCompletionUpdate chatUpdate)
    {
        // Capture tool calls
        foreach (var toolCall in chatUpdate.ToolCalls)
        {
            executionData.ToolCalls.Add(new ToolCallData
            {
                Name = toolCall.FunctionName,
                Arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    toolCall.FunctionArguments.ToString()
                ),
                ToolCallId = toolCall.ToolCallId
            });
        }
        
        // Capture response text
        if (!string.IsNullOrEmpty(chatUpdate.Text))
        {
            executionData.Response += chatUpdate.Text;
        }
    }
}
```

## Naming Conventions
- Agent classes: `{Purpose}Agent` (e.g., `WeatherAgent`, `CustomerServiceAgent`)
- Tool methods: Verb-first (e.g., `GetWeather`, `SearchProducts`, `UpdateOrder`)
- Execution data: `{AgentName}ExecutionData`

## Configuration
- Use environment variables for endpoints and keys
- Load from .env files with multiple path fallback
- Use DefaultAzureCredential for authentication

```csharp
// Multi-path .env loading
var possiblePaths = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env")
};

foreach (var envPath in possiblePaths)
{
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
        break;
    }
}
```

## Error Handling
- Always validate environment variables with clear error messages
- Use try-catch for LLM calls with retry logic
- Log tool call failures with context

## Testing Agents
- Test with diverse queries (simple, complex, ambiguous, edge cases)
- Capture execution data for every test
- Evaluate with multiple metrics
- Document expected vs actual behavior
