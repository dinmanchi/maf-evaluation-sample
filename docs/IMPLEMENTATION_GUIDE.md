# Implementation Guide

Complete guide for implementing evaluation in Microsoft Agent Framework (MAF) .NET applications.

## Table of Contents
- [Quick Start](#quick-start)
- [Architecture Overview](#architecture-overview)
- [Core Components](#core-components)
- [Evaluation Approaches](#evaluation-approaches)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Quick Start

### 1. Install Required Packages

```bash
dotnet add package Microsoft.Agents.AI --prerelease
dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
dotnet add package Microsoft.Extensions.AI.OpenAI --prerelease
dotnet add package Azure.AI.Projects --prerelease  # Optional for cloud evaluation
```

### 2. Create Agent with AsIChatClient() Bridge

```csharp
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Create Azure OpenAI client
var azureClient = new AzureOpenAIClient(
    new Uri(endpoint), 
    new DefaultAzureCredential()
);
var chatClient = azureClient.GetChatClient(deployment);

// ✅ CRITICAL: Convert to IChatClient for MAF
IChatClient iChatClient = chatClient.AsIChatClient();

// Create agent with tools
AIAgent agent = iChatClient.CreateAIAgent(
    instructions: "You are a helpful weather assistant",
    name: "WeatherAgent",
    tools: [AIFunctionFactory.Create(GetWeather)]
);
```

### 3. Capture Execution Data

Use streaming API to capture complete execution context:

```csharp
var executionData = new AgentExecutionData 
{ 
    Query = query,
    ToolCalls = new List<ToolCallData>(),
    Messages = new List<MessageData>()
};

var messages = new List<ChatMessage> 
{ 
    new ChatMessage(ChatRole.User, query) 
};

await foreach (var update in iChatClient.CompleteStreamingAsync(messages))
{
    // Capture tool calls
    if (update is StreamingChatCompletionUpdate chatUpdate)
    {
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

### 4. Implement LLM-as-Judge Evaluators

```csharp
public class EvaluationService
{
    private readonly IChatClient _evaluatorClient;

    public async Task<MetricResult> EvaluateToolCallAccuracyAsync(
        AgentExecutionData data)
    {
        var prompt = $@"Evaluate the tool call accuracy on a scale of 1-5.

CRITERIA:
- Score 5: Correct tool, correct parameters, result properly used
- Score 4: Correct tool, minor parameter issues
- Score 3: Correct tool, some parameter problems
- Score 2: Wrong tool or major parameter errors
- Score 1: No tool call when needed

DATA:
Query: {data.Query}
Tool Calls: {JsonSerializer.Serialize(data.ToolCalls)}
Response: {data.Response}

Respond ONLY with valid JSON:
{{
  ""score"": <1-5>,
  ""reason"": ""<explanation>""
}}";

        var response = await _evaluatorClient.CompleteAsync([
            new ChatMessage(ChatRole.User, prompt)
        ], new ChatOptions 
        { 
            ResponseFormat = ChatResponseFormat.Json 
        });

        var result = JsonSerializer.Deserialize<MetricResult>(
            response.Message.Text ?? "{}"
        );
        
        result.Passed = result.Score >= 3;
        result.Threshold = 3;
        
        return result;
    }
}
```

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                      │
│  ┌──────────────┐         ┌──────────────────────────┐  │
│  │  Program.cs  │────────▶│  EvaluationService.cs   │  │
│  └──────────────┘         └──────────────────────────┘  │
└──────────┬──────────────────────────────┬───────────────┘
           │                               │
           ▼                               ▼
┌─────────────────────┐         ┌─────────────────────────┐
│  Microsoft.Agents   │         │   Microsoft.Extensions  │
│      .AI (MAF)      │◀────────│      .AI.OpenAI         │
│                     │         │   (AsIChatClient Bridge)│
└─────────────────────┘         └─────────────────────────┘
           │                               
           ▼                               
┌─────────────────────────────────────────────────────────┐
│                 Azure.AI.OpenAI                         │
│          (AzureOpenAIClient, ChatClient)                │
└─────────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────┐
│              Azure OpenAI Service                       │
└─────────────────────────────────────────────────────────┘
```

## Core Components

### AgentExecutionData Model

```csharp
public class AgentExecutionData
{
    public string Query { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public List<ToolCallData> ToolCalls { get; set; } = new();
    public List<MessageData> Messages { get; set; } = new();
}

public class ToolCallData
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
    public string Result { get; set; } = string.Empty;
    public string ToolCallId { get; set; } = string.Empty;
}
```

### Evaluation Metrics

Implement these four key metrics:

1. **Tool Call Accuracy** - Validates correct tool selection and parameter usage
2. **Intent Resolution** - Assesses user intent understanding
3. **Task Adherence** - Measures response relevance to query
4. **Response Completeness** - Evaluates answer thoroughness

Each returns:
```csharp
public class MetricResult
{
    public double Score { get; set; }      // 1-5
    public string Reason { get; set; }     // Explanation
    public bool Passed { get; set; }       // Above threshold
    public int Threshold { get; set; }     // Pass/fail cutoff (typically 3)
}
```

## Evaluation Approaches

### Local LLM-as-Judge (Current Implementation) ✅

**Pros:**
- Works immediately, no additional setup
- Flexible evaluation criteria
- Fast results (seconds)
- Full control over prompts

**Cons:**
- Requires LLM API calls (cost per evaluation)
- Slight score variability between runs
- Not standardized metrics

**When to Use:**
- Development and testing
- Quick iteration
- Custom evaluation needs
- Learning and demos

### Azure AI Foundry Cloud Evaluation (Optional)

**Pros:**
- Standardized metrics
- Centralized logging
- Team collaboration
- Historical tracking

**Cons:**
- Requires Azure AI Foundry project setup
- More complex configuration
- Beta/preview status

**When to Use:**
- Production environments
- Large-scale batch evaluation
- Team collaboration needs
- Compliance/audit requirements

See [Cloud Evaluation Setup](CLOUD_EVALUATION_SETUP.md) for configuration details.

## Best Practices

### 1. Always Use AsIChatClient() Bridge

```csharp
// ❌ WRONG - ChatClient doesn't have CreateAIAgent
var agent = chatClient.CreateAIAgent(...);

// ✅ CORRECT - Convert to IChatClient first
IChatClient iChatClient = chatClient.AsIChatClient();
var agent = iChatClient.CreateAIAgent(...);
```

### 2. Use Streaming API for Data Capture

```csharp
// ✅ CORRECT - Streaming provides complete execution trace
await foreach (var update in client.CompleteStreamingAsync(messages))
{
    // Access tool calls, results, messages
}

// ❌ AVOID - Non-streaming has limited data access
var response = await client.CompleteAsync(messages);
```

### 3. Structure Evaluation Prompts

Include:
- Clear scoring criteria (1-5 scale with descriptions)
- All relevant data (query, response, tool calls)
- JSON output format request
- Examples of good/bad responses

### 4. Validate LLM Responses

```csharp
try
{
    var result = JsonSerializer.Deserialize<MetricResult>(response);
    
    if (result.Score < 1 || result.Score > 5)
        throw new InvalidOperationException("Score out of range");
    
    if (string.IsNullOrWhiteSpace(result.Reason))
        result.Reason = "No reasoning provided";
        
    return result;
}
catch (JsonException)
{
    return new MetricResult 
    { 
        Score = 0, 
        Reason = "Evaluation failed - invalid response" 
    };
}
```

### 5. Use Environment Variables for Configuration

```csharp
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT not set");
```

## Troubleshooting

### Error: 'ChatClient' does not contain definition for 'CreateAIAgent'

**Solution:** Add `AsIChatClient()` conversion:
```csharp
IChatClient iChatClient = chatClient.AsIChatClient();
```

### Missing Package: Microsoft.Extensions.AI.OpenAI

**Solution:**
```bash
dotnet add package Microsoft.Extensions.AI.OpenAI --prerelease
```

### Cannot Access AgentThread.Messages

**Solution:** Use `RunStreamingAsync()` or `CompleteStreamingAsync()` instead:
```csharp
await foreach (var update in client.CompleteStreamingAsync(messages))
{
    // Process updates
}
```

### JSON Parsing Errors in Evaluation

**Solution:** Add `ResponseFormat = ChatResponseFormat.Json` to options:
```csharp
var options = new ChatOptions 
{ 
    ResponseFormat = ChatResponseFormat.Json 
};
```

### Azure Authentication Failures

**Solution:** Ensure Azure CLI is authenticated:
```bash
az login
az account set --subscription "Your Subscription Name"
```

## Common Patterns

### Testing Multiple Queries

```csharp
var testQueries = new[]
{
    "What's the weather in Tokyo?",
    "Tell me about the weather in Paris and London",
    "Is it raining?"
};

foreach (var query in testQueries)
{
    var executionData = await RunAgentAsync(query);
    var results = await evaluationService.EvaluateAsync(executionData);
    
    Console.WriteLine($"\nQuery: {query}");
    Console.WriteLine($"Tool Accuracy: {results.ToolCallAccuracy.Score}/5");
    Console.WriteLine($"Intent Resolution: {results.IntentResolution.Score}/5");
}
```

### Batch Evaluation from File

```csharp
var queries = await File.ReadAllLinesAsync("test_queries.txt");
var allResults = new List<EvaluationResults>();

foreach (var query in queries)
{
    var execution = await RunAgentAsync(query);
    var results = await evaluationService.EvaluateAsync(execution);
    allResults.Add(results);
}

// Aggregate statistics
var avgToolAccuracy = allResults.Average(r => r.ToolCallAccuracy.Score);
var passRate = allResults.Count(r => r.AllPassed) / (double)allResults.Count;
```

## Additional Resources

- [Lessons Learned](LESSONS_LEARNED.md) - Technical insights from implementation
- [Evaluation Options](EVALUATION_OPTIONS.md) - Local vs cloud comparison
- [Cloud Setup Guide](CLOUD_EVALUATION_SETUP.md) - Azure AI Foundry configuration
- [Microsoft Agent Framework Docs](https://learn.microsoft.com/azure/ai-services/agents/)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions)
