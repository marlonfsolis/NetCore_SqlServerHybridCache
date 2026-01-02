using NetCore_SqlServerHybridCache.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();


builder.Services.AddHybridCacheStore();

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

app.UseHybridCacheStore();

app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.MapDefaultEndpoints();

app.Run();
