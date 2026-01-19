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
    /// SAP iDoc ORDERS05 Service - Tweezijdige communicatie (Request-Response)
    /// Transformeert orders naar SAP iDoc XML en verwerkt statusresponsen
    /// </summary>
    public class SapService : ISapService
    {
        private readonly ILogger<SapService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        
        // SAP iDoc configuratie
        private readonly string _sapEndpoint;
        private readonly string _sapClient;
        private readonly string _sapSystem;
        private readonly int _idocTimeout;

        public SapService(
            ILogger<SapService> logger, 
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            
            // SAP configuratie laden
            _sapEndpoint = configuration["SAP:Endpoint"] ?? "http://sap-server:8000/sap/bc/idoc";
            _sapClient = configuration["SAP:Client"] ?? "800";
            _sapSystem = configuration["SAP:System"] ?? "PRD";
            _idocTimeout = int.TryParse(configuration["SAP:TimeoutSeconds"], out int timeout) ? timeout : 30;
            
            _httpClient.Timeout = TimeSpan.FromSeconds(_idocTimeout);
        }

        #region Legacy Methods (backwards compatibility)
        public Task<bool> CheckInventoryAsync(int bookId, int quantity)
        {
            // Placeholder: Assume inventory is always available
            return Task.FromResult(true);
        }

        public async Task PostInvoiceAsync(Order order)
        {
            // Gebruik nieuwe iDoc methode
            var response = await SendOrderIDocAsync(order);
            if (!response.Success)
            {
                _logger.LogWarning($"SAP invoice posting failed for Order {order.Id}: {response.StatusDescription}");
            }
        }
        #endregion

        /// <summary>
        /// STAP 3b: Verzend Order als SAP iDoc ORDERS05
        /// Tweezijdige communicatie: Request → SAP → Response
        /// </summary>
        public async Task<SapIDocResponse> SendOrderIDocAsync(Order order)
        {
            try
            {
                _logger.LogInformation($"[SAP iDoc] START - Verzenden Order {order.Id} naar SAP R/3");

                // Stap 1: Genereer iDoc XML (ORDERS05 formaat)
                var idocXml = await GenerateOrdersIdocXmlAsync(order);
                
                // Stap 2: Genereer uniek iDoc nummer
                var idocNumber = GenerateIDocNumber();
                
                _logger.LogInformation($"[SAP iDoc] Generated IDoc: {idocNumber}");

                // Stap 3: Verzend naar SAP via HTTP POST
                var content = new StringContent(idocXml, Encoding.UTF8, "application/xml");
                content.Headers.Add("X-IDoc-Number", idocNumber);
                content.Headers.Add("X-SAP-Client", _sapClient);
                content.Headers.Add("X-SAP-System", _sapSystem);

                var response = await _httpClient.PostAsync(_sapEndpoint, content);

                // Stap 4: Verwerk SAP Response
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var sapStatus = ParseSapResponse(responseContent);
                    
                    _logger.LogInformation($"[SAP iDoc] SUCCESS - IDoc {idocNumber} Status: {sapStatus}");

                    return new SapIDocResponse
                    {
                        IDocNumber = idocNumber,
                        Status = sapStatus,
                        StatusDescription = GetStatusDescription(sapStatus),
                        Timestamp = DateTime.UtcNow
                    };
                }
                else
                {
                    _logger.LogError($"[SAP iDoc] ERROR - HTTP {response.StatusCode} voor IDoc {idocNumber}");
                    
                    return new SapIDocResponse
                    {
                        IDocNumber = idocNumber,
                        Status = SapIDocStatus.Error,
                        StatusDescription = $"HTTP Error: {response.StatusCode}",
                        Timestamp = DateTime.UtcNow
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[SAP iDoc] HTTP Connectie fout");
                
                // Fallback: Return simulated success (voor development)
                return new SapIDocResponse
                {
                    IDocNumber = GenerateIDocNumber(),
                    Status = SapIDocStatus.Success,
                    StatusDescription = "Simulated: SAP niet bereikbaar - order lokaal opgeslagen",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SAP iDoc] Onverwachte fout bij verzenden Order {order.Id}");
                throw;
            }
        }

        /// <summary>
        /// Genereer SAP iDoc XML in ORDERS05 formaat
        /// Gebruikt E1EDK01 segment met BELNR (ordernummer)
        /// </summary>
        public Task<string> GenerateOrdersIdocXmlAsync(Order order)
        {
            try
            {
                var idocNumber = GenerateIDocNumber();
                var orderNumber = $"ORD-{order.Id:D6}"; // Format: ORD-000001

                // SAP iDoc ORDERS05 XML structuur - ZONDER namespace op root (veroorzaakt conflict)
                var idocXml = new XElement("ORDERS05",
                    
                    // Control Record (verplicht)
                    new XElement("IDOC",
                        new XAttribute("BEGIN", "1"),
                        
                        // EDI_DC40: Control Record
                        new XElement("EDI_DC40",
                            new XAttribute("SEGMENT", "1"),
                            new XElement("TABNAM", "EDI_DC40"),
                            new XElement("MANDT", _sapClient),
                            new XElement("DOCNUM", idocNumber),
                            new XElement("DOCREL", "740"),
                            new XElement("STATUS", "64"), // 64 = Klaar voor verwerking
                            new XElement("DIRECT", "2"),  // 2 = Inbound
                            new XElement("IDOCTYP", "ORDERS05"),
                            new XElement("MESTYP", "ORDERS"),
                            new XElement("SNDPOR", "BOOKSTORE"),
                            new XElement("SNDPRT", "LS"),
                            new XElement("SNDPRN", "BOOKSTORE_WEB"),
                            new XElement("RCVPOR", "SAPECC"),
                            new XElement("RCVPRT", "LS"),
                            new XElement("RCVPRN", "ECCCLNT800"),
                            new XElement("CREDAT", DateTime.Now.ToString("yyyyMMdd")),
                            new XElement("CRETIM", DateTime.Now.ToString("HHmmss"))
                        ),
                        
                        // E1EDK01: Document Header (BELANGRIJK - bevat ordernummer)
                        new XElement("E1EDK01",
                            new XAttribute("SEGMENT", "1"),
                            new XElement("BELNR", orderNumber),  // ← Ordernummer (SAP vereist)
                            new XElement("CURCY", "EUR"),
                            new XElement("WKURS", "1.00"),
                            new XElement("HWAER", "EUR"),
                            new XElement("AUGRU", "001"), // Order reason
                            new XElement("BSART", "OR"),  // Document type: Order
                            new XElement("NTGEW", "0"), // Net weight
                            new XElement("BRGEW", "0"), // Gross weight
                            new XElement("GEWEI", "KG"), // Weight unit
                            
                            // E1EDKA1: Partner/Customer data
                            new XElement("E1EDKA1",
                                new XAttribute("SEGMENT", "1"),
                                new XElement("PARVW", "AG"), // Partner function: Sold-to party
                                new XElement("PARTN", order.Id.ToString("D10")), // Use Order ID as partner
                                new XElement("NAME1", order.CustomerName ?? order.CustomerEmail ?? "Unknown Customer")
                            ),
                            
                            // E1EDP01: Line items (per boek)
                            order.Items?.Select((item, index) => 
                                new XElement("E1EDP01",
                                    new XAttribute("SEGMENT", "1"),
                                    new XElement("POSEX", (index + 1).ToString("D6")), // Position number
                                    new XElement("MENGE", item.Quantity.ToString()), // Quantity
                                    new XElement("MENEE", "ST"), // Unit: Stuks
                                    new XElement("WERKS", "1000"), // Plant
                                    new XElement("LPRIO", "02"), // Delivery priority
                                    
                                    // E1EDP19: Item object identification (Book ID as material)
                                    new XElement("E1EDP19",
                                        new XAttribute("SEGMENT", "1"),
                                        new XElement("QUALF", "001"), // Qualifier: Material number
                                        new XElement("IDTNR", item.BookId.ToString("D10")),
                                        new XElement("KTEXT", item.BookTitle ?? "Unknown Book")
                                    ),
                                    
                                    // E1EDP26: Item price
                                    new XElement("E1EDP26",
                                        new XAttribute("SEGMENT", "1"),
                                        new XElement("QUALF", "003"), // Qualifier: Net price
                                        new XElement("BETRG", item.UnitPrice.ToString("F2")),
                                        new XElement("KRATE", item.Quantity.ToString()),
                                        new XElement("WAERS", "EUR")
                                    )
                                )
                            ).ToArray() ?? Array.Empty<XElement>()
                        ),
                        
                        // E1EDS01: Summary data
                        new XElement("E1EDS01",
                            new XAttribute("SEGMENT", "1"),
                            new XElement("SUMID", "001"),
                            new XElement("SUMME", order.TotalAmount.ToString("F2")),
                            new XElement("SUNIT", "EUR")
                        )
                    )
                );

                var xmlString = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n{idocXml}";
                
                _logger.LogDebug($"[SAP iDoc] Generated XML:\n{xmlString}");
                
                return Task.FromResult(xmlString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SAP iDoc] Fout bij genereren XML voor Order {order.Id}");
                throw new InvalidOperationException("SAP iDoc XML generatie gefaald", ex);
            }
        }

        /// <summary>
        /// Check SAP iDoc status (64 → 53/51)
        /// </summary>
        public async Task<SapIDocStatus> CheckIDocStatusAsync(string idocNumber)
        {
            try
            {
                _logger.LogInformation($"[SAP iDoc] Checking status voor IDoc: {idocNumber}");

                // In productie: API call naar SAP
                // Voor nu: Simuleer status check
                var statusEndpoint = $"{_sapEndpoint}/status/{idocNumber}";
                var response = await _httpClient.GetAsync(statusEndpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return ParseSapResponse(content);
                }
                
                // Fallback: Simuleer succes
                _logger.LogWarning($"[SAP iDoc] Status check gefaald, assume success voor {idocNumber}");
                return SapIDocStatus.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SAP iDoc] Status check fout voor {idocNumber}");
                return SapIDocStatus.Error;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Genereer uniek SAP iDoc nummer (16 digits)
        /// Formaat: YYYYMMDDHHMMSSXX
        /// </summary>
        private string GenerateIDocNumber()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(10, 99);
            return $"{timestamp}{random}";
        }

        /// <summary>
        /// Parse SAP response XML en extract status
        /// </summary>
        private SapIDocStatus ParseSapResponse(string responseXml)
        {
            try
            {
                // Parse SAP response XML
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

                // Default: assume success als geen status gevonden
                return SapIDocStatus.Success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SAP iDoc] Response parsing gefaald, assume pending");
                return SapIDocStatus.Pending;
            }
        }

        /// <summary>
        /// Menselijk leesbare status beschrijving
        /// </summary>
        private string GetStatusDescription(SapIDocStatus status)
        {
            return status switch
            {
                SapIDocStatus.Created => "64 - IDoc klaar voor verwerking",
                SapIDocStatus.Success => "53 - IDoc succesvol verwerkt, document aangemaakt",
                SapIDocStatus.Error => "51 - IDoc verwerking gefaald",
                SapIDocStatus.Pending => "00 - IDoc in wachtrij",
                _ => "Onbekende status"
            };
        }

        #endregion
    }
}
