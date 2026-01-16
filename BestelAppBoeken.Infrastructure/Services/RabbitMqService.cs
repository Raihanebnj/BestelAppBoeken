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
                HostName = "10.2.160.223",
                UserName = "bestelapp",
                Password = "Groep3"
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

                await channel.BasicPublishAsync(exchange: "",
                                     routingKey: queueName,
                                     mandatory: false,
                                     basicProperties: new BasicProperties(),
                                     body: body);

                _logger.LogInformation($"Successfully published order to queue '{queueName}': {order.CustomerEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish order to RabbitMQ.");
                // Retrying logic or fallback could go here
            }
        }
    }
}
