using System;
using System.Collections.Generic;
using System.Text;
using BestelAppBoeken.Core.Models;

namespace BestelAppBoeken.Core.Interfaces
{
    public interface IOrderService
    {
        IEnumerable<Order> GetAllOrders();
        Task<IEnumerable<Order>> GetAllOrdersAsync(); // ? Voor export functionaliteit
        Order? GetOrderById(int id);
        Order CreateOrder(Order order);
        IEnumerable<Order> GetOrdersByCustomerEmail(string email);
    }
}