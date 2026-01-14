using BestelAppBoeken.Core.Models;
using System.Collections.Generic;

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
