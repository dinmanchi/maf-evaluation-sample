var builder = DistributedApplication.CreateBuilder(args);

// Add the MAF evaluation console app
var consoleApp = builder.AddProject<Projects.MafEvaluation_ConsoleApp>("maf-evaluation-consoleapp")
    .WithReplicas(1);

builder.Build().Run();
