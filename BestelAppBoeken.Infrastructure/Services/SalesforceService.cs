using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using System.Threading.Tasks;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class SalesforceService : ISalesforceService
    {
        private readonly IMessageQueueService _messageQueueService;

        public SalesforceService(IMessageQueueService messageQueueService)
        {
            _messageQueueService = messageQueueService;
        }

        public async Task SyncOrderAsync(Order order)
        {
            await _messageQueueService.PublishOrderAsync(order);
        }
    }
}
