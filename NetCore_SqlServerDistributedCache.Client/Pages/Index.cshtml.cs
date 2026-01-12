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

        /* Properties section ********************************************************************************/

        public List<KeyValuePair<string, string>> CacheValueList { get; set; } = [];

        [BindProperty] public string CacheKeyName { get; set; } = string.Empty;
        [BindProperty] public string CacheKeyValue { get; set; } = string.Empty;



        /* Private Methods section ********************************************************************************/

        private async Task<string> GetPersonSerializedFromSession()
        {
            var p1 = await _cache.GetAsync<Person>(CacheKeys.Person);
            return JsonSerializer.Serialize(p1);
        }

        private async Task SetCacheValueList()
        {
            IEnumerable<string> keys = await _cache.GetKeysAsync(true);
            foreach (string key in keys)
            {
                string? cacheValue = await _cache.GetAsync<string>(key);
                if (cacheValue is not null)
                {
                    KeyValuePair<string, string> kvp = new(key, cacheValue);
                    CacheValueList.Add(kvp);
                }
            }
        }
        
        
        /* Public Methods section ********************************************************************************/       

        public async Task OnGet()
        {
            // AppCache work

            // Setup the list of KeyValue Cache values
            await SetCacheValueList();
        }

        public async Task OnPostSetCacheItem()
        {   
            await _cache.SetAsync(CacheKeyName, CacheKeyValue);
            await SetCacheValueList();
        }

        public async Task OnPostRemoveCacheItem(string key)
        {
            await _cache.RemoveAsync(key);
            await SetCacheValueList();
        }

        public async Task OnPostClearCache()
        {
            await _cache.ClearAsync();
            await SetCacheValueList();
        }

    }
}
