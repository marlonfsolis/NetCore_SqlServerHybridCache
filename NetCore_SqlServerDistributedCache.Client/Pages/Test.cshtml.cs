using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NetCore_SqlServerDistributedCache.Client.Pages;

public class TestModel : PageModel
{
    private readonly ICacheService _cacheService;

    public TestModel(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public string? TestKeyValue { get; set; } = string.Empty;

    public void OnGet()
    {
        _cacheService.Set<string>("TestKey", "This is a test value.");
    }

    public async Task OnGetResult()
    {
        TestKeyValue = await _cacheService.GetAsync<string>("TestKey");
        if (TestKeyValue is null)
        {
            TestKeyValue = "Key not found in cache.";
        }
    }
}
