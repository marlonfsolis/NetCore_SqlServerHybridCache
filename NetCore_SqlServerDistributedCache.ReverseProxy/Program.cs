var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();


var app = builder.Build();

app.UseHttpsRedirection();

app.MapReverseProxy();

app.MapHealthChecks("/healthz");

app.Run();
