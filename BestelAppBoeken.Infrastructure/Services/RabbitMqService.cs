// BestelAppBoeken.Infrastructure/Services/RabbitMqService.cs
using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class RabbitMqService : IMessageQueueService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqService> _logger;
        private readonly ConnectionFactory _factory;

        public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var rabbitMqSection = _configuration.GetSection("RabbitMq");

            _factory = new ConnectionFactory
            {
                HostName = rabbitMqSection["HostName"] ?? "10.2.160.223",
                UserName = rabbitMqSection["UserName"] ?? "bestelapp",
                Password = rabbitMqSection["Password"] ?? "Groep3"
            };
        }

        public async Task PublishOrderAsync(Order order)
        {
            try
            {
                using var connection = await _factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                var queueName = _configuration["RabbitMq:QueueName"] ?? "orders";

                await channel.QueueDeclareAsync(queue: queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                string message = JsonSerializer.Serialize(order);
                var body = Encoding.UTF8.GetBytes(message);

                await channel.BasicPublishAsync(exchange: string.Empty,
                                     routingKey: queueName,
                                     body: body);

                _logger.LogInformation($"Successfully published order to queue '{queueName}': Order ID {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish order to RabbitMQ.");
            }
        }

        public async Task PublishOrderApprovalRequestAsync(Order order)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(order.Status))
                {
                    order.Status = "Pending";
                }

                await PublishOrderAsync(order);
                _logger.LogInformation($"Published approval/request message for Order #{order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish approval request to RabbitMQ.");
            }
        }

        public async Task PublishOrderStatusUpdateAsync(int orderId, string status, string? description = null)
        {
            try
            {
                using var connection = await _factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                var queueName = _configuration["RabbitMq:UpdatesQueueName"] ?? "order-updates";

                await channel.QueueDeclareAsync(queue: queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var payload = new
                {
                    OrderId = orderId,
                    Status = status,
                    Description = description ?? $"Web Order #{orderId}",
                    UpdatedAt = DateTime.UtcNow
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

                await channel.BasicPublishAsync(exchange: string.Empty,
                                     routingKey: queueName,
                                     body: body);

                _logger.LogInformation($"Published status update for Order #{orderId} -> {status} to '{queueName}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish order status update to RabbitMQ.");
            }
        }
    }
}
