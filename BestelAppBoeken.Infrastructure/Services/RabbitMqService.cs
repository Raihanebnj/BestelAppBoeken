using BestelAppBoeken.Core.Interfaces;
using BestelAppBoeken.Core.Models;
using RabbitMQ.Client;
using System.Text;
using System.Collections.Generic;
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
            // Simple retry loop with exponential backoff to improve resilience
            const int maxAttempts = 3;
            int attempt = 0;

            while (true)
            {
                attempt++;
                try
                {
                    using var connection = await _factory.CreateConnectionAsync();
                    using var channel = await connection.CreateChannelAsync();

                    var queueName = _configuration["RabbitMq:QueueName"] ?? "orders";

                    // Dead-letter queue name for this queue
                    var dlqName = queueName + "-dlq";

                    // Declare DLQ first (simple durable queue)
                    await channel.QueueDeclareAsync(queue: dlqName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    // Declare main queue with dead-letter routing so messages that are rejected/nacked
                    // are routed to the DLQ. We keep existing semantics (non-durable previously)
                    // but mark durable for better persistence. This does NOT change message body.
                    var queueArgs = new Dictionary<string, object>
                    {
                        { "x-dead-letter-exchange", "" },
                        { "x-dead-letter-routing-key", dlqName }
                    };

                    await channel.QueueDeclareAsync(queue: queueName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: queueArgs);

                    string message = JsonSerializer.Serialize(order);
                    var body = Encoding.UTF8.GetBytes(message);

                    var props = new BasicProperties();
                    props.Persistent = true; // mark message persistent so it survives broker restarts

                    await channel.BasicPublishAsync(exchange: "",
                                         routingKey: queueName,
                                         mandatory: false,
                                         basicProperties: props,
                                         body: body);

                    _logger.LogInformation($"Successfully published order to queue '{queueName}': {order.CustomerEmail}");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt} failed to publish order to RabbitMQ.", attempt);

                    if (attempt >= maxAttempts)
                    {
                        _logger.LogError(ex, "Failed to publish order to RabbitMQ after {Attempts} attempts.", attempt);
                        break; // give up after retries - do not change message content
                    }

                    // simple exponential backoff
                    var delayMs = 250 * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delayMs);
                }
            }
        }
    }
}
