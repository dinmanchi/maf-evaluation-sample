# Agent Evaluation Patterns

## LLM-as-Judge Pattern

### Core Principle
Use an LLM to evaluate agent performance with structured prompts and scoring criteria.

### Standard Metrics

#### 1. Tool Call Accuracy
Evaluates if agent called correct tools with correct parameters.

```csharp
var prompt = $@"
You are an expert evaluator of AI agent responses.

TASK: Evaluate the tool call accuracy on a scale of 1-5.

CRITERIA:
- Score 5: Correct tool selected, all parameters valid, result properly used
- Score 4: Correct tool, minor parameter issues
- Score 3: Correct tool, some parameter problems
- Score 2: Wrong tool or major parameter errors
- Score 1: No tool call when needed, or completely wrong

DATA:
Query: {query}
Tool Calls: {JsonSerializer.Serialize(toolCalls)}
Response: {response}

OUTPUT FORMAT (JSON):
{{
  ""score"": <1-5>,
  ""reason"": ""<detailed explanation>""
}}
";
```

#### 2. Intent Resolution
Assesses if agent understood user's goal.

```csharp
CRITERIA:
- Score 5: Perfect understanding of user intent with comprehensive response
- Score 4: Good understanding with minor gaps
- Score 3: Basic understanding, acceptable response
- Score 2: Partial misunderstanding
- Score 1: Completely missed user intent
```

#### 3. Task Adherence
Measures if response addresses the query directly.

```csharp
CRITERIA:
- Score 5: Response directly and fully addresses the task
- Score 4: Mostly on task with minor deviations
- Score 3: On task but incomplete
- Score 2: Partially off task
- Score 1: Did not address the task
```

#### 4. Response Completeness
Evaluates thoroughness of the answer.

```csharp
CRITERIA:
- Score 5: Comprehensive with all relevant details
- Score 4: Complete with minor missing details
- Score 3: Adequate, covers essentials
- Score 2: Incomplete, missing important information
- Score 1: Severely lacking detail
```

## Evaluation Service Structure

```csharp
public class EvaluationService
{
    private readonly IChatClient _evaluatorClient;
    
    public EvaluationService(IChatClient evaluatorClient)
    {
        _evaluatorClient = evaluatorClient;
    }
    
    public async Task<EvaluationResults> EvaluateAsync(AgentExecutionData data)
    {
        return new EvaluationResults
        {
            ToolCallAccuracy = await EvaluateToolCallAccuracyAsync(data),
            IntentResolution = await EvaluateIntentResolutionAsync(data),
            TaskAdherence = await EvaluateTaskAdherenceAsync(data),
            ResponseCompleteness = await EvaluateResponseCompletenessAsync(data)
        };
    }
    
    private async Task<MetricResult> EvaluateToolCallAccuracyAsync(AgentExecutionData data)
    {
        // Build structured prompt
        // Call LLM with JSON response format
        // Parse and validate result
        // Return MetricResult with score, reason, passed status
    }
}
```

## Data Structures

```csharp
public class AgentExecutionData
{
    public string Query { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public List<ToolCallData> ToolCalls { get; set; } = new();
    public List<MessageData> Messages { get; set; } = new();
}

public class MetricResult
{
    public double Score { get; set; }      // 1-5 scale
    public string Reason { get; set; }     // Detailed explanation
    public bool Passed { get; set; }       // Above threshold
    public int Threshold { get; set; }     // Pass/fail cutoff (typically 3)
}
```

## Best Practices

### Prompt Design
- **Clear criteria** with specific score definitions
- **Include all context** (query, response, tool calls, messages)
- **Request JSON output** for consistent parsing
- **Provide examples** of good/bad responses when needed

### Validation
```csharp
try
{
    var result = JsonSerializer.Deserialize<MetricResult>(response.Message.Text);
    
    // Validate score range
    if (result.Score < 1 || result.Score > 5)
        throw new InvalidOperationException($"Score {result.Score} out of range");
    
    // Ensure reason is provided
    if (string.IsNullOrWhiteSpace(result.Reason))
        result.Reason = "No reasoning provided";
        
    result.Passed = result.Score >= threshold;
    result.Threshold = threshold;
    
    return result;
}
catch (JsonException ex)
{
    return new MetricResult 
    { 
        Score = 0, 
        Reason = $"Evaluation failed: {ex.Message}",
        Passed = false
    };
}
```

### Testing Strategies
- **Diverse queries**: Simple, complex, ambiguous, edge cases
- **Consistency checks**: Run same evaluation multiple times
- **Threshold tuning**: Adjust based on domain requirements
- **Batch evaluation**: Test against JSONL datasets

### Output Formats
```csharp
// Console output
Console.WriteLine($"Tool Call Accuracy:");
Console.WriteLine($"  Score: {result.Score:F2}");
Console.WriteLine($"  Passed: {result.Passed}");
Console.WriteLine($"  Threshold: {result.Threshold}");
Console.WriteLine($"  Reason: {result.Reason}");

// JSON output for programmatic use
var jsonResults = JsonSerializer.Serialize(evaluationResults, new JsonSerializerOptions 
{ 
    WriteIndented = true 
});
```

## Future Enhancements
- **Safety evaluation**: Detect harmful or biased responses
- **Groundedness**: Verify responses against source data
- **Multi-turn evaluation**: Assess conversation coherence
- **Custom metrics**: Domain-specific evaluation criteria
- **Cloud evaluation**: Azure AI Foundry integration
