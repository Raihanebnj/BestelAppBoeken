using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
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
        private readonly ISapService _sapService;
        private readonly IKlantService _klantService;
        private readonly IBookService _bookService;
        private readonly ILogger<OrdersApiController> _logger;

        public OrdersApiController(
            IOrderService orderService,
            IMessageQueueService messageQueue,
            ISalesforceService salesforceService,
            ISapService sapService,
            IKlantService klantService,
            IBookService bookService,
            ILogger<OrdersApiController> logger)
        {
            _orderService = orderService;
            _messageQueue = messageQueue;
            _salesforceService = salesforceService;
            _sapService = sapService;
            _klantService = klantService;
            _bookService = bookService;
            _logger = logger;
        }

        /// <summary>
        /// Haalt alle bestellingen op
        /// </summary>
        /// <returns>Lijst van alle bestellingen met klant en order item details</returns>
        /// <response code="200">Bestellingen succesvol opgehaald</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<OrderResponse>> GetAllOrders()
        {
            try
            {
                var orders = _orderService.GetAllOrders();
                var allKlanten = _klantService.GetAllKlanten().ToList();
                
                var response = orders.Select(o =>
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

                return Ok(response);
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
            try {
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

                // 1. Publish to RabbitMQ
                try
                {
                    await _messageQueue.PublishOrderAsync(savedOrder);
                    _logger.LogInformation("Order {OrderId} gepubliceerd naar RabbitMQ", savedOrder.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fout bij publiceren order {OrderId} naar RabbitMQ", savedOrder.Id);
                }

                // 2. Sync to Salesforce (Async)
                try
                {
                    await _salesforceService.SyncOrderAsync(savedOrder);
                    _logger.LogInformation("Order {OrderId} gesynchroniseerd met Salesforce", savedOrder.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fout bij synchroniseren order {OrderId} met Salesforce", savedOrder.Id);
                }

                // 3. Post to SAP (Async)
                try
                {
                    await _sapService.PostInvoiceAsync(savedOrder);
                    _logger.LogInformation("Order {OrderId} verstuurd naar SAP", savedOrder.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fout bij versturen order {OrderId} naar SAP", savedOrder.Id);
                }

                // Return response met klant info
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
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetAllOrders), new { id = savedOrder.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij aanmaken order");
                return StatusCode(500, new { error = "Er is een fout opgetreden bij het aanmaken van de bestelling" });
            }
        }
    }

    // Request/Response models
    public class CreateOrderRequest
    {
        public int KlantId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int BoekId { get; set; }
        public int Aantal { get; set; }
    }

    public class OrderResponse
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public KlantInfo? Klant { get; set; }
        public List<OrderItemInfo> Items { get; set; } = new();
    }

    public class KlantInfo
    {
        public int Id { get; set; }
        public string Naam { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class OrderItemInfo
    {
        public int BoekId { get; set; }
        public string Titel { get; set; } = string.Empty;
        public int Aantal { get; set; }
        public decimal Prijs { get; set; }
    }
}
