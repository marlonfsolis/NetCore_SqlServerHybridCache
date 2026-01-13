using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NetCore_SqlServerDistributedCache.Client.Pages;

public class SessionServiceTestsModel : PageModel
{
    private readonly ISessionService _session;

    public SessionServiceTestsModel(ISessionService session)
    {
        _session = session;
    }

    /* Properties section ********************************************************************************/

    public List<KeyValuePair<string, string>> SessionValueList { get; set; } = [];

    [BindProperty] public string SessionKeyName { get; set; } = string.Empty;
    [BindProperty] public string SessionKeyValue { get; set; } = string.Empty;


    /* Private Methods section ********************************************************************************/

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
        // Set up the list of KeyValue Cache/Session values
        await SetSessionValueList();
    }

    public async Task OnPostSetSessionItem()
    {
        await _session.SetAsync(SessionKeyName, SessionKeyValue);
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

    public async Task OnPostDeleteSession()
    {
        await _session.DeleteAsync();
        await SetSessionValueList();
    }
}
