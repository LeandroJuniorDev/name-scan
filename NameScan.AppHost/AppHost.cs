var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.NameScan>("namescan")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
