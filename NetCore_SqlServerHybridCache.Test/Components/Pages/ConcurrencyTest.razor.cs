using System.Net.Http;
using System.Threading.Tasks;

namespace NetCore_SqlServerHybridCache.Test.Components.Pages;

public partial class ConcurrencyTest
{
    public string ConcurrencyTestStatus { get; set; } = "Not started";
    public List<string> ConcurrencyTestErrors { get; set; } = [];
    public CancellationTokenSource MyCancellationTokenSource { get; set; } = new CancellationTokenSource();
    public CancellationToken MyCancellationToken { get; set; }

    public async Task ExecConcurrencyTest()
    {
        ConcurrencyTestStatus = "Running...";
        MyCancellationToken = MyCancellationTokenSource.Token;

        var client = new HttpClient();
        var url = "http://localhost:9000/test";

        int totalUsers = 200;   // number of parallel workers
        int requestsPerUser = 50;

        var allTasks = new List<Task>();        

        for (int u = 0; u < totalUsers; u++)
        {
            if (MyCancellationToken.IsCancellationRequested)
            {
                break;
            }

            allTasks.Add(Task.Run(async () =>
            {
                for (int r = 0; r < requestsPerUser; r++)
                {
                    if (MyCancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        var response = await client.GetAsync(url);
                        //Console.WriteLine($"User {u} -> {response.StatusCode}");
                    }
                    catch (Exception ex)
                    {
                        ConcurrencyTestErrors.Add($"User {u} error: {ex.Message}");
                    }
                }
            }, MyCancellationToken));
        }

        await Task.WhenAll(allTasks);

        ConcurrencyTestStatus = "Completed";

    }

    public void CancelConcurrencyTest()
    {
        MyCancellationTokenSource.Cancel();
        ConcurrencyTestStatus = "Cancelled";
    }
}
