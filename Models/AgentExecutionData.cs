using System.Text.Json.Serialization;

namespace MafEvaluationSample.Models;

public class AgentExecutionData
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("tool_calls")]
    public List<ToolCallInfo> ToolCalls { get; set; } = new();

    [JsonPropertyName("messages")]
    public List<MessageInfo> Messages { get; set; } = new();
}

public class ToolCallInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, object> Arguments { get; set; } = new();

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }
}

public class MessageInfo
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("tool_calls")]
    public List<ToolCallInfo>? ToolCalls { get; set; }
}

public class ToolDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
}
