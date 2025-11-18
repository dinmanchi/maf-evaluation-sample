# Lessons Learned: Microsoft Agent Framework (MAF) Integration

## Problem Summary

When implementing a .NET application using Microsoft Agent Framework (MAF) with Azure OpenAI, we encountered compilation errors indicating that `CreateAIAgent()` extension method was not available on the `ChatClient` type.

## Error Messages Encountered

```
error CS1929: 'ChatClient' does not contain a definition for 'CreateAIAgent' and the best extension method overload 'ChatClientExtensions.CreateAIAgent(IChatClient, string?, string?, string?, IList<AITool>?, ILoggerFactory?, IServiceProvider?)' requires a receiver of type 'Microsoft.Extensions.AI.IChatClient'
```

## Root Cause

The `CreateAIAgent()` extension method in Microsoft Agent Framework requires an `IChatClient` interface from the `Microsoft.Extensions.AI` namespace, not the `ChatClient` class from the `Azure.AI.OpenAI` / `OpenAI.Chat` namespace.

## Solution

### 1. Required NuGet Packages

Ensure all three MAF-related packages are installed:

```bash
dotnet add package Microsoft.Agents.AI --prerelease
dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
dotnet add package Microsoft.Extensions.AI.OpenAI --prerelease
```

**Key Package**: `Microsoft.Extensions.AI.OpenAI` provides the critical `AsIChatClient()` extension method.

### 2. Correct Code Pattern

**❌ INCORRECT - Does NOT work:**

```csharp
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;

var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential());
var chatClient = azureClient.GetChatClient(deployment);

// This FAILS - ChatClient doesn't have CreateAIAgent
AIAgent agent = chatClient.CreateAIAgent(
    instructions: "You are a helpful assistant",
    tools: [...]
);
```

**✅ CORRECT - Works perfectly:**

```csharp
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;  // Critical import

var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential());
var chatClient = azureClient.GetChatClient(deployment);

// Convert to IChatClient first
IChatClient iChatClient = chatClient.AsIChatClient();

// Now CreateAIAgent works
AIAgent agent = iChatClient.CreateAIAgent(
    instructions: "You are a helpful assistant",
    name: "MyAgent",
    tools: [AIFunctionFactory.Create(MyFunction)]
);
```

### 3. Verification of MAF Usage

Our implementation confirms we're using **Microsoft Agent Framework**, not lower-level libraries:

#### MAF Components Used:
- ✅ `Microsoft.Agents.AI` (v1.0.0-preview.251114.1)
- ✅ `Microsoft.Agents.AI.OpenAI` (v1.0.0-preview.251114.1)
- ✅ `AIAgent` class from MAF
- ✅ `CreateAIAgent()` extension method
- ✅ `RunAsync()` agent execution method
- ✅ `AIFunctionFactory.Create()` for tool creation

#### Lower-Level Components (Only for Client Creation):
- Azure.AI.OpenAI (v2.1.0) - Used only to create `AzureOpenAIClient` and `ChatClient`
- Microsoft.Extensions.AI.OpenAI (v10.0.0-preview.1.25560.10) - Provides bridge via `AsIChatClient()`

**Conclusion**: We are correctly using MAF. The Azure OpenAI SDK is only used for initial client setup, then immediately converted to MAF's abstraction layer.

## Key Takeaways

1. **Type Conversion is Required**: Azure OpenAI's `ChatClient` must be converted to `IChatClient` using `.AsIChatClient()`

2. **Package Dependencies Matter**: The `Microsoft.Extensions.AI.OpenAI` package is essential for the conversion bridge

3. **Official Documentation Pattern**: Microsoft's official docs consistently show this pattern:
   ```csharp
   new AzureOpenAIClient(...)
       .GetChatClient(deployment)
       .AsIChatClient()  // ← Critical conversion
       .CreateAIAgent(...)
   ```

4. **Function Tool Creation**: Use `AIFunctionFactory.Create(methodName)` for registering C# methods as agent tools

5. **Agent Execution**: Use `await agent.RunAsync(query)` for single-turn interactions

## Architecture Diagram

```
Azure.AI.OpenAI (Low-level SDK)
    ↓
AzureOpenAIClient.GetChatClient()
    ↓
ChatClient (OpenAI.Chat)
    ↓
.AsIChatClient() ← Bridge provided by Microsoft.Extensions.AI.OpenAI
    ↓
IChatClient (Microsoft.Extensions.AI)
    ↓
.CreateAIAgent() ← MAF extension method
    ↓
AIAgent (Microsoft.Agents.AI) ← High-level MAF abstraction
```

## References

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/)
- [Azure OpenAI Chat Completion Agents](https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-types/azure-openai-chat-completion-agent)
- [Create and Run an Agent Tutorial](https://learn.microsoft.com/en-us/agent-framework/tutorials/agents/run-agent)

## Date
November 17, 2025

## Additional Lessons: Agent Evaluation Implementation

### Problem: Agentic Evaluators Not Available in .NET

When implementing evaluation metrics like those in the Python Azure AI Evaluation SDK, we discovered that **agentic evaluators are not yet available in .NET**:

**Evaluators Missing from .NET**:
- ❌ `ToolCallAccuracyEvaluator` 
- ❌ `IntentResolutionEvaluator`
- ❌ `TaskAdherenceEvaluator`
- ❌ `ResponseCompletenessEvaluator`

**Packages Investigated**:
- ❌ `Azure.AI.Evaluation` - Package does not exist for .NET (Python-only)
- ✅ `Microsoft.Extensions.AI.Evaluation` (v10.0.0) - Exists but only contains basic evaluators (`CoherenceEvaluator`, `RelevanceEvaluator`, etc.)

### Solution: LLM-as-Judge Pattern

Implement custom evaluators using the same `IChatClient` with structured prompts:

```csharp
public class EvaluationService
{
    private readonly IChatClient _evaluatorClient;

    private async Task<EvaluationMetric> EvaluateToolCallAccuracyAsync(AgentExecutionData data)
    {
        var prompt = $@"Evaluate the tool call accuracy for this agent interaction.

Query: {data.Query}
Response: {data.Response}
Tool Calls Made: {JsonSerializer.Serialize(data.ToolCalls)}

Provide a score from 1-5 where:
1 = Completely wrong tools called or wrong parameters
5 = Perfect tool calling

Respond ONLY with valid JSON:
{{
  ""score"": <number 1-5>,
  ""reason"": ""<brief explanation>""
}}";

        var response = await _evaluatorClient.GetResponseAsync([
            new ChatMessage(ChatRole.User, prompt)
        ]);
        
        var content = response.Text ?? "{}";
        // Parse JSON and return EvaluationMetric
    }
}
```

### Capturing Agent Execution Data

**Problem**: `AgentThread.Messages` property not accessible

**Solution**: Use `RunStreamingAsync()` to capture execution trace:

```csharp
var executionData = new AgentExecutionData { Query = query };
var thread = agent.GetNewThread();

await foreach (var update in agent.RunStreamingAsync(query, thread))
{
    if (update is FunctionCallContent functionCall)
    {
        // Capture tool call name, arguments, ID
        executionData.ToolCalls.Add(new ToolCallInfo { ... });
    }
    
    if (update is FunctionResultContent functionResult)
    {
        // Match and update with result
        var toolCall = executionData.ToolCalls.FirstOrDefault(
            tc => tc.ToolCallId == functionResult.CallId);
        if (toolCall != null) toolCall.Result = functionResult.Result?.ToString();
    }
    
    if (update is TextContent textContent)
    {
        // Capture assistant response
        executionData.Response += textContent.Text;
    }
}
```

### Key API Differences

**ChatResponse Access Pattern**:
```csharp
// ✅ CORRECT
var response = await chatClient.GetResponseAsync([new ChatMessage(...)]);
var content = response.Text;  // Direct property access

// ❌ INCORRECT
var content = response.Message.Text;  // Property doesn't exist
```

### Trade-offs of LLM-as-Judge Approach

**Benefits**:
- ✅ Works with current .NET packages
- ✅ Flexible evaluation criteria
- ✅ Provides detailed reasoning
- ✅ Can be extended to any custom metric

**Trade-offs**:
- ⚠️ Requires LLM API calls (cost/latency per evaluation)
- ⚠️ Results may vary slightly between runs
- ⚠️ No standardized metrics like Python SDK
- ⚠️ Need to handle JSON parsing and error cases

### Complete Evaluation Workflow

```
1. Agent Execution (RunStreamingAsync)
   ↓
2. Capture execution data (tool calls, responses, messages)
   ↓
3. Serialize to JSON
   ↓
4. For each metric:
   - Create evaluation prompt
   - Call LLM evaluator
   - Parse JSON response
   - Calculate pass/fail based on threshold
   ↓
5. Display results
```

### Example Output

```json
{
  "query": "What's the weather like in Tokyo?",
  "response": "The weather in Tokyo is sunny with a high of 18°C.",
  "toolCallAccuracy": {
    "score": 5,
    "reason": "The correct tool 'GetWeather' was called with appropriate parameter",
    "passed": true,
    "threshold": 3
  },
  "intentResolution": {
    "score": 4,
    "reason": "Agent correctly understood user's intent to get weather",
    "passed": true,
    "threshold": 3
  }
}
```

### Future Considerations

When agentic evaluators become available in .NET:
1. Replace LLM-as-judge with native evaluators for standardized metrics
2. Keep execution data capture logic (still needed)
3. Consider hybrid approach: native evaluators for standard metrics + LLM-as-judge for custom metrics

