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
            var authUrl = _configuration["Salesforce:AuthUrl"] ?? "https://login.salesforce.com/services/oauth2/token";
            var clientId = (_configuration["Salesforce:ConsumerKey"] ?? "").Trim();
            var clientSecret = (_configuration["Salesforce:ConsumerSecret"] ?? "").Trim();
            var username = (_configuration["Salesforce:Username"] ?? "").Trim();
            var password = (_configuration["Salesforce:Password"] ?? "").Trim();
            var securityToken = (_configuration["Salesforce:SecurityToken"] ?? "").Trim();

            // Strategy 1: Password Grant with Security Token
            try 
            {
                _logger.LogInformation("Attempting Auth Strategy 1: Password Flow with Security Token...");
                await TryAuthStrategy(authUrl, new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password + securityToken)
                });
                return; // Success
            }
            catch (Exception ex) 
            { 
               _logger.LogWarning($"Strategy 1 Failed: {ex.Message}");
            }

            // Strategy 2: Password Grant WITHOUT Security Token
            try 
            {
                _logger.LogInformation("Attempting Auth Strategy 2: Password Flow WITHOUT Security Token...");
                await TryAuthStrategy(authUrl, new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password)
                });
                return; // Success
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Strategy 2 Failed: {ex.Message}");
            }

            // Strategy 3: Client Credentials Flow (Fallback)
            try 
            {
                _logger.LogInformation("Attempting Auth Strategy 3: Client Credentials Flow...");
                await TryAuthStrategy(authUrl, new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret)
                });
                return; // Success
            }
            catch (Exception ex)
            {
                _logger.LogError($"All Authentication Strategies Failed. Final Error: {ex.Message}");
                throw;
            }
        }

        private async Task TryAuthStrategy(string url, KeyValuePair<string, string>[] formData)
        {
            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Salesforce returned {response.StatusCode}: {responseString}");
            }

            var authResponse = JsonConvert.DeserializeObject<dynamic>(responseString);
            var token = (string)authResponse?.access_token;
            var instance = (string)authResponse?.instance_url;

            if (string.IsNullOrEmpty(token))
            {
                 throw new Exception("Auth successful but Access Token is null.");
            }

            _accessToken = token;
            _instanceUrl = instance;
            _logger.LogInformation($"Strategy Succeeded! Authenticated. Instance: {_instanceUrl}");
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
