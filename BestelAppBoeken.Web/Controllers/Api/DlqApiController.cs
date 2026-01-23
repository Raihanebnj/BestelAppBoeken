using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Collections.Generic;

namespace BestelAppBoeken.Web.Controllers.Api
{
    [Route("api/dlq")]
    [ApiController]
    public class DlqApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DlqApiController> _logger;

        public DlqApiController(IConfiguration configuration, ILogger<DlqApiController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private bool IsRequestAuthorized()
        {
            // Require X-Api-Key header to match configured admin key for DLQ operations
            var configured = _configuration["ApiKey:ValidKey"];
            if (string.IsNullOrWhiteSpace(configured)) return false;

            if (!Request.Headers.TryGetValue("X-Api-Key", out var key)) return false;
            return key.Equals(configured);
        }

        [HttpGet("list")]
        public async Task<IActionResult> List([FromQuery] string queue = "orders", [FromQuery] int count = 50)
        {
            var dlqName = queue + "-dlq";

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            var messages = new List<object>();

            try
            {
                if (!IsRequestAuthorized()) return Unauthorized(new { error = "API-key vereist voor DLQ acties" });
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                for (int i = 0; i < count; i++)
                {
                    var result = await channel.BasicGetAsync(dlqName, autoAck: false);
                    if (result == null) break;

                    var body = Encoding.UTF8.GetString(result.Body.ToArray());

                    messages.Add(new
                    {
                        Body = body,
                        Properties = result.BasicProperties?.Headers,
                        Timestamp = result.BasicProperties?.Timestamp.UnixTime > 0 ? System.DateTimeOffset.FromUnixTimeSeconds((long)result.BasicProperties.Timestamp.UnixTime).ToString() : null
                    });

                    // Requeue the message so DLQ remains unchanged
                    await channel.BasicNackAsync(result.DeliveryTag, multiple: false, requeue: true);
                }

                return Ok(new { Queue = dlqName, Count = messages.Count, Messages = messages });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error listing DLQ messages");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("requeue-all")]
        public async Task<IActionResult> RequeueAll([FromQuery] string queue = "orders")
        {
            var dlqName = queue + "-dlq";

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            int requeued = 0;

            try
            {
                if (!IsRequestAuthorized()) return Unauthorized(new { error = "API-key vereist voor DLQ acties" });
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                // Ensure target queue exists
                await channel.QueueDeclareAsync(queue: queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

                while (true)
                {
                    var result = await channel.BasicGetAsync(dlqName, autoAck: false);
                    if (result == null) break;

                    var body = result.Body.ToArray();

                    var props = new BasicProperties();
                    props.Persistent = true;
                    if (result.BasicProperties?.Headers != null)
                    {
                        props.Headers = result.BasicProperties.Headers;
                    }

                    await channel.BasicPublishAsync(exchange: "", routingKey: queue, mandatory: false, basicProperties: props, body: body);

                    // Acknowledge message on DLQ to remove it
                    await channel.BasicAckAsync(result.DeliveryTag, multiple: false);

                    requeued++;
                }

                return Ok(new { success = true, requeued });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error requeueing DLQ messages");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("requeue")]
        public async Task<IActionResult> RequeueOne([FromQuery] string queue = "orders", [FromQuery] int index = 0)
        {
            var dlqName = queue + "-dlq";

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            if (!IsRequestAuthorized()) return Unauthorized(new { error = "API-key vereist voor DLQ acties" });

            try
            {
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                // Ensure target queue exists
                await channel.QueueDeclareAsync(queue: queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

                int current = 0;
                bool done = false;
                int requeued = 0;

                while (true)
                {
                    var result = await channel.BasicGetAsync(dlqName, autoAck: false);
                    if (result == null) break;

                    var body = result.Body.ToArray();

                    if (current == index && !done)
                    {
                        var props = new BasicProperties();
                        props.Persistent = true;
                        if (result.BasicProperties?.Headers != null) props.Headers = result.BasicProperties.Headers;

                        await channel.BasicPublishAsync(exchange: "", routingKey: queue, mandatory: false, basicProperties: props, body: body);
                        await channel.BasicAckAsync(result.DeliveryTag, multiple: false);
                        requeued++;
                        done = true;
                    }
                    else
                    {
                        // republish back to DLQ to keep other messages
                        var backProps = new BasicProperties();
                        backProps.Persistent = true;
                        if (result.BasicProperties?.Headers != null) backProps.Headers = result.BasicProperties.Headers;

                        await channel.BasicPublishAsync(exchange: "", routingKey: dlqName, mandatory: false, basicProperties: backProps, body: body);
                        await channel.BasicAckAsync(result.DeliveryTag, multiple: false);
                    }

                    current++;
                }

                return Ok(new { success = true, requeued });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error requeueing single DLQ message");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteOne([FromQuery] string queue = "orders", [FromQuery] int index = 0)
        {
            var dlqName = queue + "-dlq";

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            if (!IsRequestAuthorized()) return Unauthorized(new { error = "API-key vereist voor DLQ acties" });

            try
            {
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                int current = 0;
                bool done = false;
                int deleted = 0;

                while (true)
                {
                    var result = await channel.BasicGetAsync(dlqName, autoAck: false);
                    if (result == null) break;

                    var body = result.Body.ToArray();

                    if (current == index && !done)
                    {
                        // acknowledge to delete
                        await channel.BasicAckAsync(result.DeliveryTag, multiple: false);
                        deleted++;
                        done = true;
                    }
                    else
                    {
                        // republish back to DLQ
                        var backProps = new BasicProperties();
                        backProps.Persistent = true;
                        if (result.BasicProperties?.Headers != null) backProps.Headers = result.BasicProperties.Headers;

                        await channel.BasicPublishAsync(exchange: "", routingKey: dlqName, mandatory: false, basicProperties: backProps, body: body);
                        await channel.BasicAckAsync(result.DeliveryTag, multiple: false);
                    }

                    current++;
                }

                return Ok(new { success = true, deleted });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting single DLQ message");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> Count([FromQuery] string queue = "orders")
        {
            var dlqName = queue + "-dlq";

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            try
            {
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                var ok = await channel.QueueDeclareAsync(dlqName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                var count = (int)ok.MessageCount;
                return Ok(new { queue = dlqName, count });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching DLQ count");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("purge")]
        public async Task<IActionResult> Purge([FromQuery] string queue = "orders")
        {
            var dlqName = queue + "-dlq";

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            try
            {
                if (!IsRequestAuthorized()) return Unauthorized(new { error = "API-key vereist voor DLQ acties" });
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                // Purge DLQ
                await channel.QueuePurgeAsync(dlqName);

                return Ok(new { success = true });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error purging DLQ");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
