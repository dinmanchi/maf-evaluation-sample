# Azure AI Foundry Cloud Evaluation - Setup Guide

## Quick Configuration (Manual)

Since you mentioned you already have an AI Foundry project, here's how to configure it:

### Step 1: Get Your Project Endpoint

Your AI Foundry project endpoint has this format:
```
https://<region>.services.ai.azure.com/api/projects/<project-name>
```

**How to find it:**
1. Go to [Azure AI Foundry](https://ai.azure.com)
2. Open your project
3. Look for **Project connection string** or **Endpoint** in settings
4. It looks like: `https://eastus2.services.ai.azure.com/api/projects/my-project`

### Step 2: Update .env File

Add these lines to your `.env` file:

```bash
# Azure AI Foundry Project (for cloud evaluation)
PROJECT_ENDPOINT=https://YOUR-REGION.services.ai.azure.com/api/projects/YOUR-PROJECT-NAME
MODEL_DEPLOYMENT_NAME=gpt-5-test-eval

# Optional: If different from AZURE_OPENAI_ENDPOINT
# MODEL_ENDPOINT=https://dinak-mi2p8ds2-eastus2.cognitiveservices.azure.com
```

### Step 3: Verify Storage Account Connection

Cloud evaluation requires a storage account connected to your project.

**Check in Azure Portal:**
1. Go to your AI Foundry project resource
2. Look under **Settings** → **Storage**
3. Ensure a storage account is connected

**Or use Azure CLI:**
```bash
az resource show \
  --name <your-project-name> \
  --resource-group <your-resource-group> \
  --resource-type "Microsoft.MachineLearningServices/workspaces" \
  --query "properties.storageAccount"
```

If no storage account is connected, see: [Connect Storage Account](https://learn.microsoft.com/azure/ai-foundry/how-to/evaluations-storage-account)

---

## Implementation Example

Once configured, here's how to use cloud evaluation:

### Create CloudEvaluationService.cs

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.AI.Projects;
using Azure.Identity;
using MafEvaluationSample.Models;

namespace MafEvaluationSample.Services;

public class CloudEvaluationService
{
    private readonly AIProjectClient _projectClient;
    private readonly string _modelDeploymentName;

    public CloudEvaluationService(string projectEndpoint, string modelDeploymentName)
    {
        _projectClient = new AIProjectClient(
            new Uri(projectEndpoint),
            new DefaultAzureCredential()
        );
        _modelDeploymentName = modelDeploymentName;
    }

    public async Task<string> RunCloudEvaluationAsync(AgentExecutionData executionData)
    {
        // 1. Create JSONL dataset
        var datasetPath = "evaluation_data.jsonl";
        await CreateDatasetFileAsync(executionData, datasetPath);

        // 2. Upload dataset
        Console.WriteLine("Uploading evaluation dataset...");
        var dataset = await _projectClient.Datasets.UploadFileAsync(
            name: "agent-eval-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
            version: "1.0",
            filePath: datasetPath
        );

        // 3. Configure evaluators
        var evaluators = new Dictionary<string, EvaluatorConfiguration>
        {
            ["ToolCallAccuracy"] = new EvaluatorConfiguration(
                id: "azureml://registries/azureml/models/ToolCallAccuracy/versions/1"
            )
            {
                DataMapping = new Dictionary<string, string>
                {
                    ["query"] = "${data.query}",
                    ["response"] = "${data.response}",
                    ["tool_calls"] = "${data.tool_calls}"
                }
            },
            ["IntentResolution"] = new EvaluatorConfiguration(
                id: "azureml://registries/azureml/models/IntentResolution/versions/1"
            )
            {
                InitParams = new Dictionary<string, object>
                {
                    ["deployment_name"] = _modelDeploymentName
                },
                DataMapping = new Dictionary<string, string>
                {
                    ["query"] = "${data.query}",
                    ["response"] = "${data.response}"
                }
            },
            ["TaskAdherence"] = new EvaluatorConfiguration(
                id: "azureml://registries/azureml/models/TaskAdherence/versions/1"
            )
            {
                InitParams = new Dictionary<string, object>
                {
                    ["deployment_name"] = _modelDeploymentName
                },
                DataMapping = new Dictionary<string, string>
                {
                    ["query"] = "${data.query}",
                    ["response"] = "${data.response}"
                }
            }
        };

        // 4. Create evaluation
        var evaluation = new Evaluation(
            data: new InputData { Id = dataset.Id },
            evaluators: evaluators
        )
        {
            DisplayName = "Agent Evaluation - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Description = $"Evaluating query: {executionData.Query}"
        };

        // 5. Submit evaluation
        Console.WriteLine("Submitting cloud evaluation...");
        var evaluationsClient = _projectClient.GetEvaluationsClient();
        var result = await evaluationsClient.CreateAsync(evaluation);

        Console.WriteLine($"✅ Evaluation submitted: {result.Value.Name}");
        Console.WriteLine($"   Status: {result.Value.Status}");
        
        // Clean up temp file
        File.Delete(datasetPath);

        return result.Value.Name;
    }

    private async Task CreateDatasetFileAsync(AgentExecutionData data, string filePath)
    {
        // Convert to JSONL format required by Azure AI Foundry
        var dataPoint = new
        {
            query = data.Query,
            response = data.Response,
            tool_calls = data.ToolCalls.Select(tc => new
            {
                name = tc.Name,
                arguments = tc.Arguments,
                result = tc.Result
            })
        };

        var jsonLine = JsonSerializer.Serialize(dataPoint);
        await File.WriteAllTextAsync(filePath, jsonLine + "\n");
    }

    public async Task<string> GetEvaluationStatusAsync(string evaluationName)
    {
        var evaluationsClient = _projectClient.GetEvaluationsClient();
        var result = await evaluationsClient.GetAsync(evaluationName);
        
        return result.Value.Status;
    }
}
```

### Update Program.cs

```csharp
// Add after local evaluation
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")))
{
    Console.WriteLine("\n=== Running Cloud Evaluation ===\n");
    
    var cloudEvaluationService = new CloudEvaluationService(
        projectEndpoint: Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!,
        modelDeploymentName: Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!
    );
    
    try
    {
        var evaluationName = await cloudEvaluationService.RunCloudEvaluationAsync(executionData);
        
        Console.WriteLine($"\n📊 View results in Azure AI Foundry:");
        Console.WriteLine($"   https://ai.azure.com/projects/evaluations/{evaluationName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Cloud evaluation failed: {ex.Message}");
        Console.WriteLine("   Continuing with local evaluation results...");
    }
}
```

---

## Testing Your Configuration

Before running cloud evaluation, test your connection:

```csharp
// Test connection
try
{
    var testClient = new AIProjectClient(
        new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
        new DefaultAzureCredential()
    );
    
    // Try to list connections
    var connections = testClient.Connections.GetConnections();
    Console.WriteLine("✅ Successfully connected to AI Foundry project");
    
    foreach (var conn in connections)
    {
        Console.WriteLine($"   Connection: {conn.Name} ({conn.ConnectionType})");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Connection failed: {ex.Message}");
}
```

---

## Common Issues & Solutions

### Issue 1: "Storage account not connected"

**Solution:**
1. Go to Azure Portal → Your AI Foundry project
2. Add a storage account under Settings → Storage
3. Grant "Storage Blob Data Contributor" role to your user identity

### Issue 2: "Evaluator not found"

**Solution:**
The evaluator IDs might be different. Check the evaluator library in Azure AI Foundry portal:
- Go to Evaluation → Evaluator Library
- Find the correct evaluator IDs

### Issue 3: "Authentication failed"

**Solution:**
```bash
# Ensure you're logged in
az login

# Grant yourself access to the project
az role assignment create \
  --assignee <your-email> \
  --role "Azure AI Developer" \
  --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.MachineLearningServices/workspaces/<project-name>
```

---

## Quick Start (Minimal Setup)

If you just want to try it quickly:

```bash
# 1. Add to .env
echo 'PROJECT_ENDPOINT=https://<region>.services.ai.azure.com/api/projects/<project-name>' >> .env

# 2. Test connection
dotnet run

# 3. If it works, cloud evaluation will run automatically!
```

---

## What You Get

**Local Evaluation (LLM-as-Judge):**
- ✅ Runs immediately
- ✅ Detailed reasoning
- ✅ No infrastructure required

**Cloud Evaluation:**
- ✅ Centralized logging in Azure AI Foundry
- ✅ Historical tracking
- ✅ Team collaboration
- ✅ Standardized metrics
- ✅ CI/CD integration ready

---

## Need Help Finding Your Project Details?

Run these commands:

```bash
# Find your subscription
az account show

# Find resources in resource group
az resource list --resource-group <your-rg> --output table

# Get workspace/project details
az ml workspace show --name <project-name> --resource-group <rg-name>
```

Or contact me with:
- Your Azure subscription name
- Resource group name
- Project name

And I'll help you get the exact endpoint!
