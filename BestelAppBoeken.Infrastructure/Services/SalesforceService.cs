using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class SalesforceService : ISalesforceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SalesforceService> _logger;

        public SalesforceService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SalesforceService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        // Fetch OAuth token from Salesforce using username/password flow (if configured)
        private async Task<(string AccessToken, string InstanceUrl)?> GetSalesforceAccessTokenAsync()
        {
            try
            {
                var authUrl = _configuration["Salesforce:AuthUrl"];
                if (string.IsNullOrWhiteSpace(authUrl)) return null;

                var client = _httpClientFactory.CreateClient();

                var values = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "client_id", _configuration["Salesforce:ConsumerKey"] ?? string.Empty },
                    { "client_secret", _configuration["Salesforce:ConsumerSecret"] ?? string.Empty },
                    { "username", _configuration["Salesforce:Username"] ?? string.Empty },
                    { "password", (_configuration["Salesforce:Password"] ?? string.Empty) + (_configuration["Salesforce:SecurityToken"] ?? string.Empty) }
                };

                var req = new HttpRequestMessage(HttpMethod.Post, authUrl)
                {
                    Content = new FormUrlEncodedContent(values)
                };

                var resp = await client.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return null;

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var accessToken = root.GetProperty("access_token").GetString();
                var instanceUrl = root.GetProperty("instance_url").GetString();
                if (string.IsNullOrWhiteSpace(accessToken)) return null;

                return (accessToken!, instanceUrl!);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Salesforce token fetch failed");
                return null;
            }
        }

        public async Task SyncOrderAsync(Order order)
        {
            // correlationId: use provided or generate a new one
            var correlationId = Guid.NewGuid().ToString();

            var salesforceEndpoint = _configuration["Salesforce:Endpoint"] ??
                                     "https://salesforce.example/api/orders"; // configure in appsettings
            var timeoutSeconds = int.TryParse(_configuration["Salesforce:TimeoutSeconds"], out var t) ? t : 10;
            var maxAttempts = int.TryParse(_configuration["Salesforce:MaxAttempts"], out var m) ? m : 3;

            var client = _httpClientFactory.CreateClient();
            var payload = JsonSerializer.Serialize(order);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            // If no explicit Orders endpoint configured, perform OAuth to obtain instance_url and construct endpoint
            if (string.IsNullOrWhiteSpace(_configuration["Salesforce:Endpoint"]))
            {
                try
                {
                    var tokenResult = await GetSalesforceAccessTokenAsync();
                    if (tokenResult != null)
                    {
                        // prefer configured orders endpoint if present, otherwise build from instance_url
                        var ordersEndpoint = _configuration["Salesforce:OrdersEndpoint"];
                        if (tokenResult.HasValue)
                        {
                            var token = tokenResult.Value;
                            if (string.IsNullOrWhiteSpace(ordersEndpoint) && !string.IsNullOrWhiteSpace(token.InstanceUrl))
                            {
                                // default to sobjects Order (may need adjustment for your org)
                                ordersEndpoint = token.InstanceUrl.TrimEnd('/') + "/services/data/v56.0/sobjects/Order__c";
                            }

                            if (!string.IsNullOrWhiteSpace(ordersEndpoint))
                            {
                                salesforceEndpoint = ordersEndpoint;
                                // attach auth header for subsequent requests
                                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to obtain Salesforce access token - will continue with configured endpoint (if any)");
                }
            }

            HttpResponseMessage? lastResponse = null;
            string? lastResponseBody = null;
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                    var request = new HttpRequestMessage(HttpMethod.Post, salesforceEndpoint)
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("X-Correlation-Id", correlationId);

                    // If we have no auth header yet and config provides client credentials, try setting basic auth or other headers as needed
                    // (Token flow above will set Bearer header when available)

                    _logger.LogInformation("SalesforceSync attempt {Attempt} for Order {OrderId} (CorrelationId: {CorrelationId})", attempt, order.Id, correlationId);

                    var response = await client.SendAsync(request, cts.Token);
                    lastResponse = response;
                    lastResponseBody = await response.Content.ReadAsStringAsync(cts.Token);

                    _logger.LogInformation("Salesforce response for Order {OrderId}: {Status} | CorrelationId: {CorrelationId} | Body: {Body}",
                        order.Id, (int)response.StatusCode, correlationId, lastResponseBody);

                    // Success
                    if ((int)response.StatusCode == 201)
                    {
                        _logger.LogInformation("Salesforce Sync SUCCESS for Order {OrderId} (CorrelationId: {CorrelationId})", order.Id, correlationId);
                        return;
                    }

                    // Business error -> publish to DLQ
                    if ((int)response.StatusCode == 400 || (int)response.StatusCode == 422)
                    {
                        _logger.LogWarning("Salesforce Business Error for Order {OrderId} (Status {Status}) -> pushing to DLQ. CorrelationId: {CorrelationId}", order.Id, (int)response.StatusCode, correlationId);
                        await PublishToDlqAsync(order, "salesforce_business_error", correlationId, lastResponseBody);
                        return;
                    }

                    // Auth issues -> alert/log and stop retrying
                    if ((int)response.StatusCode == 401 || (int)response.StatusCode == 403)
                    {
                        _logger.LogError("Salesforce Authentication Error (Status {Status}) for Order {OrderId}. CorrelationId: {CorrelationId}", (int)response.StatusCode, order.Id, correlationId);
                        // Optionally raise alert here (email/monitoring). Do NOT push to DLQ automatically.
                        return;
                    }

                    // 5xx or other -> treat as transient and retry
                    if ((int)response.StatusCode >= 500)
                    {
                        _logger.LogWarning("Salesforce server error (Status {Status}) for Order {OrderId}, will retry (attempt {Attempt})", (int)response.StatusCode, order.Id, attempt);
                    }
                    else
                    {
                        // Unknown non-success -> treat as transient but will be retried
                        _logger.LogWarning("Salesforce unexpected status {Status} for Order {OrderId}, will retry (attempt {Attempt})", (int)response.StatusCode, order.Id, attempt);
                    }
                }
                catch (TaskCanceledException tex)
                {
                    lastException = tex;
                    _logger.LogWarning(tex, "Salesforce request timed out for Order {OrderId} (attempt {Attempt}) CorrelationId: {CorrelationId}", order.Id, attempt, correlationId);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Exception when calling Salesforce for Order {OrderId} (attempt {Attempt}) CorrelationId: {CorrelationId}", order.Id, attempt, correlationId);
                }

                // Backoff before next attempt (exponential)
                if (attempt < maxAttempts)
                {
                    var delayMs = 250 * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delayMs);
                }
            }

            // Exhausted retries -> push to DLQ with reason = timeout/retry-exhausted
            var reason = lastException != null ? "salesforce_exception" : "salesforce_retry_exhausted";
            var responseInfo = lastResponseBody ?? lastException?.Message ?? string.Empty;
            _logger.LogError("Salesforce retry exhausted for Order {OrderId} -> pushing to DLQ (reason: {Reason}). CorrelationId: {CorrelationId}", order.Id, reason, correlationId);

            await PublishToDlqAsync(order, reason, correlationId, responseInfo);
        }

        private async Task PublishToDlqAsync(Order order, string reason, string correlationId, string? responseBody = null)
        {
            // Publish original order JSON to the DLQ with headers (do not change order payload)
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            var queueName = _configuration["RabbitMq:QueueName"] ?? "orders";
            var dlqName = queueName + "-dlq";

            var body = JsonSerializer.Serialize(order);
            var bytes = Encoding.UTF8.GetBytes(body);

            try
            {
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                // Ensure DLQ exists
                await channel.QueueDeclareAsync(queue: dlqName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var props = new BasicProperties();
                props.Persistent = true;

                // Add headers with context info
                props.Headers ??= new Dictionary<string, object>();
                props.Headers["x-dlq-reason"] = reason;
                props.Headers["x-correlation-id"] = correlationId;
                if (!string.IsNullOrEmpty(responseBody))
                {
                    props.Headers["x-salesforce-response"] = responseBody;
                }

                // Publish into DLQ
                await channel.BasicPublishAsync(exchange: "", routingKey: dlqName, mandatory: false, basicProperties: props, body: bytes);

                _logger.LogInformation("Published Order {OrderId} to DLQ {Dlq} (reason: {Reason}) CorrelationId: {CorrelationId}", order.Id, dlqName, reason, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish Order {OrderId} to DLQ {Dlq} (CorrelationId: {CorrelationId})", order.Id, dlqName, correlationId);
            }

            await Task.CompletedTask;
        }
    }
}
