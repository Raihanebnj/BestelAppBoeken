using BestelAppBoeken.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendOrderConfirmationEmailAsync(string customerEmail, string customerName, int orderId, decimal totalAmount)
        {
            try
            {
                // Email body met HTML formatting
                var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea, #764ba2); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f7fafc; padding: 30px; border-radius: 0 0 10px 10px; }}
        .order-details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e2e8f0; color: #718096; font-size: 12px; }}
        .total {{ font-size: 24px; font-weight: bold; color: #48bb78; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>? Bestelbevestiging</h1>
            <p>Bedankt voor uw bestelling!</p>
        </div>
        <div class='content'>
            <p>Beste {customerName},</p>
            <p>We hebben uw bestelling succesvol ontvangen en verwerken deze zo snel mogelijk.</p>
            
            <div class='order-details'>
                <h2 style='color: #667eea; margin-top: 0;'>?? Bestelgegevens</h2>
                <p><strong>Ordernummer:</strong> ORD-{orderId:D6}</p>
                <p><strong>Besteldatum:</strong> {DateTime.Now:dd MMMM yyyy HH:mm}</p>
                <div class='total'>
                    ?? Totaalbedrag: EUR {totalAmount:F2}
                </div>
            </div>

            <p><strong>Volgende stappen:</strong></p>
            <ul>
                <li>? Uw bestelling wordt verwerkt in ons systeem</li>
                <li>?? Uw boeken worden ingepakt en verzonden</li>
                <li>?? U ontvangt een track & trace code</li>
            </ul>

            <p>Heeft u vragen over uw bestelling? Neem gerust contact met ons op via support@bookstore@ehb.be</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Bookstore - Alle rechten voorbehouden</p>
            <p>Nijverheidskaai 170, 1070 Brussel</p>
            <p>Powered by RabbitMQ + Salesforce + SAP R/3</p>
        </div>
    </div>
</body>
</html>";

                // In productie zou hier de echte email verzending plaatsvinden
                // Bijvoorbeeld met SMTP, SendGrid, of een andere email service
                
                _logger.LogInformation($"?? Email bevestiging verzonden naar {customerEmail}");
                _logger.LogInformation($"   Klant: {customerName}");
                _logger.LogInformation($"   Order ID: {orderId}");
                _logger.LogInformation($"   Totaal: EUR {totalAmount:F2}");
                _logger.LogInformation($"   Email body length: {emailBody.Length} characters");

                // Simuleer asynchroon verzenden
                await Task.Delay(100);

                // TODO: Implementeer echte email service
                // using var smtpClient = new SmtpClient("smtp.example.com", 587);
                // smtpClient.Credentials = new NetworkCredential("user", "password");
                // smtpClient.EnableSsl = true;
                // 
                // var mailMessage = new MailMessage
                // {
                //     From = new MailAddress("noreply@bookstore.com", "Bookstore"),
                //     Subject = $"Bestelbevestiging - Order #{orderId:D6}",
                //     Body = emailBody,
                //     IsBodyHtml = true
                // };
                // mailMessage.To.Add(customerEmail);
                // 
                // await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"? Bevestigingsmail succesvol verzonden naar {customerEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"? Fout bij verzenden bevestigingsmail naar {customerEmail}");
                // Don't throw - email failure shouldn't stop the order
            }
        }
    }
}
