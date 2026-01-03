var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.NetCore_SqlServerHybridCache_Aspire_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.NetCore_SqlServerHybridCache_Aspire_Web>("BlazorWebApp")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.NetCore_SqlServerDistributedCache_Client>("HybridCacheWebApp-1")
    .WithHttpEndpoint(name: "HybridCacheWebApp-1", port: 5001)
    .WithEnvironment("InstanceName", "Instance1")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    //.WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.NetCore_SqlServerDistributedCache_Client>("HybridCacheWebApp-2")
    .WithHttpEndpoint(name: "HybridCacheWebApp-2", port: 5002)
    .WithEnvironment("InstanceName", "Instance2")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    //.WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.NetCore_SqlServerDistributedCache_Client>("HybridCacheWebApp-3")
    .WithHttpEndpoint(name: "HybridCacheWebApp-3", port: 5003)
    .WithEnvironment("InstanceName", "Instance3")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    //.WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.NetCore_SqlServerDistributedCache_ReverseProxy>("ReverseProxy");

builder.AddProject<Projects.NetCore_SqlServerHybridCache_Test>("HybridCache-Test");

builder.Build().Run();
