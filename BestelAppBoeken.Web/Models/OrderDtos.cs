using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BestelAppBoeken.Web.Models
{
    public class CreateOrderRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "KlantId moet een positief geheel getal zijn.")]
        public int KlantId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Er moet minstens één item in de bestelling zijn.")]
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "BoekId moet een positief geheel getal zijn.")]
        public int BoekId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Aantal moet minimaal 1 zijn.")]
        public int Aantal { get; set; }
    }
}
