using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using BestelAppBoeken.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class KlantService : IKlantService
    {
        private readonly BookstoreDbContext _context;

        public KlantService(BookstoreDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Klant> GetAllKlanten()
        {
            return _context.Klanten.ToList();
        }

        public Klant? GetKlantById(int id)
        {
            return _context.Klanten.FirstOrDefault(k => k.Id == id);
        }

        public Klant CreateKlant(Klant klant)
        {
            // Business rule: email must be unique
            if (_context.Klanten.Any(k => k.Email == klant.Email))
            {
                throw new ValidationException("Een klant met dit e-mailadres bestaat al.");
            }

            try
            {
                _context.Klanten.Add(klant);
                _context.SaveChanges();
                return klant;
            }
            catch (DbUpdateException ex)
            {
                // Rethrow as validation for controller to map to 409/500
                throw new Exception("Fout bij opslaan van klant in database", ex);
            }
        }

        public Klant? UpdateKlant(int id, Klant klant)
        {
            var existingKlant = _context.Klanten.FirstOrDefault(k => k.Id == id);
            if (existingKlant == null) return null;

            existingKlant.Naam = klant.Naam;
            existingKlant.Email = klant.Email;
            existingKlant.Telefoon = klant.Telefoon;
            existingKlant.Adres = klant.Adres;

            _context.SaveChanges();
            return existingKlant;
        }

        public bool DeleteKlant(int id)
        {
            var klant = _context.Klanten.FirstOrDefault(k => k.Id == id);
            if (klant == null) return false;

            _context.Klanten.Remove(klant);
            _context.SaveChanges();
            return true;
        }

        public IEnumerable<Klant> SearchKlanten(string query)
        {
            query = query.ToLower();
            return _context.Klanten
                .Where(k => k.Naam.ToLower().Contains(query) ||
                           k.Email.ToLower().Contains(query) ||
                           k.Telefoon.Contains(query) ||
                           k.Adres.ToLower().Contains(query))
                .ToList();
        }
    }
}