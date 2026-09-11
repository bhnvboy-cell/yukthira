namespace YuktiraERP.Web.Middleware;

public class ApiReverseProxyMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HttpClient _httpClient = new() { BaseAddress = new Uri("http://localhost:5000") };

    public ApiReverseProxyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var targetRequest = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = new Uri("http://localhost:5000" + context.Request.Path + context.Request.QueryString)
            };

            if (context.Request.ContentLength > 0)
            {
                var bodyStream = new StreamReader(context.Request.Body);
                var body = await bodyStream.ReadToEndAsync();
                targetRequest.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            }

            foreach (var header in context.Request.Headers)
            {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    continue;
                targetRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            try
            {
                var response = await _httpClient.SendAsync(targetRequest);
                context.Response.StatusCode = (int)response.StatusCode;

                var skipHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Transfer-Encoding", "Connection", "Content-Type"
                };

                foreach (var header in response.Headers)
                {
                    if (!skipHeaders.Contains(header.Key))
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                if (response.Content.Headers.ContentType != null)
                    context.Response.ContentType = response.Content.Headers.ContentType.ToString();

                var responseContent = await response.Content.ReadAsStringAsync();
                await context.Response.WriteAsync(responseContent);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 502;
                await context.Response.WriteAsync($"{{\"error\":\"API unreachable: {ex.Message}\"}}");
            }
            return;
        }
        await _next(context);
    }
}
