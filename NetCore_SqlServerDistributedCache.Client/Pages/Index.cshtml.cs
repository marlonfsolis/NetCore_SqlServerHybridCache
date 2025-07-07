using Microsoft.AspNetCore.Mvc.RazorPages;
using NetCore_SqlServerDistributedCache.Models;
using NetCore_SqlServerDistributedCache.Shared.Constants;
using System.Text.Json;

namespace NetCore_SqlServerDistributedCache.Client.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ICacheService _cache;

        public IndexModel(ILogger<IndexModel> logger, ICacheService cache)
        {
            _logger = logger;
            _cache = cache;
        }

        /* Properties section */

        public string SessionPersonName { get; set; } = string.Empty;
        public string CachePerson { get; set; } = string.Empty;



        /* Methods section */

        public async Task OnGet()
        {
            // AppCache work
            Person? p1 = await _cache.Get<Person?>(CacheKeys.Person);
            if (p1 is null)
            {
                await _cache.Set(CacheKeys.Person, new Person() { Name = "Yenni", Age = 36 });
            }

            p1 = await _cache.Get<Person>(CacheKeys.Person);
            CachePerson = p1 is null ? string.Empty : JsonSerializer.Serialize(p1);
        }
    }
}
