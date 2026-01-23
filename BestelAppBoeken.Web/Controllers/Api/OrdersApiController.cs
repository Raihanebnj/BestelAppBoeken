using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using BestelAppBoeken.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BestelAppBoeken.Web.Controllers.Api
{
    [Route("api/orders")]
    [ApiController]
    [Produces("application/json")]
    public class OrdersApiController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IMessageQueueService _messageQueue;
        private readonly ISalesforceService _salesforceService;
        private readonly ISapService _sapService; // ✅ SAP iDoc Service (ACTIEF)
        private readonly IKlantService _klantService;
        private readonly IBookService _bookService;
        private readonly IEmailService _emailService;
        private readonly ILogger<OrdersApiController> _logger;

        public OrdersApiController(
            IOrderService orderService,
            IMessageQueueService messageQueue,
            ISalesforceService salesforceService,
            ISapService sapService, // ✅ SAP iDoc Service (ACTIEF)
            IKlantService klantService,
            IBookService bookService,
            IEmailService emailService,
            ILogger<OrdersApiController> logger)
        {
            _orderService = orderService;
            _messageQueue = messageQueue;
            _salesforceService = salesforceService;
            _sapService = sapService; // ✅ SAP iDoc Service (ACTIEF)
            _klantService = klantService;
            _bookService = bookService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Haalt alle bestellingen op
        /// </summary>
        /// <returns>Lijst van alle bestellingen met klant en order item details</returns>
        /// <response code="200">Bestellingen succesvol opgehaald</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult GetAllOrders([FromQuery] int? page, [FromQuery] int? pageSize)
        {
            try
            {
                var allOrders = _orderService.GetAllOrders().ToList();
                var allKlanten = _klantService.GetAllKlanten().ToList();

                // Map to response objects
                var mapped = allOrders.Select(o =>
                {
                    var klant = allKlanten.FirstOrDefault(k => k.Email == o.CustomerEmail);
                    return new OrderResponse
                    {
                        Id = o.Id,
                        OrderDate = o.OrderDate,
                        CustomerEmail = o.CustomerEmail,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        Klant = klant != null ? new KlantInfo
                        {
                            Id = klant.Id,
                            Naam = klant.Naam,
                            Email = klant.Email
                        } : null,
                        Items = o.Items.Select(i => new OrderItemInfo
                        {
                            BoekId = i.BookId,
                            Titel = i.BookTitle,
                            Aantal = i.Quantity,
                            Prijs = i.UnitPrice
                        }).ToList()
                    };
                }).ToList();

                // If pagination requested
                if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
                {
                    var p = page.Value;
                    var ps = pageSize.Value;
                    var total = mapped.Count;
                    var items = mapped.Skip((p - 1) * ps).Take(ps).ToList();
                    var hasMore = (p * ps) < total;

                    return Ok(new
                    {
                        items,
                        totalCount = total,
                        page = p,
                        pageSize = ps,
                        hasMore
                    });
                }

                // Fallback: return all (legacy)
                return Ok(new { items = mapped, totalCount = mapped.Count, page = 1, pageSize = mapped.Count, hasMore = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij ophalen orders");
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het ophalen van bestellingen" });
            }
        }

        /// <summary>
        /// Maakt een nieuwe bestelling aan
        /// </summary>
        /// <param name="request">Bestelling gegevens met klant ID en items</param>
        /// <returns>De aangemaakte bestelling</returns>
        /// <remarks>
        /// Voorbeeld request:
        /// 
        ///     POST /api/orders
        ///     {
        ///        "klantId": 1,
        ///        "items": [
        ///          {
        ///            "boekId": 1,
        ///            "aantal": 2
        ///          }
        ///        ]
        ///     }
        /// 
        /// De bestelling wordt automatisch:
        /// - Opgeslagen in de database
        /// - Verstuurd naar RabbitMQ
        /// - Gesynchroniseerd met Salesforce
        /// - Gepost naar SAP R/3
        /// - Voorraad wordt automatisch bijgewerkt
        /// </remarks>
        /// <response code="201">Bestelling succesvol aangemaakt</response>
        /// <response code="400">Ongeldige input of onvoldoende voorraad</response>
        /// <response code="404">Klant of boek niet gevonden</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Valideer klant
                var klant = _klantService.GetKlantById(request.KlantId);
                if (klant == null)
                {
                    return NotFound(new { error = "Klant niet gevonden" });
                }

                // Valideer en bereken totaal
                decimal totaalBedrag = 0;
                var orderItems = new List<OrderItem>();

                foreach (var item in request.Items)
                {
                    var boek = _bookService.GetBookById(item.BoekId);
                    if (boek == null)
                    {
                        return NotFound(new { error = $"Boek met ID {item.BoekId} niet gevonden" });
                    }

                    // Check voorraad
                    if (boek.VoorraadAantal < item.Aantal)
                    {
                        return BadRequest(new { error = $"Onvoldoende voorraad voor {boek.Title}. Beschikbaar: {boek.VoorraadAantal}" });
                    }

                    var orderItem = new OrderItem
                    {
                        BookId = item.BoekId,
                        BookTitle = boek.Title,
                        Quantity = item.Aantal,
                        UnitPrice = boek.Price
                    };

                    orderItems.Add(orderItem);
                    totaalBedrag += boek.Price * item.Aantal;

                    // Update voorraad
                    boek.VoorraadAantal -= item.Aantal;
                    _bookService.UpdateBook(boek.Id, boek);
                }

                // Maak order aan
                var order = new Order
                {
                    OrderDate = DateTime.Now,
                    CustomerEmail = klant.Email,
                    TotalAmount = totaalBedrag,
                    Status = "Pending",
                    Items = orderItems
                };

                // Opslaan in database
                var savedOrder = _orderService.CreateOrder(order);

                _logger.LogInformation("✅ Order {OrderId} opgeslagen in database", savedOrder.Id);

                // ============================================
                // STAP 2: PARALLELLE VERWERKING (RabbitMQ + SAP)
                // ============================================
                string? salesforceId = null;
                SapIDocResponse? sapResponse = null;

                // ✅ Start BEIDE processen PARALLEL voor snelheid
                var salesforceTask = Task.Run(async () =>
                {
                    try
                    {
                        // 3a) RabbitMQ → Salesforce (eenzijdige communicatie)
                        await _salesforceService.SyncOrderAsync(savedOrder);
                        _logger.LogInformation("📨 [RabbitMQ] Order {OrderId} gepubliceerd naar queue 'salesforce_orders'", savedOrder.Id);
                        
                        // Simuleer Salesforce ID (in productie komt dit van Salesforce)
                        salesforceId = $"SF-{DateTime.Now:yyyyMMddHHmmss}-{savedOrder.Id}";
                        return salesforceId;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ [RabbitMQ] Fout bij publiceren naar Salesforce queue");
                        return null;
                    }
                });

                var sapTask = Task.Run(async () =>
                {
                    try
                    {
                        // 3b) SAP iDoc → SAP R/3 (tweezijdige communicatie: Request-Response)
                        _logger.LogInformation("📤 [SAP iDoc] START - Transformeren Order {OrderId} naar ORDERS05 XML", savedOrder.Id);
                        
                        var response = await _sapService.SendOrderIDocAsync(savedOrder);
                        
                        _logger.LogInformation(
                            "✅ [SAP iDoc] SUCCESS - IDoc {IDocNumber} | Status: {Status} | {Description}",
                            response.IDocNumber,
                            (int)response.Status,
                            response.StatusDescription
                        );
                        
                        return response;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ [SAP iDoc] FOUT bij verzenden naar SAP R/3");
                        
                        return new SapIDocResponse
                        {
                            IDocNumber = "ERROR",
                            Status = SapIDocStatus.Error,
                            StatusDescription = "SAP integratie gefaald: " + ex.Message,
                            Timestamp = DateTime.UtcNow
                        };
                    }
                });

                // ✅ WACHT OP BEIDE TAKEN (parallelle verwerking)
                await Task.WhenAll(salesforceTask, sapTask);
                
                salesforceId = salesforceTask.Result;
                sapResponse = sapTask.Result;

                // 3. Send confirmation email (Async, niet blokkerend)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendOrderConfirmationEmailAsync(
                            klant.Email,
                            klant.Naam,
                            savedOrder.Id,
                            savedOrder.TotalAmount
                        );
                        _logger.LogInformation("📧 Bevestigingsmail verzonden naar {Email} voor order {OrderId}", klant.Email, savedOrder.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "❌ Fout bij verzenden bevestigingsmail voor order {OrderId}", savedOrder.Id);
                    }
                });

                // ============================================
                // STAP 6: GECOMBINEERDE RESPONSE (Salesforce + SAP)
                // ============================================
                var response = new OrderResponse
                {
                    Id = savedOrder.Id,
                    OrderDate = savedOrder.OrderDate,
                    CustomerEmail = savedOrder.CustomerEmail,
                    TotalAmount = savedOrder.TotalAmount,
                    Status = savedOrder.Status,
                    Klant = new KlantInfo
                    {
                        Id = klant.Id,
                        Naam = klant.Naam,
                        Email = klant.Email
                    },
                    Items = savedOrder.Items.Select(i => new OrderItemInfo
                    {
                        BoekId = i.BookId,
                        Titel = i.BookTitle,
                        Aantal = i.Quantity,
                        Prijs = i.UnitPrice
                    }).ToList(),
                    
                    // ✅ STAP 6: Gecombineerde integratie status (Salesforce + SAP)
                    IntegrationStatus = new IntegrationStatusInfo
                    {
                        SalesforceId = salesforceId,
                        SalesforceSuccess = salesforceId != null,
                        SapIDocNumber = sapResponse?.IDocNumber,
                        SapStatus = (int?)sapResponse?.Status,
                        SapStatusDescription = sapResponse?.StatusDescription,
                        SapSuccess = sapResponse?.Status == SapIDocStatus.Success,
                        Timestamp = DateTime.UtcNow
                    }
                };

                _logger.LogInformation(
                    "🎉 [ORDER COMPLETE] Order {OrderId} | Salesforce: {SfId} | SAP IDoc: {IDocNum} (Status {SapStatus})",
                    savedOrder.Id,
                    salesforceId ?? "N/A",
                    sapResponse?.IDocNumber ?? "N/A",
                    (int?)sapResponse?.Status ?? 0
                );

                return CreatedAtAction(nameof(GetAllOrders), new { id = savedOrder.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij aanmaken order");
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het aanmaken van de bestelling" });
            }
        }

        /// <summary>
        /// [TEST] Preview SAP iDoc XML voor een bestaande order
        /// </summary>
        [HttpGet("{id}/sap-idoc-preview")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetSapIDocPreview(int id)
        {
            try
            {
                var order = _orderService.GetOrderById(id);
                if (order == null)
                {
                    return NotFound(new { error = "Order niet gevonden" });
                }

                var idocXml = await _sapService.GenerateOrdersIdocXmlAsync(order);
                
                return Ok(new
                {
                    orderId = order.Id,
                    message = "SAP iDoc ORDERS05 XML preview",
                    format = "ORDERS05",
                    segment = "E1EDK01 (BELNR = ordernummer)",
                    xml = idocXml
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij genereren SAP iDoc preview");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // Request DTOs moved to BestelAppBoeken.Web.Models.OrderDtos
}