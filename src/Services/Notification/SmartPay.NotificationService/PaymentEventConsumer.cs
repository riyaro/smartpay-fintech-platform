using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public sealed class PaymentEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly NotificationStore _store;
    private readonly ILogger<PaymentEventConsumer> _logger;

    public PaymentEventConsumer(
        IConfiguration configuration,
        NotificationStore store,
        ILogger<PaymentEventConsumer> logger)
    {
        _configuration = configuration;
        _store = store;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = _configuration["RabbitMQ:Username"] ?? "smartpay",
            Password = _configuration["RabbitMQ:Password"] ?? "local_dev_only"
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: "payment.events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(0, 1, false);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (_, eventArgs) =>
        {
            try
            {
                var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var eventId = eventArgs.BasicProperties.MessageId;

                if (string.IsNullOrWhiteSpace(eventId))
                {
                    _logger.LogWarning("Payment event has no message ID.");
                    channel.BasicNack(eventArgs.DeliveryTag, false, false);
                    return;
                }

                var eventType = eventArgs.BasicProperties.Type;

                if (eventType == "PaymentCreated")
                {
                    var notification = new NotificationResponse(
                        Guid.NewGuid(),
                        "payment",
                        $"Payment event received: {message}",
                        "Received",
                        DateTimeOffset.UtcNow);

                    _store.AddIfNew(eventId, notification);
                }

                channel.BasicAck(eventArgs.DeliveryTag, false);
                _logger.LogInformation(
                    "Processed payment event {MessageId}", eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process payment event.");
                channel.BasicNack(eventArgs.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(
            queue: "payment.events",
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Payment event consumer started.");

        stoppingToken.Register(() =>
        {
            channel.Close();
            connection.Close();
            channel.Dispose();
            connection.Dispose();
        });

        return Task.CompletedTask;
    }
}
