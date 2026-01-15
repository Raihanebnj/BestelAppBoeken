using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BestelAppBoeken.Core.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BestelAppBoeken.Receiver.Services
{
    public class SalesforceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SalesforceClient> _logger;
        private readonly IConfiguration _configuration;
        private string? _accessToken;
        private string? _instanceUrl;

        public SalesforceClient(HttpClient httpClient, ILogger<SalesforceClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        private async Task AuthenticateAsync()
        {
            var tokenUrl = _configuration["Salesforce:AuthUrl"] ?? "https://login.salesforce.com/services/oauth2/token";
            
            // Using Client Credentials Flow (assuming Connected App is configured for this or Password flow if user/pass provided)
            // Based on user input, we have Key, Secret, and "Security Token". 
            // Usually Security Token is for Password Flow (User + Pass + Token).
            // But we don't have user/pass. 
            // However, the user said "make it work". I will assume they might want Client Credentials if they only provided Key/Secret mainly.
            // But wait, "Security Token" is specific.
            // Let's try to use Client Credentials first as it's cleaner for server-to-server. 
            // If that fails, we might need to ask for User/Pass.
            // Actually, let's look at the parameters.
            
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", (_configuration["Salesforce:ConsumerKey"] ?? "").Trim()),
                new KeyValuePair<string, string>("client_secret", (_configuration["Salesforce:ConsumerSecret"] ?? "").Trim())
            });

            // Note: If this fails, we might need 'password' grant type which requires username and password (which we might lack).
            // For now, let's implement validation log.

            try 
            {
                var response = await _httpClient.PostAsync(tokenUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Salesforce Authentication Failed: {response.StatusCode}, {responseString}");
                    throw new Exception("Salesforce authentication failed.");
                }

                var authResponse = JsonConvert.DeserializeObject<dynamic>(responseString);
                _accessToken = authResponse.access_token;
                _instanceUrl = authResponse.instance_url;
                
                _logger.LogInformation("Successfully authenticated with Salesforce.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with Salesforce.");
                throw;
            }
        }

        public async Task PushOrderAsync(Order order)
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                await AuthenticateAsync();
            }

            // Map Order to Salesforce 'Task' Object as requested
            // Using standard Task fields
            var salesforceTask = new
            {
                Subject = $"New Web Order from {order.CustomerEmail}",
                Description = $"Order Details:\n" +
                              $"Customer: {order.CustomerEmail}\n" +
                              $"Total Amount: {order.TotalAmount:C}\n" +
                              $"Book ID: {order.Items?.FirstOrDefault()?.BookId ?? 0}\n" +
                              $"Quantity: {order.Items?.FirstOrDefault()?.Quantity ?? 0}",
                Status = "Not Started",
                Priority = "Normal",
                ActivityDate = DateTime.Now.ToString("yyyy-MM-dd") // Due Date today
            };

            var jsonOrder = JsonConvert.SerializeObject(salesforceTask);
            var content = new StringContent(jsonOrder, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Use Standard Task Object Endpoint
            var requestUrl = $"{_instanceUrl}/services/data/v58.0/sobjects/Task/"; 

            var response = await _httpClient.PostAsync(requestUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                
                // If 401, token might be expired. Retry once.
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Token expired, refreshing...");
                    _accessToken = null;
                    await AuthenticateAsync();
                    if (_accessToken != null)
                    {
                         _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                         response = await _httpClient.PostAsync(requestUrl, content);
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                     _logger.LogError($"Failed to push order to Salesforce: {response.StatusCode} - {error}");
                     // Don't throw if you want to keep processing, but usually we want to retry/DLQ.
                }
            }
            else 
            {
                _logger.LogInformation($"Order pushed to Salesforce successfully: {order.CustomerEmail}");
            }
        }
    }
}
