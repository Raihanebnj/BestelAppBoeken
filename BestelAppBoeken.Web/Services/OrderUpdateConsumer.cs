using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using BestelAppBoeken.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BestelAppBoeken.Core.Models;

namespace BestelAppBoeken.Web.Services
{
    public class OrderUpdateConsumer : BackgroundService
    {
        private readonly ILogger<OrderUpdateConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private IConnection? _connection;
        private IChannel? _channel;

        public OrderUpdateConsumer(ILogger<OrderUpdateConsumer> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"],
                UserName = _configuration["RabbitMq:UserName"],
                Password = _configuration["RabbitMq:Password"]
            };

            // Try to establish connection with simple retry/backoff
            int attempt = 0;
            const int maxAttempts = 5;

            while (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    _connection = await factory.CreateConnectionAsync();
                    _channel = await _connection.CreateChannelAsync();

                    await _channel.QueueDeclareAsync(queue: "order-updates",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    Console.WriteLine("OrderUpdateConsumer listening on 'order-updates'...");
                    _logger.LogInformation("OrderUpdateConsumer listening on 'order-updates'...");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt} failed to connect to RabbitMQ", attempt);
                    if (attempt >= maxAttempts)
                    {
                        _logger.LogError(ex, "Failed to connect to RabbitMQ after {Attempts} attempts", attempt);
                        break;
                    }

                    // exponential backoff
                    var delayMs = 500 * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null) return;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received update: {message}");
                _logger.LogInformation($"Received update: {message}");

                try
                {
                    var update = JsonConvert.DeserializeObject<dynamic>(message);
                    string status = update.Status;
                    string description = update.Description;

                    // Parse Order ID from Description: "Web Order #{Id} from ..."
                    var match = Regex.Match(description, @"Web Order #(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int orderId))
                    {
                        await UpdateOrderStatusAsync(orderId, status);
                    }
                    else
                    {
                        _logger.LogWarning($"Could not parse Order ID from description: {description}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order update");
                }
            };

            await _channel.BasicConsumeAsync(queue: "order-updates", autoAck: true, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
                var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

                if (order != null)
                {
                    if (order.Status != newStatus)
                    {
                        _logger.LogInformation($"Updating Order #{orderId} Status: {order.Status} -> {newStatus}");

                        // Log to console as requested by user
                        if (newStatus.Equals("Activated", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"\n[User Alert] bestelling afgerond met de info van de bestelling:");
                            Console.WriteLine($"- Order Nummer: {order.Id}");
                            Console.WriteLine($"- Klant: {order.CustomerEmail}");
                            Console.WriteLine($"- Datum: {order.OrderDate}");
                            Console.WriteLine($"- Totaal Bedrag: {order.TotalAmount}");
                            Console.WriteLine($"- Nieuwe Status: {newStatus}\n");
                        }

                        order.Status = newStatus;
                        await dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    _logger.LogWarning($"Order #{orderId} not found in database.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _channel?.Dispose();
            _connection?.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }
}
