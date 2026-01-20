using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BestelAppBoeken.Infrastructure.Services
{
    /// <summary>
    /// 🎭 SAP iDoc Service - SIMPLIFIED MOCK VERSION
    /// 
    /// ✅ Features:
    /// - Simuleert SAP R/3 integratie (geen echte connectie nodig)
    /// - Genereert realistische IDoc nummers
    /// - Uitgebreide logging voor debugging
    /// - Minimale XML generatie (klaar voor echte SAP)
    /// - Configurable: switch tussen mock en live mode
    /// 
    /// ⚙️ Configuration:
    /// {
    ///   "SAP": {
    ///     "UseMockMode": true,           // true = simulatie, false = echte SAP
    ///     "Endpoint": "http://sap:8000", // SAP server (alleen bij live mode)
    ///     "Client": "800",               // SAP client nummer
    ///     "System": "DEV"                // SAP systeem (DEV/QA/PRD)
    ///   }
    /// }
    /// </summary>
    public class SapService : ISapService
    {
        private readonly ILogger<SapService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        
        // Configuration
        private readonly bool _useMockMode;
        private readonly string _sapEndpoint;
        private readonly string _sapClient;
        private readonly string _sapSystem;

        public SapService(
            ILogger<SapService> logger, 
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            
            // Load configuration
            bool.TryParse(configuration["SAP:UseMockMode"], out _useMockMode);
            if (string.IsNullOrEmpty(configuration["SAP:UseMockMode"]))
            {
                _useMockMode = true; // Default to mock mode
            }
            
            _sapEndpoint = configuration["SAP:Endpoint"] ?? "http://sap-mock:8000/idoc";
            _sapClient = configuration["SAP:Client"] ?? "800";
            _sapSystem = configuration["SAP:System"] ?? "DEV";
            
            
            // Log startup mode
            if (_useMockMode)
            {
                _logger.LogInformation("🎭 [SAP MOCK] Service started - All orders will be SIMULATED");
            }
            else
            {
                _logger.LogInformation("🔌 [SAP LIVE] Service started - Endpoint: {Endpoint}, Client: {Client}", 
                    _sapEndpoint, _sapClient);
            }
        }

        #region Legacy Methods (backwards compatibility)
        
        /// <summary>
        /// Check inventory (always returns true in mock mode)
        /// </summary>
        public Task<bool> CheckInventoryAsync(int bookId, int quantity)
        {
            if (_useMockMode)
            {
                _logger.LogDebug("📦 [SAP Mock] Inventory check - Book: {BookId}, Qty: {Quantity} → ✅ AVAILABLE", 
                    bookId, quantity);
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// Post invoice to SAP
        /// </summary>
        public async Task PostInvoiceAsync(Order order)
        {
            _logger.LogInformation("📄 [SAP] Posting invoice for Order {OrderId}", order.Id);
            var response = await SendOrderIDocAsync(order);
            
            if (response.Success)
            {
                _logger.LogInformation("✅ [SAP] Invoice posted successfully - IDoc: {IDocNumber}", 
                    response.IDocNumber);
            }
            else
            {
                _logger.LogWarning("⚠️ [SAP] Invoice posting issue - {Status}", 
                    response.StatusDescription);
            }
        }
        
        #endregion

        #region Main IDoc Methods

        /// <summary>
        /// 🚀 Send Order to SAP as IDoc
        /// 
        /// MOCK MODE: Simulates SAP response (for development/testing)
        /// LIVE MODE: Sends actual HTTP request to SAP endpoint
        /// </summary>
        public async Task<SapIDocResponse> SendOrderIDocAsync(Order order)
        {
            var idocNumber = GenerateIDocNumber();
            
            try
            {
                _logger.LogInformation("📤 [SAP] START - Order {OrderId} → IDoc {IDocNumber}", 
                    order.Id, idocNumber);
                
                // MOCK MODE: Simulate response
                if (_useMockMode)
                {
                    return SimulateSapResponse(order, idocNumber);
                }
                
                // LIVE MODE: Send to real SAP
                return await SendToRealSapAsync(order, idocNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SAP] ERROR - Order {OrderId} / IDoc {IDocNumber}", 
                    order.Id, idocNumber);
                
                return new SapIDocResponse
                {
                    IDocNumber = idocNumber,
                    Status = SapIDocStatus.Error,
                    StatusDescription = $"Error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Generate SAP iDoc XML (backwards compatibility)
        /// Returns minimal XML structure
        /// </summary>
        public Task<string> GenerateOrdersIdocXmlAsync(Order order)
        {
            var idocNumber = GenerateIDocNumber();
            var xml = GenerateMinimalIdocXml(order, idocNumber);
            return Task.FromResult(xml);
        }

        /// <summary>
        /// Check IDoc status in SAP
        /// </summary>
        public async Task<SapIDocStatus> CheckIDocStatusAsync(string idocNumber)
        {
            if (_useMockMode)
            {
                _logger.LogDebug("🔍 [SAP Mock] Status check - IDoc: {IDocNumber} → ✅ SUCCESS", idocNumber);
                return SapIDocStatus.Success;
            }
            
            try
            {
                var statusEndpoint = $"{_sapEndpoint}/status/{idocNumber}";
                var response = await _httpClient.GetAsync(statusEndpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return ParseSapStatus(content);
                }
                
                _logger.LogWarning("⚠️ [SAP] Status check failed for {IDocNumber}, assuming success", idocNumber);
                return SapIDocStatus.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SAP] Status check error for {IDocNumber}", idocNumber);
                return SapIDocStatus.Error;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 🎭 MOCK: Simulate SAP response
        /// </summary>
        private SapIDocResponse SimulateSapResponse(Order order, string idocNumber)
        {
            // Log order details (as if sending to SAP)
            _logger.LogInformation("📋 [SAP Mock] Order Details:");
            _logger.LogInformation("   Order ID: {OrderId}", order.Id);
            _logger.LogInformation("   Customer: {Customer}", order.CustomerName ?? order.CustomerEmail);
            _logger.LogInformation("   Items: {Count} items", order.Items?.Count ?? 0);
            _logger.LogInformation("   Total: €{Amount:F2}", order.TotalAmount);
            _logger.LogInformation("   IDoc: {IDocNumber}", idocNumber);
            
            // Simulate processing delay
            System.Threading.Thread.Sleep(50);
            
            // Return success
            _logger.LogInformation("✅ [SAP Mock] IDoc {IDocNumber} - Status: SUCCESS (simulated)", idocNumber);
            
            return new SapIDocResponse
            {
                IDocNumber = idocNumber,
                Status = SapIDocStatus.Success,
                StatusDescription = "Mock: Order successfully processed (simulated)",
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 🔌 LIVE: Send to real SAP endpoint
        /// </summary>
        private async Task<SapIDocResponse> SendToRealSapAsync(Order order, string idocNumber)
        {
            try
            {
                _logger.LogInformation("🔌 [SAP Live] Sending to endpoint: {Endpoint}", _sapEndpoint);
                
                // Generate minimal XML
                var xml = GenerateMinimalIdocXml(order, idocNumber);
                
                // POST to SAP
                var content = new StringContent(xml, Encoding.UTF8, "application/xml");
                content.Headers.Add("X-IDoc-Number", idocNumber);
                content.Headers.Add("X-SAP-Client", _sapClient);
                content.Headers.Add("X-SAP-System", _sapSystem);
                
                var response = await _httpClient.PostAsync(_sapEndpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var status = ParseSapStatus(responseContent);
                    
                    _logger.LogInformation("✅ [SAP Live] IDoc {IDocNumber} - Status: {Status}", 
                        idocNumber, status);
                    
                    return new SapIDocResponse
                    {
                        IDocNumber = idocNumber,
                        Status = status,
                        StatusDescription = GetStatusDescription(status),
                        Timestamp = DateTime.UtcNow
                    };
                }
                
                _logger.LogError("❌ [SAP Live] HTTP Error: {StatusCode}", response.StatusCode);
                
                return new SapIDocResponse
                {
                    IDocNumber = idocNumber,
                    Status = SapIDocStatus.Error,
                    StatusDescription = $"HTTP Error: {response.StatusCode}",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ [SAP Live] Connection failed - falling back to mock");
                
                // Fallback to mock
                return SimulateSapResponse(order, idocNumber);
            }
        }

        /// <summary>
        /// Generate minimal SAP iDoc XML
        /// Contains only essential fields for SAP processing
        /// </summary>
        private string GenerateMinimalIdocXml(Order order, string idocNumber)
        {
            var orderNumber = $"ORD-{order.Id:D6}";
            
            var xml = new XElement("ORDERS05",
                new XElement("IDOC",
                    // Control Record
                    new XElement("EDI_DC40",
                        new XElement("DOCNUM", idocNumber),
                        new XElement("IDOCTYP", "ORDERS05"),
                        new XElement("MESTYP", "ORDERS"),
                        new XElement("MANDT", _sapClient),
                        new XElement("CREDAT", DateTime.Now.ToString("yyyyMMdd")),
                        new XElement("CRETIM", DateTime.Now.ToString("HHmmss"))
                    ),
                    // Document Header
                    new XElement("E1EDK01",
                        new XElement("BELNR", orderNumber),
                        new XElement("CURCY", "EUR"),
                        
                        // Customer
                        new XElement("E1EDKA1",
                            new XElement("PARVW", "AG"),
                            new XElement("PARTN", order.Id.ToString("D10")),
                            new XElement("NAME1", order.CustomerName ?? order.CustomerEmail ?? "Unknown")
                        ),
                        
                        // Line Items
                        order.Items?.Select((item, index) =>
                            new XElement("E1EDP01",
                                new XElement("POSEX", (index + 1).ToString("D6")),
                                new XElement("MENGE", item.Quantity),
                                new XElement("MENEE", "ST"),
                                
                                // Material
                                new XElement("E1EDP19",
                                    new XElement("QUALF", "001"),
                                    new XElement("IDTNR", item.BookId.ToString("D10")),
                                    new XElement("KTEXT", item.BookTitle ?? "Unknown")
                                ),
                                
                                // Price
                                new XElement("E1EDP26",
                                    new XElement("QUALF", "003"),
                                    new XElement("BETRG", item.UnitPrice.ToString("F2")),
                                    new XElement("WAERS", "EUR")
                                )
                            )
                        ).ToArray() ?? Array.Empty<XElement>()
                    ),
                    // Summary
                    new XElement("E1EDS01",
                        new XElement("SUMME", order.TotalAmount.ToString("F2")),
                        new XElement("SUNIT", "EUR")
                    )
                )
            );
            
            return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n{xml}";
        }

        /// <summary>
        /// Generate unique SAP IDoc number (16 digits)
        /// Format: YYYYMMDDHHMMSSRR
        /// </summary>
        private string GenerateIDocNumber()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(10, 99);
            return $"{timestamp}{random}";
        }

        /// <summary>
        /// Parse SAP response and extract status
        /// </summary>
        private SapIDocStatus ParseSapStatus(string responseXml)
        {
            try
            {
                var doc = XDocument.Parse(responseXml);
                var statusElement = doc.Descendants("STATUS").FirstOrDefault();
                
                if (statusElement != null && int.TryParse(statusElement.Value, out int statusCode))
                {
                    return statusCode switch
                    {
                        64 => SapIDocStatus.Created,
                        53 => SapIDocStatus.Success,
                        51 => SapIDocStatus.Error,
                        _ => SapIDocStatus.Pending
                    };
                }
                
                return SapIDocStatus.Success;
            }
            catch
            {
                return SapIDocStatus.Pending;
            }
        }

        /// <summary>
        /// Get human-readable status description
        /// </summary>
        private string GetStatusDescription(SapIDocStatus status)
        {
            return status switch
            {
                SapIDocStatus.Created => "64 - IDoc ready for processing",
                SapIDocStatus.Success => "53 - IDoc successfully processed",
                SapIDocStatus.Error => "51 - IDoc processing failed",
                SapIDocStatus.Pending => "00 - IDoc in queue",
                _ => "Unknown status"
            };
        }

        #endregion
    }
}
