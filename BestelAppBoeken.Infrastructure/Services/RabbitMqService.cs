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
            
            // Lees RabbitMQ config
            var rabbitMqSection = _configuration.GetSection("RabbitMq");

            // Encrypt/decrypt test
            var encryptedPassword = SimpleCrypto.Encrypt("Groep3");
            var decryptedPassword = SimpleCrypto.Decrypt(encryptedPassword);

            _factory = new ConnectionFactory
            {
                HostName = rabbitMqSection["HostName"] ?? "10.2.160.223",
                UserName = rabbitMqSection["UserName"] ?? "bestelapp",
                Password = decryptedPassword // Gebruik gedecrypt wachtwoord
            };

            _logger.LogInformation("RabbitMQ service met encryptie geïnitialiseerd");
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

                // FIX: Gebruik een property die WEL bestaat in Order
                _logger.LogInformation($"Successfully published order to queue '{queueName}': Order ID {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish order to RabbitMQ.");
            }
        }
    }
}