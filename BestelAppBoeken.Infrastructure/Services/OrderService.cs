using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using BestelAppBoeken.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly BookstoreDbContext _context;
        private readonly IMessageQueueService _messageQueueService;

        public OrderService(BookstoreDbContext context, IMessageQueueService messageQueueService)
        {
            _context = context;
            _messageQueueService = messageQueueService;
        }

        public IEnumerable<Order> GetAllOrders()
        {
            return _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
        }

        // ? Async versie voor export functionaliteit
        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public Order? GetOrderById(int id)
        {
            return _context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == id);
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        // Keep existing synchronous API but call into async implementation to publish messages cleanly.
        public Order CreateOrder(Order order)
        {
            return CreateOrderAsync(order).GetAwaiter().GetResult();
        }

        // New async creation that persists the order and publishes an approval/request message to RabbitMQ
        public async Task<Order> CreateOrderAsync(Order order)
        {
            // Ensure initial status
            if (string.IsNullOrWhiteSpace(order.Status))
            {
                order.Status = "Pending";
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Publish approval/request message to RabbitMQ so Receiver -> Salesforce flow is triggered.
            try
            {
                await _messageQueueService.PublishOrderApprovalRequestAsync(order);
            }
            catch
            {
                // Logging happens inside RabbitMqService. Do not fail order creation because of MQ issues.
            }

            return order;
        }

        public IEnumerable<Order> GetOrdersByCustomerEmail(string email)
        {
            return _context.Orders
                .Include(o => o.Items)
                .Where(o => o.CustomerEmail == email)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
        }
    }
}