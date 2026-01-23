using System;
using System.Collections.Generic;

namespace BestelAppBoeken.Web.Models
{
    public class OrderResponse
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public KlantInfo? Klant { get; set; }
        public List<OrderItemInfo> Items { get; set; } = new();

        public IntegrationStatusInfo? IntegrationStatus { get; set; }
    }

    public class IntegrationStatusInfo
    {
        public string? SalesforceId { get; set; }
        public bool SalesforceSuccess { get; set; }

        public string? SapIDocNumber { get; set; }
        public int? SapStatus { get; set; }
        public string? SapStatusDescription { get; set; }
        public bool SapSuccess { get; set; }

        public DateTime Timestamp { get; set; }
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
