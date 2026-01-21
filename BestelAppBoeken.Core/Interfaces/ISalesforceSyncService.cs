// BestelAppBoeken.Core/Interfaces/ISalesforceSyncService.cs
using System.Threading.Tasks;
using System.Collections.Generic;
using BestelAppBoeken.Core.Models;

namespace BestelAppBoeken.Core.Interfaces
{
    public interface ISalesforceSyncService
    {
        Task<bool> SyncOrderToSalesforceAsync(Order order);
        Task<List<Order>> SyncOrdersFromSalesforceAsync();
    }
}