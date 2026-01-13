using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class RabbitMqService : IMessageQueueService
    {
        private readonly ConnectionFactory _factory;

        public RabbitMqService()
        {
            _factory = new ConnectionFactory
            {
                HostName = "10.2.160.223",
                UserName = "bestelapp",
                Password = "Groep3"
            };
        }

        public async Task PublishOrderAsync(Order order)
        {
            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "orders",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            string message = JsonSerializer.Serialize(order);
            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(exchange: "",
                                 routingKey: "orders",
                                 mandatory: false,
                                 basicProperties: new BasicProperties(),
                                 body: body);
        }
    }
}
