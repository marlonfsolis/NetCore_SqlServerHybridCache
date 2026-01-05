namespace NetCore_SqlServerDistributedCache.Client.Middleware;

public class AuthenticationAppMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationAppMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Here you can add your authentication logic


        await _next(context);

    }
}
