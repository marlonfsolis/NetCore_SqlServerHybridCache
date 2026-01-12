using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NetCore_SqlServerDistributedCache.Client.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ICacheService _cache;
        private readonly ISessionService _session;

        public IndexModel(
            ICacheService cache)
        {
            _cache = cache;
        }

        /* Properties section ********************************************************************************/

        public List<KeyValuePair<string, string>> CacheValueList { get; set; } = [];

        [BindProperty] public string CacheKeyName { get; set; } = string.Empty;
        [BindProperty] public string CacheKeyValue { get; set; } = string.Empty;



        /* Private Methods section ********************************************************************************/

        private async Task SetCacheValueList()
        {
            IEnumerable<string> keys = await _cache.GetKeysAsync(true);
            foreach (string key in keys)
            {
                string? cacheValue = await _cache.GetAsync<string>(key);
                if (cacheValue is null) continue;
                KeyValuePair<string, string> kvp = new(key, cacheValue);
                CacheValueList.Add(kvp);
            }
        }




        /* Public Methods section ********************************************************************************/

        public async Task OnGet()
        {
            // AppCache work

            // Set up the list of KeyValue Cache/Session values
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
