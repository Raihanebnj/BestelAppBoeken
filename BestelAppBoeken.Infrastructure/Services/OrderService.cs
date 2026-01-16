using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using BestelAppBoeken.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly BookstoreDbContext _context;

        public OrderService(BookstoreDbContext context)
        {
            _context = context;
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

        public Order CreateOrder(Order order)
        {
            _context.Orders.Add(order);
            _context.SaveChanges();
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