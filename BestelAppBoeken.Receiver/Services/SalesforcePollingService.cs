using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BestelAppBoeken.Receiver.Services
{
    public class SalesforcePollingService : BackgroundService
    {
        private readonly ILogger<SalesforcePollingService> _logger;
        private readonly SalesforceClient _salesforceClient;
        private readonly IConfiguration _configuration;
        private DateTime _lastCheckTime;

        public SalesforcePollingService(ILogger<SalesforcePollingService> logger, SalesforceClient salesforceClient, IConfiguration configuration)
        {
            _logger = logger;
            _salesforceClient = salesforceClient;
            _configuration = configuration;
            _lastCheckTime = DateTime.UtcNow.AddMinutes(-5); // Start checking from 5 mins ago
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Salesforce Polling Service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation($"Checking Salesforce for updates since {_lastCheckTime}...");
                    var updates = await _salesforceClient.GetModifiedOrdersAsync(_lastCheckTime);

                    if (updates != null && updates.Count > 0)
                    {
                        _logger.LogInformation($"Found {updates.Count} modified orders.");
                        await PublishUpdatesAsync(updates);
                    }
                    else 
                    {
                        _logger.LogInformation("No modified orders found.");
                    }

                    _lastCheckTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling Salesforce");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task PublishUpdatesAsync(List<dynamic> updates)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"],
                UserName = _configuration["RabbitMq:UserName"],
                Password = _configuration["RabbitMq:Password"]
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "order-updates",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            foreach (var update in updates)
            {
                string status = update.Status;
                string desc = update.Description ?? "";
                string sfId = update.Id;

                // Simple payload
                var payload = new 
                {
                    SalesforceId = sfId,
                    Status = status,
                    Description = desc,
                    UpdatedAt = DateTime.UtcNow
                };

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));

                await channel.BasicPublishAsync(exchange: string.Empty,
                                     routingKey: "order-updates",
                                     body: body);
                
                _logger.LogInformation($"Published update for Order {sfId} [{status}] to RabbitMQ");
            }
        }
    }
}
