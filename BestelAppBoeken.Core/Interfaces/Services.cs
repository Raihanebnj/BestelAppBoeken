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
        
        // SAP iDoc ORDERS05 specifieke methodes
        Task<SapIDocResponse> SendOrderIDocAsync(Order order);
        Task<string> GenerateOrdersIdocXmlAsync(Order order);
        Task<SapIDocStatus> CheckIDocStatusAsync(string idocNumber);
    }

    // SAP iDoc Response model
    public class SapIDocResponse
    {
        public string IDocNumber { get; set; } = string.Empty;
        public SapIDocStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool Success => Status == SapIDocStatus.Success;
    }

    // SAP iDoc Status codes
    public enum SapIDocStatus
    {
        Created = 64,        // Klaar voor verwerking
        Success = 53,        // Verwerkt, document aangemaakt
        Error = 51,          // Niet succesvol
        Pending = 0          // Nog niet verstuurd
    }
}
