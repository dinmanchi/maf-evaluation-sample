using System.Text.Json;
using Microsoft.Extensions.AI;
using MafEvaluation.ConsoleApp.Models;

namespace MafEvaluation.ConsoleApp.Services;

public class EvaluationService
{
    private readonly IChatClient _evaluatorClient;

    public EvaluationService(IChatClient evaluatorClient)
    {
        _evaluatorClient = evaluatorClient;
    }

    public async Task<EvaluationResults> EvaluateAgentExecutionAsync(AgentExecutionData executionData)
    {
        var results = new EvaluationResults
        {
            Query = executionData.Query,
            Response = executionData.Response
        };

        // 1. Tool Call Accuracy
        if (executionData.ToolCalls.Any())
        {
            results.ToolCallAccuracy = await EvaluateToolCallAccuracyAsync(executionData);
        }

        // 2. Intent Resolution
        results.IntentResolution = await EvaluateIntentResolutionAsync(executionData);

        // 3. Task Adherence  
        results.TaskAdherence = await EvaluateTaskAdherenceAsync(executionData);

        // 4. Response Completeness
        results.ResponseCompleteness = await EvaluateResponseCompletenessAsync(executionData);

        return results;
    }

    private async Task<EvaluationMetric> EvaluateToolCallAccuracyAsync(AgentExecutionData data)
    {
        var toolCallsJson = JsonSerializer.Serialize(data.ToolCalls, new JsonSerializerOptions { WriteIndented = true });
        
        var prompt = $@"Evaluate the tool call accuracy for this agent interaction.

Query: {data.Query}
Response: {data.Response}

Tool Calls Made:
{toolCallsJson}

Provide a score from 1-5 where:
1 = Completely wrong tools called or wrong parameters
2 = Partially correct but significant errors
3 = Mostly correct with minor issues
4 = Correct tools and parameters
5 = Perfect tool calling

Respond ONLY with valid JSON in this format:
{{
  ""score"": <number 1-5>,
  ""reason"": ""<brief explanation>""
}}";

        return await GetEvaluationFromLLMAsync(prompt, 3.0);
    }

    private async Task<EvaluationMetric> EvaluateIntentResolutionAsync(AgentExecutionData data)
    {
        var prompt = $@"Evaluate whether the agent correctly identified and resolved the user's intent.

Query: {data.Query}
Response: {data.Response}

Provide a score from 1-5 where:
1 = Completely failed to understand user intent
2 = Partially understood but poorly addressed
3 = Understood intent, adequate response
4 = Well understood and addressed
5 = Perfectly identified and resolved intent

Respond ONLY with valid JSON in this format:
{{
  ""score"": <number 1-5>,
  ""reason"": ""<brief explanation>""
}}";

        return await GetEvaluationFromLLMAsync(prompt, 3.0);
    }

    private async Task<EvaluationMetric> EvaluateTaskAdherenceAsync(AgentExecutionData data)
    {
        var prompt = $@"Evaluate whether the agent adhered to its assigned task of providing weather information.

Query: {data.Query}
Response: {data.Response}

Provide a score from 1-5 where:
1 = Completely off-task
2 = Partially on-task with significant deviation
3 = Mostly on-task
4 = Well focused on task
5 = Perfect task adherence

Respond ONLY with valid JSON in this format:
{{
  ""score"": <number 1-5>,
  ""reason"": ""<brief explanation>""
}}";

        return await GetEvaluationFromLLMAsync(prompt, 3.0);
    }

    private async Task<EvaluationMetric> EvaluateResponseCompletenessAsync(AgentExecutionData data)
    {
        var prompt = $@"Evaluate whether the response is complete and contains all necessary information.

Query: {data.Query}
Response: {data.Response}

Provide a score from 1-5 where:
1 = Missing critical information
2 = Incomplete with significant gaps
3 = Contains basic necessary information
4 = Complete with good detail
5 = Exceptionally complete and thorough

Respond ONLY with valid JSON in this format:
{{
  ""score"": <number 1-5>,
  ""reason"": ""<brief explanation>""
}}";

        return await GetEvaluationFromLLMAsync(prompt, 3.0);
    }

    private async Task<EvaluationMetric> GetEvaluationFromLLMAsync(string prompt, double threshold)
    {
        try
        {
            var response = await _evaluatorClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)]);
            var content = response.Text ?? "{}";
            
            // Extract JSON from the response (might have markdown code blocks)
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var evaluation = JsonSerializer.Deserialize<LLMEvaluationResponse>(jsonContent);
                
                if (evaluation != null)
                {
                    return new EvaluationMetric
                    {
                        Score = evaluation.Score,
                        Reason = evaluation.Reason,
                        Passed = evaluation.Score >= threshold,
                        Threshold = threshold
                    };
                }
            }

            return new EvaluationMetric
            {
                Score = 0,
                Reason = "Failed to parse evaluation response",
                Passed = false,
                Threshold = threshold
            };
        }
        catch (Exception ex)
        {
            return new EvaluationMetric
            {
                Score = 0,
                Reason = $"Evaluation error: {ex.Message}",
                Passed = false,
                Threshold = threshold
            };
        }
    }

    private class LLMEvaluationResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("score")]
        public double Score { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}

public class EvaluationResults
{
    public string Query { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public EvaluationMetric? ToolCallAccuracy { get; set; }
    public EvaluationMetric? IntentResolution { get; set; }
    public EvaluationMetric? TaskAdherence { get; set; }
    public EvaluationMetric? ResponseCompleteness { get; set; }
}

public class EvaluationMetric
{
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public double Threshold { get; set; }
}
