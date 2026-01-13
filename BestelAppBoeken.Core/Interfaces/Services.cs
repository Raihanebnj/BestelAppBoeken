using BestelAppBoeken.Core.Models;
using System.Threading.Tasks;

namespace BestelAppBoeken.Core.Interfaces
{
    public interface IMessageQueueService
    {
        Task PublishOrderAsync(Order order);
    }

    public interface ISalesforceService
    {
        Task SyncOrderAsync(Order order);
    }

    public interface ISapService
    {
        Task<bool> CheckInventoryAsync(int bookId, int quantity);
        Task PostInvoiceAsync(Order order);
    }
}
