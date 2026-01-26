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
        [StringLength(100)]
        public string Naam { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string Telefoon { get; set; } = string.Empty;

        [StringLength(300)]
        public string Adres { get; set; } = string.Empty;
    }
}