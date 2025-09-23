using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add Hybrid Caching services to the container.
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        // Set default options for cache entries here
        Expiration = TimeSpan.FromHours(24)
    };
});

builder.Services.AddSingleton<ICacheService>(service =>
{
    HybridCache cache = service.GetRequiredService<HybridCache>();
    IConfiguration configuration = service.GetRequiredService<IConfiguration>();

    ICacheService cacheService = new CacheService(cache, configuration, "App_", TimeSpan.FromMinutes(10));

    return cacheService;
});

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.MapDefaultEndpoints();

app.Run();
