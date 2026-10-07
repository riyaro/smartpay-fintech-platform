using SmartPay.BuildingBlocks;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<NotificationStore>();
builder.Services.AddHostedService<PaymentEventConsumer>();
var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    context.Response.Headers["X-Correlation-ID"] = correlationId;

    using (app.Logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        app.Logger.LogInformation(
            "Request started: {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        await next();

        app.Logger.LogInformation(
            "Request completed: {Method} {Path} {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode);
    }
});

app.MapGet("/health", () => Results.Ok(new HealthResponse("Notification", "Healthy", DateTimeOffset.UtcNow)));

app.MapPost("/notifications/demo", (CreateNotificationRequest request, NotificationStore store) => {
    var item = new NotificationResponse(Guid.NewGuid(), request.Recipient, request.Message, "Queued", DateTimeOffset.UtcNow);
    store.Notifications.Add(item);
    return Results.Accepted($"/notifications/{item.Id}", item);
});
app.MapGet("/notifications", (NotificationStore store) => Results.Ok(store.Notifications));

app.Run();

public sealed record CreateNotificationRequest(string Recipient, string Message);
public sealed record NotificationResponse(Guid Id, string Recipient, string Message, string Status, DateTimeOffset CreatedAtUtc);

public sealed class NotificationStore
{
    private readonly HashSet<string> _processedEventIds = new();
    private readonly object _lock = new();

    public List<NotificationResponse> Notifications { get; } = new();

    public bool AddIfNew(
        string eventId,
        NotificationResponse notification)
        {
            lock (_lock)
            {
                if (!_processedEventIds.Add(eventId))
                {
                    return false;
                }

            Notifications.Add(notification);
            return true;
            
            }
        }
}
