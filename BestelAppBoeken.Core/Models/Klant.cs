using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace BestelAppBoeken.Core.Models
{
    public class Klant
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Naam { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string Telefoon { get; set; } = string.Empty;

        [StringLength(500)]
        public string Adres { get; set; } = string.Empty;
    }
}