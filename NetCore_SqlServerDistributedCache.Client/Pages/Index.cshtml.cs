using Microsoft.AspNetCore.Mvc;
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

        [BindProperty]
        public string NewName { get; set; } = string.Empty;
        public string CachePerson { get; set; } = string.Empty;



        /* Methods section */

        private async Task<string> GetPersonSerializedFromSession()
        {
            var p1 = await _cache.Get<Person>(CacheKeys.Person);
            return JsonSerializer.Serialize(p1);
        }

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

        public async Task OnPostUpdateName()
        {
            await _cache.Set(CacheKeys.Person, new Person() { Name = NewName, Age = 36 });
            CachePerson = await GetPersonSerializedFromSession();
        }
    }
}
