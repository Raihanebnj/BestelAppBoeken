namespace BestelAppBoeken.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendOrderConfirmationEmailAsync(string customerEmail, string customerName, int orderId, decimal totalAmount);
    }
}
