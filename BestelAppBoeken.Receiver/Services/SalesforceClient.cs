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

            try 
            {
                // 1. Find or Create Account
                var accountId = await GetAccountIdAsync(order.CustomerEmail);
                if (string.IsNullOrEmpty(accountId))
                {
                    _logger.LogInformation($"Account not found for {order.CustomerEmail}. Creating new account...");
                    accountId = await CreateAccountAsync(order.CustomerEmail, order.CustomerEmail);
                }

                // 2. Get Standard Pricebook ID
                var pricebookId = await GetStandardPricebookIdAsync();
                
                // 3. Get or Create Contract (Required for Activation)
                var contractId = await EnsureContractAsync(accountId);

                // 4. Create Order Object
                var salesforceOrder = new
                {
                    AccountId = accountId,
                    ContractId = contractId,
                    EffectiveDate = order.OrderDate.ToString("yyyy-MM-dd"), // Use actual order date
                    Status = "Draft",
                    Pricebook2Id = pricebookId, // Required for adding Line Items
                    Description = $"Web Order #{order.Id} from {order.CustomerEmail}"
                };

                var orderId = await CreateObjectAsync("Order", salesforceOrder);
                
                if (string.IsNullOrEmpty(orderId))
                {
                     throw new Exception("Failed to create Order Header (returned null ID)");
                }

                _logger.LogInformation($"Created Order Header {orderId}. Syncing {order.Items.Count} items...");

                // 4. Sync Items (Products & OrderItems)
                foreach(var item in order.Items)
                {
                    await SyncOrderItemAsync(orderId, pricebookId, item);
                }

                _logger.LogInformation($"Successfully fully synced Order {orderId} for {order.CustomerEmail}. Ready for manual activation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing order to Salesforce");
                throw;
            }
        }

        private async Task SyncOrderItemAsync(string orderId, string pricebookId, OrderItem item)
        {
             // A. Ensure Product Exists
             var productId = await EnsureProductAsync(item.BookTitle, $"BOOK-{item.BookId}");

             // B. Ensure Pricebook Entry Exists
             var pbeId = await EnsurePricebookEntryAsync(productId, pricebookId, item.UnitPrice);

             // C. Create OrderItem
             var orderItem = new
             {
                 OrderId = orderId,
                 PricebookEntryId = pbeId,
                 Quantity = item.Quantity,
                 UnitPrice = item.UnitPrice
             };

             await CreateObjectAsync("OrderItem", orderItem);
        }

        private async Task<string> EnsureProductAsync(string name, string code)
        {
            // Check if exists by ProductCode
            var query = $"SELECT Id FROM Product2 WHERE ProductCode = '{code}' LIMIT 1";
            var result = await QueryAsync(query);
            
            if (result?.records != null && result.records.Count > 0)
            {
                return result.records[0].Id;
            }

            // Create
            var product = new
            {
                Name = name,
                ProductCode = code,
                IsActive = true,
                Description = "Synced from BestelApp"
            };

            var id = await CreateObjectAsync("Product2", product);
            return id;
        }

        private async Task<string> EnsurePricebookEntryAsync(string productId, string pricebookId, decimal price)
        {
            // Check if exists
            var query = $"SELECT Id FROM PricebookEntry WHERE Product2Id = '{productId}' AND Pricebook2Id = '{pricebookId}' LIMIT 1";
            var result = await QueryAsync(query);

            if (result?.records != null && result.records.Count > 0)
            {
                return result.records[0].Id;
            }

            // Create
            var pbe = new
            {
                Pricebook2Id = pricebookId,
                Product2Id = productId,
                UnitPrice = price,
                IsActive = true,
                UseStandardPrice = false
            };

            var id = await CreateObjectAsync("PricebookEntry", pbe);
            return id;
        }

        private async Task<string> GetStandardPricebookIdAsync()
        {
            // Standard Pricebook is usually harddat to query reliably without `isStandard` which varies by API version, 
            // but getting by ID if known is best. 
            // Fallback strategy: Query for standard pricebook
            var query = "SELECT Id FROM Pricebook2 WHERE IsStandard = true LIMIT 1";
            var result = await QueryAsync(query);
             if (result?.records != null && result.records.Count > 0)
            {
                return result.records[0].Id;
            }
            throw new Exception("Could not find Standard Pricebook!");
        }

        // Helper for queries to reduce boilerplate
        private async Task<dynamic?> QueryAsync(string soql)
        {
             return await ExecuteWithRetryAsync(async () => 
             {
                 var requestUrl = $"{_instanceUrl}/services/data/v58.0/query/?q={Uri.EscapeDataString(soql)}";
                 _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                 return await _httpClient.GetAsync(requestUrl);
             });
        }

        // Helper for creation
        private async Task<string> CreateObjectAsync(string objectType, object data)
        {
             var result = await ExecuteWithRetryAsync(async () => 
             {
                 var json = JsonConvert.SerializeObject(data);
                 var content = new StringContent(json, Encoding.UTF8, "application/json");
                 var requestUrl = $"{_instanceUrl}/services/data/v58.0/sobjects/{objectType}/";
                 _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                 return await _httpClient.PostAsync(requestUrl, content);
             });
             
            return result?.id;
        }

        private async Task<dynamic?> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> action)
        {
            var response = await action();

            // Retry on 401 Unauthorized
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Salesforce Token Expired (401). refreshing...");
                await AuthenticateAsync();
                response = await action();
            }

            if (!response.IsSuccessStatusCode) 
            {
                 var error = await response.Content.ReadAsStringAsync();
                 _logger.LogError($"Salesforce Request Failed: {response.StatusCode} - {error} - URL: {response.RequestMessage?.RequestUri}");
                 // For QueryAsync which expects null on fail:
                 return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<dynamic>(json);
        }

        private async Task<string?> GetAccountIdAsync(string email)
        {
            var query = $"SELECT Id FROM Account WHERE Name = '{email}' LIMIT 1";
            var result = await QueryAsync(query);
            if (result?.records != null && result.records.Count > 0) return result.records[0].Id;
            return null;
        }

        private async Task<string> CreateAccountAsync(string name, string email)
        {
            var account = new
            {
                Name = name,
                Type = "Customer - Direct",
                Phone = "1234567890" 
            };
            return await CreateObjectAsync("Account", account);
        }

        private async Task<string> EnsureContractAsync(string accountId)
        {
            // 1. Try to find an ALREADY ACTIVATED contract
            var query = $"SELECT Id FROM Contract WHERE AccountId = '{accountId}' AND Status = 'Activated' LIMIT 1";
            var result = await QueryAsync(query);
            
            if (result?.records != null && result.records.Count > 0)
            {
                return result.records[0].Id;
            }

            // 2. If no active contract, Create a NEW one (Draft -> Activate)
            // We avoid reusing old Drafts to prevent "missing field" issues
            var contract = new
            {
                AccountId = accountId,
                Status = "Draft",
                StartDate = DateTime.Now.ToString("yyyy-MM-dd"),
                ContractTerm = 12,
                Description = "Auto-created by BestelApp"
            };

            var contractId = await CreateObjectAsync("Contract", contract);

            // 3. Activate it immediately
             await UpdateContractStatusAsync(contractId, "Activated");
             
             return contractId;
        }

        private async Task UpdateContractStatusAsync(string contractId, string status)
        {
            var update = new { Status = status };
             await ExecuteWithRetryAsync(async () => 
             {
                 var json = JsonConvert.SerializeObject(update);
                 var content = new StringContent(json, Encoding.UTF8, "application/json");
                 var requestUrl = $"{_instanceUrl}/services/data/v58.0/sobjects/Contract/{contractId}";
                 _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                 return await _httpClient.PatchAsync(requestUrl, content);
             });
        }
        public async Task<List<dynamic>> GetModifiedOrdersAsync(DateTime since)
        {
            if (string.IsNullOrEmpty(_accessToken)) await AuthenticateAsync();

            var dateStr = since.ToString("yyyy-MM-ddTHH:mm:ssZ");
            // Query for orders modified after the date
            // We select Description to parse out the original email if needed, or we just trust matching by Reference/Name if we had that.
            // Returning Description to help validat in Consumer.
            var query = $"SELECT Id, Status, Description, OrderNumber FROM Order WHERE LastModifiedDate > {dateStr} ORDER BY LastModifiedDate DESC";
            
            var result = await QueryAsync(query);
            var list = new List<dynamic>();

            if (result?.records != null)
            {
                foreach (var record in result.records)
                {
                    list.Add(record);
                }
            }
            return list;
        }
        private async Task UpdateStatusAsync(string orderId, string status)
        {
            var update = new { Status = status };
             await ExecuteWithRetryAsync(async () => 
             {
                 var json = JsonConvert.SerializeObject(update);
                 var content = new StringContent(json, Encoding.UTF8, "application/json");
                 // Use PATCH for updates
                 var requestUrl = $"{_instanceUrl}/services/data/v58.0/sobjects/Order/{orderId}";
                 _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                 return await _httpClient.PatchAsync(requestUrl, content);
             });
        }
    }
}
