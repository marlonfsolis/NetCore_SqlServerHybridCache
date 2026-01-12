using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        public List<KeyValuePair<string, string>> SessionValueList { get; set; } = [];

        [BindProperty] public string CacheKeyName { get; set; } = string.Empty;
        [BindProperty] public string CacheKeyValue { get; set; } = string.Empty;
        [BindProperty] public string SessionKeyName { get; set; } = string.Empty;
        [BindProperty] public string SessionKeyValue { get; set; } = string.Empty;



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

        private async Task SetSessionValueList()
        {
            IEnumerable<string> keys = await _session.GetKeysAsync();
            foreach (string key in keys)
            {
                string? sessionValue = await _session.GetAsync<string>(key);
                if (sessionValue is null) continue;
                KeyValuePair<string, string> kvp = new(key, sessionValue);
                SessionValueList.Add(kvp);
            }
        }


        /* Public Methods section ********************************************************************************/

        public async Task OnGet()
        {
            // AppCache work

            // Set up the list of KeyValue Cache/Session values
            await SetCacheValueList();
            await SetSessionValueList();
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

        
        public async Task OnPostSetSessionItem()
        {   
            await _session.SetAsync(CacheKeyName, CacheKeyValue);
            await SetSessionValueList();
        }

        public async Task OnPostRemoveSessionItem(string key)
        {
            await _session.RemoveAsync(key);
            await SetSessionValueList();
        }

        public async Task OnPostClearSession()
        {
            await _session.ClearAsync();
            await SetSessionValueList();
        }        
    }
}
