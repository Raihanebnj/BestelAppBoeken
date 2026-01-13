using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using System.Threading.Tasks;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class SalesforceService : ISalesforceService
    {
        public Task SyncOrderAsync(Order order)
        {
            // Placeholder for Salesforce integration
            // In a real scenario, this would use a REST API or Salesforce SDK
            return Task.CompletedTask;
        }
    }
}
