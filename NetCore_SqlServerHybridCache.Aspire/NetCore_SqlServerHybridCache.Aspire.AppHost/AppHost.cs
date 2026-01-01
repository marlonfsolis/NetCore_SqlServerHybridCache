var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.NetCore_SqlServerHybridCache_Aspire_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.NetCore_SqlServerHybridCache_Aspire_Web>("BlazorWebApp")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.NetCore_SqlServerDistributedCache_Client>("HybridCacheWebApp")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.NetCore_SqlServerDistributedCache_ReverseProxy>("ReverseProxy");

builder.Build().Run();
