using System.Collections.Concurrent;

namespace Game.ApiGateway.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, DateTime> RequestTimestamps = new();
        private static readonly ConcurrentDictionary<string, int> RequestCounts = new();
        private const int RequestsPerMinute = 60;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var now = DateTime.UtcNow;

            // Clean up old timestamps
            var cutoffTime = now.AddMinutes(-1);
            var expiredKeys = RequestTimestamps
                .Where(kv => kv.Value < cutoffTime)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                RequestTimestamps.TryRemove(key, out _);
                RequestCounts.TryRemove(key, out _);
            }

            // Check if client is rate limited
            if (RequestTimestamps.ContainsKey(clientIp) && RequestCounts[clientIp] >= RequestsPerMinute)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Update timestamp and count
            RequestTimestamps[clientIp] = now;
            RequestCounts[clientIp] = RequestCounts.GetValueOrDefault(clientIp) + 1;

            await _next(context);
        }
    }
}