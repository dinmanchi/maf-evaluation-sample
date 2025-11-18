# Evaluation Options for Microsoft Agent Framework (.NET)

## Current Implementation: LLM-as-Judge (Local Evaluation) ✅

### What We Have Now
- **Custom evaluators** using GPT model to judge agent performance
- **Four metrics**: ToolCallAccuracy, IntentResolution, TaskAdherence, ResponseCompleteness
- **Scores**: 1-5 scale with pass/fail thresholds
- **Detailed reasoning**: Each score includes explanation
- **Running**: Locally on your machine, instant results

### Pros
✅ **Working right now** - No additional setup required  
✅ **Flexible** - Easy to customize evaluation prompts and criteria  
✅ **Fast** - Results in seconds  
✅ **Transparent** - See exactly what prompts are used  
✅ **No infrastructure** - No Azure AI Foundry project needed  
✅ **Detailed explanations** - LLM provides reasoning for each score  

### Cons
⚠️ **Costs LLM tokens** - Each evaluation requires API calls  
⚠️ **Slight variability** - Scores may vary slightly between runs  
⚠️ **Not standardized** - Custom implementation, not Azure's official evaluators  

### Code Example (What's Running Now)
```csharp
var evaluationService = new EvaluationService(iChatClient);
var results = await evaluationService.EvaluateAgentExecutionAsync(executionData);

// Output:
// Tool Call Accuracy: 5.0/5 ✅
// Intent Resolution: 4.0/5 ✅
// Task Adherence: 5.0/5 ✅
// Response Completeness: 3.0/5 ✅
```

---

## Alternative: Azure AI Foundry Cloud Evaluation (Available)

### What's Available
- **Azure.AI.Projects** package (just installed v1.2.0-beta.3)
- **Cloud-based evaluation** using Azure AI Foundry
- **Built-in evaluators** including agentic metrics
- **Centralized results** in Azure AI Foundry portal
- **Batch evaluation** on datasets

### Pros
✅ **Official Azure evaluators** - Standardized metrics  
✅ **Centralized logging** - Results stored in Azure AI Foundry  
✅ **Batch processing** - Evaluate multiple queries at once  
✅ **No local compute** - Runs in Azure  
✅ **Integrated monitoring** - Track evaluations over time  

### Cons
⚠️ **Requires Azure AI Foundry project** - Additional infrastructure setup  
⚠️ **More complex** - Need to upload datasets, configure project  
⚠️ **Storage account required** - For evaluation data  
⚠️ **Regional limitations** - Only certain Azure regions supported  
⚠️ **Beta/Preview** - API may change  

### Requirements
1. **Azure AI Foundry Project** (need to create one)
2. **Storage Account** connected to project
3. **Azure OpenAI deployment** (we already have this)
4. **Dataset upload** (JSONL format)

### Code Example (How It Would Work)
```csharp
// 1. Create AI Project client
var projectClient = new AIProjectClient(
    new Uri("https://<account>.services.ai.azure.com/api/projects/<project>"),
    new DefaultAzureCredential()
);

// 2. Upload evaluation dataset
var dataId = await projectClient.Datasets.UploadFileAsync(
    name: "agent-eval-data",
    version: "1.0",
    filePath: "./evaluation_data.jsonl"
);

// 3. Configure evaluators
var evaluators = new Dictionary<string, EvaluatorConfiguration>
{
    ["ToolCallAccuracy"] = new EvaluatorConfiguration("ToolCallAccuracyEvaluator")
    {
        DataMapping = new Dictionary<string, string>
        {
            ["query"] = "${data.query}",
            ["response"] = "${data.response}",
            ["tool_calls"] = "${data.tool_calls}"
        }
    },
    ["IntentResolution"] = new EvaluatorConfiguration("IntentResolutionEvaluator"),
    ["TaskAdherence"] = new EvaluatorConfiguration("TaskAdherenceEvaluator")
};

// 4. Create and run evaluation
var evaluation = new Evaluation(
    data: new InputData { Id = dataId },
    evaluators: evaluators
)
{
    DisplayName = "Agent Evaluation Run",
    Description = "Evaluating weather agent"
};

var evaluationsClient = projectClient.GetEvaluationsClient();
var result = await evaluationsClient.CreateAsync(evaluation);

// 5. View results in Azure AI Foundry portal
Console.WriteLine($"Evaluation: {result.Name}");
Console.WriteLine($"Status: {result.Status}");
```

---

## Recommendation

### **For This Sample: Keep LLM-as-Judge** ✅

**Reasons:**
1. **Already working** - No need to set up additional Azure infrastructure
2. **Simple to understand** - Good for learning and demonstration
3. **Flexible** - Easy to modify evaluation criteria
4. **Fast iteration** - Immediate results without cloud round-trips

### **When to Use Azure AI Foundry Cloud Evaluation:**

Use cloud evaluation when you:
- Need **standardized metrics** for compliance/reporting
- Want **centralized evaluation tracking** across teams
- Have **large datasets** (hundreds/thousands of queries)
- Need **continuous monitoring** in production
- Want **built-in CI/CD integration**
- Require **historical comparison** of evaluation runs

---

## Next Steps (If You Want Cloud Evaluation)

### Setup Required:
```bash
# 1. Create Azure AI Foundry project (via portal)
# 2. Connect storage account
# 3. Add environment variables

echo 'PROJECT_ENDPOINT=https://<account>.services.ai.azure.com/api/projects/<project>' >> .env
```

### Update Code:
Would need to:
1. Convert `executionData` to JSONL format
2. Upload to Azure storage
3. Configure project client
4. Map data to evaluator inputs
5. Poll for results

**Estimated effort**: 1-2 hours setup + code changes

---

## Hybrid Approach (Best of Both)

You could use **both**:
- **LLM-as-Judge** for quick local iteration and development
- **Cloud Evaluation** for official/production evaluation runs

This gives you flexibility during development and standardization for production.

---

## Summary

| Feature | LLM-as-Judge (Current) | Cloud Evaluation |
|---------|----------------------|------------------|
| **Setup time** | ✅ Done | ⚠️ 1-2 hours |
| **Cost** | LLM tokens per eval | LLM tokens + storage |
| **Speed** | ✅ Seconds | Minutes |
| **Customization** | ✅ Full control | Standard evaluators |
| **Tracking** | Local logs | ✅ Azure portal |
| **Production ready** | Good for demos | ✅ Enterprise |
| **Our current status** | ✅ **Working** | Package installed, not configured |

**Current implementation matches the Python example's functionality** and provides the same metrics. Cloud evaluation would add enterprise features but isn't necessary for this sample.
