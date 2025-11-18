using System;
using System.Collections.Generic;
using System.Linq;
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

    public async Task TestConnectionAsync()
    {
        Console.WriteLine("Testing connection to Azure AI Foundry project...");
        
        try
        {
            // Test by listing connections
            var connections = _projectClient.Connections.GetConnections();
            Console.WriteLine("✅ Successfully connected to AI Foundry project");
            
            var connectionList = connections.ToList();
            if (connectionList.Any())
            {
                Console.WriteLine($"   Found {connectionList.Count} connection(s):");
                foreach (var conn in connectionList)
                {
                    Console.WriteLine($"      - {conn.Name}");
                }
            }
            else
            {
                Console.WriteLine("   No connections found in project.");
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection test failed: {ex.Message}");
            Console.WriteLine($"   Error type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner error: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    public string GetProjectInfo()
    {
        return $"Connected to Azure AI Foundry project using model deployment: {_modelDeploymentName}";
    }
}
