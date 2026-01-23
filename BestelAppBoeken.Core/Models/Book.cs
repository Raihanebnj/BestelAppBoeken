using System;
using System.ComponentModel.DataAnnotations;

namespace BestelAppBoeken.Core.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Author { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Isbn { get; set; } = string.Empty;

        [Range(0, 10000)]
        public decimal Price { get; set; }

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(1000)]
        public string ImageUrl { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int VoorraadAantal { get; set; } = 0;
    }
}