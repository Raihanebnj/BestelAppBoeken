using BestelAppBoeken.Core.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BestelAppBoeken.Core.Interfaces
{
    public interface IKlantService
    {
        IEnumerable<Klant> GetAllKlanten();
        Klant? GetKlantById(int id);
        Klant CreateKlant(Klant klant);
        Klant? UpdateKlant(int id, Klant klant);
        bool DeleteKlant(int id);
        IEnumerable<Klant> SearchKlanten(string query);
    }
}