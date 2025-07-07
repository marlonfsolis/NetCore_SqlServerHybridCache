using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;

var builder = WebApplication.CreateBuilder(args);

// Add Hybrid Caching services to the container.
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        
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

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
