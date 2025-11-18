#!/bin/bash

# Azure AI Foundry Cloud Evaluation Setup Script
# This script helps configure your project for cloud-based evaluation

echo "=== Azure AI Foundry Cloud Evaluation Setup ==="
echo ""

# Check if logged in
if ! az account show &> /dev/null; then
    echo "❌ Not logged into Azure. Please run: az login"
    exit 1
fi

echo "✅ Logged into Azure"
echo ""

# Get current subscription
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
echo "📌 Current Subscription: $SUBSCRIPTION_NAME"
echo "   ID: $SUBSCRIPTION_ID"
echo ""

# Find AI Foundry projects (they're workspace resources with kind=project or hub)
echo "🔍 Searching for AI Foundry projects..."
PROJECTS=$(az resource list \
    --resource-type "Microsoft.MachineLearningServices/workspaces" \
    --query "[?tags.CreatedBy=='AIFoundry' || tags.azureml.workspace.type=='project' || contains(id, 'foundry')].{name:name, resourceGroup:resourceGroup, location:location, id:id}" \
    -o json)

if [ "$(echo $PROJECTS | jq length)" -eq "0" ]; then
    echo "⚠️  No AI Foundry projects found automatically."
    echo ""
    echo "Please provide your project details manually:"
    echo ""
    read -p "Project Name: " PROJECT_NAME
    read -p "Resource Group: " RESOURCE_GROUP
    
    # Try to get the project
    PROJECT_ID=$(az resource show --name "$PROJECT_NAME" --resource-group "$RESOURCE_GROUP" --resource-type "Microsoft.MachineLearningServices/workspaces" --query id -o tsv 2>/dev/null)
    
    if [ -z "$PROJECT_ID" ]; then
        echo "❌ Could not find project '$PROJECT_NAME' in resource group '$RESOURCE_GROUP'"
        echo ""
        echo "Available workspaces:"
        az ml workspace list -o table
        exit 1
    fi
else
    echo "✅ Found AI Foundry projects:"
    echo "$PROJECTS" | jq -r '.[] | "   - \(.name) (\(.resourceGroup))"'
    echo ""
    
    # Use first project or let user choose
    PROJECT_NAME=$(echo $PROJECTS | jq -r '.[0].name')
    RESOURCE_GROUP=$(echo $PROJECTS | jq -r '.[0].resourceGroup')
    
    echo "Using project: $PROJECT_NAME"
    echo "Resource group: $RESOURCE_GROUP"
fi

echo ""
echo "🔧 Getting project details..."

# Get project endpoint
# For AI Foundry, the endpoint format is:
# https://<region>.api.azureml.ms/api/projects/<subscription>/<resource-group>/<workspace-name>
LOCATION=$(az ml workspace show -n "$PROJECT_NAME" -g "$RESOURCE_GROUP" --query location -o tsv)
PROJECT_ENDPOINT="https://${LOCATION}.api.azureml.ms/api/projects/${SUBSCRIPTION_ID}/${RESOURCE_GROUP}/${PROJECT_NAME}"

echo "✅ Project Endpoint: $PROJECT_ENDPOINT"
echo ""

# Get Azure OpenAI connection (if exists)
echo "🔍 Looking for Azure OpenAI connections..."
OPENAI_ENDPOINT=$(az cognitiveservices account show -n "dinak-mi2p8ds2-eastus2" -g "rg-foundry-testing" --query properties.endpoint -o tsv 2>/dev/null || echo "")

if [ -n "$OPENAI_ENDPOINT" ]; then
    echo "✅ Found OpenAI endpoint: $OPENAI_ENDPOINT"
else
    echo "⚠️  OpenAI endpoint not found automatically"
    OPENAI_ENDPOINT="https://dinak-mi2p8ds2-eastus2.cognitiveservices.azure.com/"
fi

# Get deployment name from .env
DEPLOYMENT_NAME=$(grep AZURE_OPENAI_DEPLOYMENT .env 2>/dev/null | cut -d'=' -f2 | tr -d '"' || echo "gpt-5-test-eval")

echo ""
echo "=== Configuration Complete ==="
echo ""
echo "Add these to your .env file:"
echo ""
echo "# Azure AI Foundry Project Configuration"
echo "PROJECT_ENDPOINT=$PROJECT_ENDPOINT"
echo "PROJECT_NAME=$PROJECT_NAME"
echo "RESOURCE_GROUP=$RESOURCE_GROUP"
echo "MODEL_DEPLOYMENT_NAME=$DEPLOYMENT_NAME"
echo ""
echo "=== Next Steps ==="
echo ""
echo "1. Verify storage account is connected to your project:"
echo "   az ml workspace show -n '$PROJECT_NAME' -g '$RESOURCE_GROUP' --query storageAccount"
echo ""
echo "2. If no storage account, connect one:"
echo "   https://learn.microsoft.com/azure/ai-foundry/how-to/evaluations-storage-account"
echo ""
echo "3. Update .env file with the configuration above"
echo ""
echo "4. Run: dotnet run (with cloud evaluation enabled)"
echo ""
