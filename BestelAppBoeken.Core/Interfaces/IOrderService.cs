using BestelAppBoeken.Core.Models;

namespace BestelAppBoeken.Core.Interfaces
{
    public interface IOrderService
    {
        IEnumerable<Order> GetAllOrders();
        Order? GetOrderById(int id);
        Order CreateOrder(Order order);
        IEnumerable<Order> GetOrdersByCustomerEmail(string email);
    }
}
