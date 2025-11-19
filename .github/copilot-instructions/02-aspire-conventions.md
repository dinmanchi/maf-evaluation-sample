# .NET Aspire Conventions

## Project Structure
```
solution-root/
├── {Solution}.AppHost/          # Orchestration (stays at root)
├── {Solution}.ServiceDefaults/  # Shared infrastructure (stays at root)
├── src/
│   ├── {Solution}.Api/          # API projects
│   ├── {Solution}.Web/          # UI projects
│   ├── {Solution}.ConsoleApp/   # Console applications
│   └── {Solution}.Worker/       # Background workers
└── {Solution}.sln
```

## AppHost Configuration

### Adding Projects
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Console apps
var consoleApp = builder.AddProject<Projects.MafEvaluation_ConsoleApp>("console-app")
    .WithReplicas(1);

// APIs
var api = builder.AddProject<Projects.MafEvaluation_Api>("api")
    .WithExternalHttpEndpoints();

// Web apps
var web = builder.AddProject<Projects.MafEvaluation_Web>("web")
    .WithReference(api);

builder.Build().Run();
```

### Resource Dependencies
```csharp
// Redis cache
var cache = builder.AddRedis("cache");

// PostgreSQL database
var db = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("appdb");

// API with dependencies
var api = builder.AddProject<Projects.MafEvaluation_Api>("api")
    .WithReference(cache)
    .WithReference(db);
```

## ServiceDefaults Integration

### In Each Project
```csharp
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
// or: var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add your services
builder.Services.AddScoped<IMyService, MyService>();

var host = builder.Build();
await host.RunAsync();
```

### What ServiceDefaults Provides
- **Telemetry**: OpenTelemetry with distributed tracing
- **Health Checks**: Automatic health endpoint
- **Service Discovery**: Find other services by name
- **Resilience**: Retry policies and circuit breakers

## Dashboard Access
- Default URL: `https://localhost:17280`
- View logs, traces, metrics, and resource status
- Test service endpoints directly from dashboard

## Environment Variables
Aspire projects automatically load:
- `appsettings.json`
- `appsettings.{Environment}.json`
- User secrets (in development)
- Environment variables

Prefer user secrets or environment variables for sensitive data.

## Naming Conventions
- AppHost project: `{Solution}.AppHost`
- ServiceDefaults: `{Solution}.ServiceDefaults`
- Resource names in AppHost: lowercase with hyphens (e.g., `"api"`, `"worker-service"`)

## Running Projects

### With Aspire Dashboard
```bash
dotnet run --project {Solution}.AppHost
```

### Standalone (without Aspire)
```bash
dotnet run --project src/{Project}/{Project}.csproj
```

## Best Practices
- Keep AppHost and ServiceDefaults at solution root
- All application code goes in `src/` folder
- Use `WithReference()` for service-to-service communication
- Add external endpoints only when needed
- Use resource volumes for stateful services
- Monitor via dashboard during development
