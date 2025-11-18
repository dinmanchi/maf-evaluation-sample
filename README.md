# MAF Evaluation Sample

A .NET implementation demonstrating how to evaluate Microsoft Agent Framework (MAF) agents using LLM-as-judge patterns and Azure AI Foundry integration.

## Overview

This project implements a complete evaluation system for MAF agents, featuring:
- **Weather Agent** with tool calling capabilities
- **LLM-as-Judge Evaluation** with 4 comprehensive metrics
- **Azure AI Foundry Integration** for cloud-based evaluation
- **Execution Data Capture** using streaming APIs

## Features

### Agent Implementation
- Built with `Microsoft.Agents.AI` (MAF preview)
- Uses `AsIChatClient()` bridge for Extensions.AI integration
- Implements streaming API for complete execution data capture
- Mock weather tool demonstrating function calling

### Evaluation Metrics
Four LLM-based evaluators providing detailed analysis:

1. **Tool Call Accuracy** - Validates correct tool selection and parameter usage
2. **Intent Resolution** - Assesses user intent understanding
3. **Task Adherence** - Measures response relevance to query
4. **Response Completeness** - Evaluates answer thoroughness

Each metric provides:
- Score (1-5 scale)
- Pass/fail status based on thresholds
- Detailed reasoning for the score
- JSON output for programmatic analysis

### Azure AI Foundry Support
- Connection validation to existing AI Foundry projects
- Framework for cloud-based evaluation runs
- Project endpoint configuration
- Authentication via Azure CLI credentials

## Quick Start

### Prerequisites
- .NET 10.0 SDK
- Azure OpenAI deployment
- Azure CLI (optional, for cloud evaluation)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/dinmanchi/maf-evaluation-sample.git
   cd maf-evaluation-sample
   ```

2. **Configure environment variables**
   ```bash
   cp .env.example .env
   ```
   
   Edit `.env` and add your Azure OpenAI credentials:
   ```
   AZURE_OPENAI_ENDPOINT=https://your-endpoint.openai.azure.com/
   AZURE_OPENAI_DEPLOYMENT=your-deployment-name
   ```

3. **Run the sample**
   ```bash
   dotnet run
   ```

### Output Example
```
Query: What's the weather like in Tokyo?
Response: The weather in Tokyo is currently stormy, with a high of 14°C.

=== Evaluation Results ===

Tool Call Accuracy:
  Score: 5.00
  Passed: True
  Reason: The correct tool (GetWeather) was called with the correct parameter...

Intent Resolution:
  Score: 4.00
  Passed: True
  Reason: The agent correctly identified that the user wanted current weather...
```

## Documentation

📖 **[Complete Implementation Guide](docs/IMPLEMENTATION_GUIDE.md)** - Comprehensive guide covering:
- Quick start with code examples
- Architecture overview and components
- Evaluation approaches (local vs cloud)
- Best practices and patterns
- Troubleshooting common issues
- Testing strategies

## Best Practices Summary

### Key Patterns for MAF Evaluation

**1. Use AsIChatClient() Bridge**
```csharp
// ✅ CORRECT: Convert ChatClient to IChatClient for MAF
IChatClient iChatClient = chatClient.AsIChatClient();
var agent = iChatClient.CreateAIAgent(...);
```

**2. Capture Data with Streaming API**
```csharp
// ✅ Streaming gives complete execution trace
await foreach (var update in client.CompleteStreamingAsync(messages))
{
    // Access tool calls, results, and messages
}
```

**3. Structure Evaluation Prompts**
- Clear 1-5 scoring criteria
- Include all relevant data (query, response, tool calls)
- Request JSON output format
- Validate and handle errors

**4. Multiple Metrics > Single Metric**
- Tool Call Accuracy (correct tools, parameters)
- Intent Resolution (understanding user goal)
- Task Adherence (response relevance)
- Response Completeness (thoroughness)

**5. Environment-Based Configuration**
```csharp
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
// Works in dev (Azure CLI) and prod (Managed Identity)
```

See [Implementation Guide](docs/IMPLEMENTATION_GUIDE.md) for complete best practices.

## Project Structure

```
maf-evaluation-sample/
├── Models/
│   └── AgentExecutionData.cs      # Data structures for evaluation
├── Services/
│   ├── EvaluationService.cs       # LLM-as-judge evaluators
│   └── CloudEvaluationService.cs  # Azure AI Foundry integration
├── docs/                          # Documentation
├── Program.cs                     # Main orchestration
├── WeatherTool.cs                 # Mock weather function
└── .env.example                   # Configuration template
```

## Dependencies

- **Microsoft.Agents.AI** (1.0.0-preview.251114.1) - Core MAF framework
- **Microsoft.Extensions.AI.OpenAI** (10.0.0-preview.1.25560.10) - AsIChatClient() bridge
- **Microsoft.Extensions.AI.Evaluation** (10.0.0) - Evaluation base types
- **Azure.AI.Projects** (1.2.0-beta.3) - Cloud evaluation support

## Key Concepts

### AsIChatClient() Bridge
MAF agents can be converted to `IChatClient` for use with Extensions.AI:
```csharp
IChatClient client = chatClient.AsIChatClient(agent);
```

### Execution Data Capture
Uses streaming API to capture complete execution context:
```csharp
await foreach (var update in client.CompleteStreamingAsync(messages))
{
    // Capture tool calls, messages, and final response
}
```

### LLM-as-Judge Pattern
Structured prompts evaluate agent performance:
```csharp
var prompt = $@"Evaluate the tool call accuracy on a scale of 1-5...
Query: {query}
Tool Calls: {toolCallsJson}
Response: {response}";
```

## Azure AI Foundry Integration

Optional cloud evaluation using your existing AI Foundry project:

1. Set `PROJECT_ENDPOINT` in `.env`
2. Authenticate with `az login`
3. Run the sample - connection test runs automatically

See [Cloud Evaluation Setup](docs/CLOUD_EVALUATION_SETUP.md) for details.

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is provided as-is for educational and demonstration purposes.

## Resources

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/azure/ai-services/agents/)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions)
- [Azure AI Foundry](https://ai.azure.com/)
- [Original Python Example](https://github.com/balakreshnan/Samples2025/blob/main/msagentframework/agtfrmkeval.md)

## Support

For issues or questions, please open an issue on GitHub.
