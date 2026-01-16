using System;
using System.Collections.Generic;

namespace BestelAppBoeken.Core.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public decimal TotalAmount { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
