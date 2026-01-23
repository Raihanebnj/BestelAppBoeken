using System;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

public static class Program
{
    public static void Main(string[] args)
    {
        // Read config from environment or use defaults for local dev (bestelapp / Groep3)
        var host = Environment.GetEnvironmentVariable("RABBIT_HOST") ?? "10.2.160.223";
        var user = Environment.GetEnvironmentVariable("RABBIT_USERNAME") ?? "bestelapp";
        var pass = Environment.GetEnvironmentVariable("RABBIT_PASSWORD") ?? "Groep3";
        var queue = Environment.GetEnvironmentVariable("RABBIT_QUEUE") ?? "order-updates";

        // Optional: allow order id and customer name from args
        var orderIdArg = args.Length > 0 ? args[0] : "1";
        var customerNameArg = args.Length > 1 ? args[1] : "Jan Jansen";

        if (!int.TryParse(orderIdArg, out var orderId))
        {
            orderId = 1;
        }

        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = user,
            Password = pass
        };

        try
        {
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: queue,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var payload = new
            {
                SalesforceId = "00Dxxx0000",
                Status = "Completed",
                Description = $"Web Order #{orderId} from Salesforce",
                CustomerName = customerNameArg,
                UpdatedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payload);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "",
                                 routingKey: queue,
                                 basicProperties: null,
                                 body: body);

            Console.WriteLine("Published test message to queue '{0}':", queue);
            Console.WriteLine(json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to publish test message: " + ex.Message);
            Environment.ExitCode = 1;
        }
    }
}