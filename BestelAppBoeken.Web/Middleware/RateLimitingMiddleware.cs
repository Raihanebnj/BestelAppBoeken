using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BestelAppBoeken.Web.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clientRequests = new();
        private const int MAX_REQUESTS_PER_MINUTE = 100;
        private const int TIME_WINDOW_MINUTES = 1;

        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);
            var now = DateTime.UtcNow;

            var requestInfo = _clientRequests.GetOrAdd(clientId,
                id => new ClientRequestInfo { FirstRequest = now, RequestCount = 0 });

            // Reset counter if time window expired
            if ((now - requestInfo.FirstRequest).TotalMinutes > TIME_WINDOW_MINUTES)
            {
                requestInfo.FirstRequest = now;
                requestInfo.RequestCount = 0;
            }

            requestInfo.RequestCount++;

            if (requestInfo.RequestCount > MAX_REQUESTS_PER_MINUTE)
            {
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsync("Rate limit exceeded. Maximum 100 requests per minute.");
                return;
            }

            _clientRequests[clientId] = requestInfo;

            // rate limit headers
            context.Response.Headers.Add("X-RateLimit-Limit", MAX_REQUESTS_PER_MINUTE.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining",
                (MAX_REQUESTS_PER_MINUTE - requestInfo.RequestCount).ToString());
            context.Response.Headers.Add("X-RateLimit-Reset",
                requestInfo.FirstRequest.AddMinutes(TIME_WINDOW_MINUTES).ToString("R"));

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Use API Key if available, otherwise IP address
            if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
                return $"api-key:{apiKey}";

            return $"ip:{context.Connection.RemoteIpAddress}";
        }

        private class ClientRequestInfo
        {
            public DateTime FirstRequest { get; set; }
            public int RequestCount { get; set; }
        }
    }
}