using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class RabbitMqService : IMessageQueueService
    {
        private readonly IConfiguration _configuration;
        private readonly ConnectionFactory _factory;

        public RabbitMqService(IConfiguration configuration)
        {
            _configuration = configuration;
            var rabbitMqSection = _configuration.GetSection("RabbitMq");

            _factory = new ConnectionFactory
            {
                HostName = rabbitMqSection["HostName"],
                UserName = rabbitMqSection["UserName"],
                Password = rabbitMqSection["Password"]
            };
        }

        public async Task PublishOrderAsync(Order order)
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
        }
    }
}
