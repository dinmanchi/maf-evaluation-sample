using System;
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MafEvaluationSample;
using MafEvaluationSample.Models;
using MafEvaluationSample.Services;
using DotNetEnv;
using OpenAI.Chat;

// Load environment variables from .env file
Env.Load();

Console.WriteLine("=== Weather Agent with Microsoft Agent Framework ===\n");

// Get configuration from environment variables
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") 
    ?? throw new Exception("AZURE_OPENAI_ENDPOINT not set");
var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") 
    ?? throw new Exception("AZURE_OPENAI_DEPLOYMENT not set");

Console.WriteLine($"Endpoint: {endpoint}");
Console.WriteLine($"Deployment: {deployment}\n");

// Create Azure OpenAI client and get chat client
var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential());
var chatClient = azureClient.GetChatClient(deployment);

// Convert to IChatClient and create AI Agent using Microsoft Agent Framework
IChatClient iChatClient = chatClient.AsIChatClient();

AIAgent agent = iChatClient.CreateAIAgent(
    instructions: "You are a helpful weather agent. Use the GetWeather function to provide accurate weather information for any location.",
    name: "WeatherAgent",
    tools: [AIFunctionFactory.Create(WeatherTool.GetWeather)]
);

// Execute query
var query = "What's the weather like in Tokyo?";
Console.WriteLine($"Query: {query}\n");

// Create execution data tracker
var executionData = new AgentExecutionData
{
    Query = query
};

// Get agent thread to capture messages
var thread = agent.GetNewThread();

// Use streaming to capture all updates including tool calls
var responseText = new System.Text.StringBuilder();

await foreach (var update in agent.RunStreamingAsync(query, thread))
{
    foreach (var content in update.Contents)
    {
        switch (content)
        {
            case TextContent textContent:
                responseText.Append(textContent.Text);
                Console.Write(textContent.Text);
                break;

            case FunctionCallContent functionCall:
                var toolCall = new ToolCallInfo
                {
                    Name = functionCall.Name,
                    ToolCallId = functionCall.CallId,
                    Arguments = ParseArguments(functionCall.Arguments)
                };
                executionData.ToolCalls.Add(toolCall);
                
                executionData.Messages.Add(new MessageInfo
                {
                    Role = "assistant",
                    Content = "",
                    ToolCalls = [toolCall]
                });
                break;

            case FunctionResultContent functionResult:
                // Find matching tool call and update result
                var matchingCall = executionData.ToolCalls
                    .FirstOrDefault(tc => tc.ToolCallId == functionResult.CallId);
                if (matchingCall != null)
                {
                    matchingCall.Result = functionResult.Result?.ToString() ?? string.Empty;
                }
                
                executionData.Messages.Add(new MessageInfo
                {
                    Role = "tool",
                    Content = functionResult.Result?.ToString() ?? string.Empty
                });
                break;
        }
    }
}

executionData.Response = responseText.ToString();

// Add user message
executionData.Messages.Insert(0, new MessageInfo
{
    Role = "user",
    Content = query
});

// Add final assistant response
executionData.Messages.Add(new MessageInfo
{
    Role = "assistant",
    Content = executionData.Response
});

Console.WriteLine("\n\n=== Captured Execution Data ===\n");

var jsonOptions = new JsonSerializerOptions 
{ 
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

var json = JsonSerializer.Serialize(executionData, jsonOptions);
Console.WriteLine(json);

// Run evaluations
Console.WriteLine("\n\n=== Running Evaluations ===\n");

var evaluationService = new EvaluationService(iChatClient);
var evaluationResults = await evaluationService.EvaluateAgentExecutionAsync(executionData);

Console.WriteLine("Tool Call Accuracy:");
if (evaluationResults.ToolCallAccuracy != null)
{
    Console.WriteLine($"  Score: {evaluationResults.ToolCallAccuracy.Score:F2}");
    Console.WriteLine($"  Passed: {evaluationResults.ToolCallAccuracy.Passed}");
    Console.WriteLine($"  Threshold: {evaluationResults.ToolCallAccuracy.Threshold}");
    Console.WriteLine($"  Reason: {evaluationResults.ToolCallAccuracy.Reason}");
}
else
{
    Console.WriteLine("  N/A - No tool calls made");
}

Console.WriteLine("\nIntent Resolution:");
Console.WriteLine($"  Score: {evaluationResults.IntentResolution?.Score:F2}");
Console.WriteLine($"  Passed: {evaluationResults.IntentResolution?.Passed}");
Console.WriteLine($"  Threshold: {evaluationResults.IntentResolution?.Threshold}");
Console.WriteLine($"  Reason: {evaluationResults.IntentResolution?.Reason}");

Console.WriteLine("\nTask Adherence:");
Console.WriteLine($"  Score: {evaluationResults.TaskAdherence?.Score:F2}");
Console.WriteLine($"  Passed: {evaluationResults.TaskAdherence?.Passed}");
Console.WriteLine($"  Threshold: {evaluationResults.TaskAdherence?.Threshold}");
Console.WriteLine($"  Reason: {evaluationResults.TaskAdherence?.Reason}");

Console.WriteLine("\nResponse Completeness:");
Console.WriteLine($"  Score: {evaluationResults.ResponseCompleteness?.Score:F2}");
Console.WriteLine($"  Passed: {evaluationResults.ResponseCompleteness?.Passed}");
Console.WriteLine($"  Threshold: {evaluationResults.ResponseCompleteness?.Threshold}");
Console.WriteLine($"  Reason: {evaluationResults.ResponseCompleteness?.Reason}");

var jsonResults = JsonSerializer.Serialize(evaluationResults, jsonOptions);
Console.WriteLine("\n=== Evaluation Results (JSON) ===\n");
Console.WriteLine(jsonResults);

Console.WriteLine("\n=== Execution Complete ===");

// Optional: Test Azure AI Foundry connection if configured
var projectEndpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT");
if (!string.IsNullOrEmpty(projectEndpoint))
{
    Console.WriteLine("\n=== Azure AI Foundry Integration Test ===\n");
    
    try
    {
        var cloudService = new CloudEvaluationService(
            projectEndpoint: projectEndpoint,
            modelDeploymentName: deployment
        );
        
        Console.WriteLine(cloudService.GetProjectInfo());
        await cloudService.TestConnectionAsync();
        
        Console.WriteLine("\n💡 Cloud evaluation integration is configured.");
        Console.WriteLine("   Full cloud evaluation feature coming soon!");
        Console.WriteLine("   For now, local LLM-as-judge evaluation provides all metrics.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n⚠️  Cloud evaluation connection test failed: {ex.Message}");
        Console.WriteLine("   This is optional - local evaluation results are available above.");
    }
}

// Helper method to parse function arguments
static Dictionary<string, object> ParseArguments(IDictionary<string, object?>? arguments)
{
    if (arguments == null)
        return new Dictionary<string, object>();

    var result = new Dictionary<string, object>();
    foreach (var kvp in arguments)
    {
        result[kvp.Key] = kvp.Value ?? string.Empty;
    }
    return result;
}