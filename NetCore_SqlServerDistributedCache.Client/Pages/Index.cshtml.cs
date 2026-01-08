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
        private readonly ISessionService _session;

        public IndexModel(
            ILogger<IndexModel> logger, 
            ICacheService cache,
            ISessionService session)
        {
            _logger = logger;
            _cache = cache;
            _session = session;
        }

        /* Properties section */

        [BindProperty]
        public string NewName { get; set; } = string.Empty;
        public string CachePerson { get; set; } = string.Empty;
        [BindProperty]
        public int ArraySize { get; set; }



        /* Methods section */

        private async Task<string> GetPersonSerializedFromSession()
        {
            var p1 = await _cache.GetAsync<Person>(CacheKeys.Person);
            return JsonSerializer.Serialize(p1);
        }

        public async Task OnGet()
        {
            // AppCache work
            Person? p1 = await _cache.GetAsync<Person?>(CacheKeys.Person);
            if (p1 is null)
            {
                await _cache.SetAsync(CacheKeys.Person, new Person() { Name = "Yenni", Age = 36 });
            }

            p1 = await _cache.GetAsync<Person>(CacheKeys.Person);
            CachePerson = p1 is null ? string.Empty : JsonSerializer.Serialize(p1);
        }

        public async Task OnPostUpdateName()
        {
            await _cache.SetAsync(CacheKeys.Person, new Person() { Name = NewName, Age = 36 });
            CachePerson = await GetPersonSerializedFromSession();
        }

        public async Task OnPostReplaceName()
        {
            // Remove first
            await _cache.RemoveAsync(CacheKeys.Person);

            // Then add new
            await _cache.SetAsync(CacheKeys.Person, new Person() { Name = NewName, Age = 36 });
            CachePerson = await GetPersonSerializedFromSession();
        }

        public async Task OnPostClearCache()
        {
            await _cache.ClearAsync();
            CachePerson = string.Empty;
        }

        public async Task OnPostSerializeComparason()
        {
            List<Person> people = new List<Person>(ArraySize);
            for (int i = 0; i < ArraySize; i++)
            {
                Person p = new Person() { Name = $"Name_{i}", Age = i };
                people.Add(p);
            }

            await _session.SetAsync("People", people);
            
            var peopleFromCache = await _session.GetAsync<List<Person>>("People");
        }
    }
}
