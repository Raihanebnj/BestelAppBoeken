using BestelAppBoeken.Infrastructure.Services;
using BestelAppBoeken.Core.Interfaces; // ? Voor IOrderService
using Microsoft.AspNetCore.Mvc;

namespace BestelAppBoeken.Web.Controllers.Api
{
    [Route("api/backup")]
    [ApiController]
    [Produces("application/json")]
    public class BackupApiController : ControllerBase
    {
        private readonly IDatabaseBackupService _backupService;
        private readonly IOrderService _orderService;
        private readonly IKlantService _klantService; // ? Voor klant lookup
        private readonly ILogger<BackupApiController> _logger;

        public BackupApiController(
            IDatabaseBackupService backupService,
            IOrderService orderService,
            IKlantService klantService, // ? Voor klant lookup
            ILogger<BackupApiController> logger)
        {
            _backupService = backupService;
            _orderService = orderService;
            _klantService = klantService;
            _logger = logger;
        }

        /// <summary>
        /// Maak een nieuwe database backup
        /// </summary>
        /// <returns>Backup informatie</returns>
        /// <response code="200">Backup succesvol aangemaakt</response>
        /// <response code="500">Server error</response>
        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                var backupPath = await _backupService.CreateBackupAsync();
                return Ok(new 
                { 
                    success = true, 
                    message = "Backup succesvol aangemaakt",
                    fileName = Path.GetFileName(backupPath),
                    filePath = backupPath,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij aanmaken backup");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Haal lijst van beschikbare backups op
        /// </summary>
        /// <returns>Lijst van backup bestanden</returns>
        /// <response code="200">Backups succesvol opgehaald</response>
        /// <response code="500">Server error</response>
        [HttpGet("list")]
        [ProducesResponseType(typeof(BackupListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetBackups()
        {
            try
            {
                var backups = _backupService.GetAvailableBackups();
                
                var backupDetails = backups.Select(fileName => {
                    // Parse timestamp from filename: bookstore_backup_yyyyMMdd_HHmmss.db
                    var match = System.Text.RegularExpressions.Regex.Match(fileName, @"bookstore_backup_(\d{8})_(\d{6})\.db");
                    DateTime? timestamp = null;
                    
                    if (match.Success)
                    {
                        var dateStr = match.Groups[1].Value; // yyyyMMdd
                        var timeStr = match.Groups[2].Value; // HHmmss
                        
                        var year = int.Parse(dateStr.Substring(0, 4));
                        var month = int.Parse(dateStr.Substring(4, 2));
                        var day = int.Parse(dateStr.Substring(6, 2));
                        var hour = int.Parse(timeStr.Substring(0, 2));
                        var minute = int.Parse(timeStr.Substring(2, 2));
                        var second = int.Parse(timeStr.Substring(4, 2));
                        
                        timestamp = new DateTime(year, month, day, hour, minute, second);
                    }
                    
                    return new BackupInfo
                    {
                        FileName = fileName,
                        Timestamp = timestamp,
                        FormattedDate = timestamp?.ToString("dd-MM-yyyy HH:mm:ss") ?? "Onbekend"
                    };
                }).ToList();
                
                return Ok(new BackupListResponse 
                { 
                    Success = true, 
                    Backups = backupDetails,
                    Count = backupDetails.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij ophalen backups");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Herstel database van backup
        /// </summary>
        /// <param name="request">Restore request met bestandsnaam</param>
        /// <returns>Restore status</returns>
        /// <response code="200">Database succesvol hersteld</response>
        /// <response code="400">Ongeldige backup file</response>
        /// <response code="500">Server error</response>
        [HttpPost("restore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RestoreBackup([FromBody] RestoreRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FileName))
                {
                    return BadRequest(new { success = false, error = "Bestandsnaam is verplicht" });
                }

                var backupPath = Path.Combine(Directory.GetCurrentDirectory(), "Backups", request.FileName);
                
                if (!System.IO.File.Exists(backupPath))
                {
                    return BadRequest(new { success = false, error = "Backup bestand niet gevonden" });
                }

                var success = await _backupService.RestoreBackupAsync(backupPath);
                
                if (success)
                {
                    return Ok(new 
                    { 
                        success = true, 
                        message = "Database succesvol hersteld",
                        fileName = request.FileName,
                        timestamp = DateTime.Now
                    });
                }
                else
                {
                    return StatusCode(500, new { success = false, error = "Restore mislukt" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij herstellen backup");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Download een backup bestand
        /// </summary>
        /// <param name="fileName">Naam van het backup bestand</param>
        /// <returns>Backup bestand</returns>
        /// <response code="200">Backup bestand succesvol gedownload</response>
        /// <response code="404">Backup niet gevonden</response>
        [HttpGet("download/{fileName}")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DownloadBackup(string fileName)
        {
            try
            {
                var backupPath = Path.Combine(Directory.GetCurrentDirectory(), "Backups", fileName);
                
                if (!System.IO.File.Exists(backupPath))
                {
                    return NotFound(new { success = false, error = "Backup bestand niet gevonden" });
                }

                var fileBytes = System.IO.File.ReadAllBytes(backupPath);
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij downloaden backup");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Exporteer alle bestellingen naar JSON formaat
        /// </summary>
        /// <returns>JSON bestand met alle bestellingen</returns>
        /// <response code="200">Export succesvol</response>
        /// <response code="500">Server error</response>
        [HttpGet("export/orders/json")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportOrdersJson()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                var klanten = _klantService.GetAllKlanten().ToList(); // ? Haal alle klanten op
                
                var exportData = new
                {
                    ExportDate = DateTime.Now,
                    TotalOrders = orders.Count(),
                    TotalRevenue = orders.Sum(o => o.TotalAmount),
                    Orders = orders.Select(order => 
                    {
                        // ? Lookup klant op basis van email
                        var klant = klanten.FirstOrDefault(k => k.Email == order.CustomerEmail);
                        
                        return new
                        {
                            OrderId = order.Id,
                            OrderNumber = $"ORD-{order.Id}",
                            OrderDate = order.OrderDate,
                            CustomerEmail = order.CustomerEmail,
                            CustomerName = klant?.Naam ?? "Onbekend", // ? Gebruik klant naam
                            TotalAmount = order.TotalAmount,
                            Status = order.Status,
                            Items = order.Items.Select(item => new
                            {
                                BookId = item.BookId,
                                BookTitle = item.BookTitle,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice,
                                Subtotal = item.Quantity * item.UnitPrice
                            }).ToList()
                        };
                    }).ToList()
                };

                var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                var fileName = $"orders_export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);

                _logger.LogInformation($"?? Orders geëxporteerd naar JSON: {fileName}");

                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij exporteren orders naar JSON");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Exporteer alle bestellingen naar TXT formaat (leesbaar)
        /// </summary>
        /// <returns>TXT bestand met alle bestellingen</returns>
        /// <response code="200">Export succesvol</response>
        /// <response code="500">Server error</response>
        [HttpGet("export/orders/txt")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportOrdersTxt()
        {
            try {
                var orders = await _orderService.GetAllOrdersAsync();
                var klanten = _klantService.GetAllKlanten().ToList(); // ? Haal alle klanten op
                
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("???????????????????????????????????????????????????????????????");
                sb.AppendLine("                 BOOKSTORE - BESTELLINGEN EXPORT");
                sb.AppendLine("???????????????????????????????????????????????????????????????");
                sb.AppendLine($"Export Datum:        {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                sb.AppendLine($"Totaal Bestellingen: {orders.Count()}");
                sb.AppendLine($"Totale Omzet:        EUR {orders.Sum(o => o.TotalAmount):N2}");
                sb.AppendLine("???????????????????????????????????????????????????????????????");
                sb.AppendLine();

                foreach (var order in orders.OrderByDescending(o => o.OrderDate))
                {
                    // ? Lookup klant op basis van email
                    var klant = klanten.FirstOrDefault(k => k.Email == order.CustomerEmail);
                    
                    sb.AppendLine("???????????????????????????????????????????????????????????????");
                    sb.AppendLine($"?? ORDER #{order.Id} - ORD-{order.Id}");
                    sb.AppendLine("???????????????????????????????????????????????????????????????");
                    sb.AppendLine($"Datum:           {order.OrderDate:dd-MM-yyyy HH:mm:ss}");
                    sb.AppendLine($"Klant:           {klant?.Naam ?? "Onbekend"}"); // ? Gebruik klant naam
                    sb.AppendLine($"Email:           {order.CustomerEmail}");
                    sb.AppendLine($"Status:          {order.Status}");
                    sb.AppendLine($"Totaal Bedrag:   EUR {order.TotalAmount:N2}");
                    sb.AppendLine();
                    sb.AppendLine("Bestelde Items:");
                    sb.AppendLine();

                    foreach (var item in order.Items)
                    {
                        var subtotal = item.Quantity * item.UnitPrice;
                        sb.AppendLine($"  • {item.BookTitle}");
                        sb.AppendLine($"    Aantal:    {item.Quantity}x");
                        sb.AppendLine($"    Prijs:     EUR {item.UnitPrice:N2}");
                        sb.AppendLine($"    Subtotaal: EUR {subtotal:N2}");
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }

                sb.AppendLine("???????????????????????????????????????????????????????????????");
                sb.AppendLine("                      EINDE RAPPORT");
                sb.AppendLine("???????????????????????????????????????????????????????????????");

                var fileName = $"orders_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

                _logger.LogInformation($"?? Orders geëxporteerd naar TXT: {fileName}");

                return File(bytes, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij exporteren orders naar TXT");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    // Response models
    public class RestoreRequest
    {
        public string FileName { get; set; } = string.Empty;
    }

    public class BackupListResponse
    {
        public bool Success { get; set; }
        public List<BackupInfo> Backups { get; set; } = new();
        public int Count { get; set; }
    }

    public class BackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public DateTime? Timestamp { get; set; }
        public string FormattedDate { get; set; } = string.Empty;
    }
}
