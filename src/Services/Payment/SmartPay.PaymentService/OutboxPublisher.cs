using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace SmartPay.PaymentService;

public sealed class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly IConfiguration _configuration;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisher> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = _configuration["RabbitMQ:Username"] ?? "smartpay",
            Password = _configuration["RabbitMQ:Password"] ?? "local_dev_only"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: "payment.events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _logger.LogInformation("Outbox publisher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<PaymentDbContext>();

                var messages = await db.OutboxMessages
                    .Where(x => x.ProcessedAtUtc == null)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        var body = System.Text.Encoding.UTF8.GetBytes(
                            message.Payload);

                        var properties = channel.CreateBasicProperties();
                        properties.Persistent = true;
                        properties.MessageId = message.Id.ToString();
                        properties.Type = message.Type;

                        channel.BasicPublish(
                            exchange: "",
                            routingKey: "payment.events",
                            basicProperties: properties,
                            body: body);

                        message.ProcessedAtUtc = DateTimeOffset.UtcNow;

                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "Published outbox message {MessageId}",
                            message.Id);
                    }
                    catch (Exception ex)
                        when (ex is not OperationCanceledException)
                    {
                        message.RetryCount++;

                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogError(
                            ex,
                            "Failed to publish outbox message {MessageId}",
                            message.Id);
                    }
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
