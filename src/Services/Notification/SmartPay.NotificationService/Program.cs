using SmartPay.BuildingBlocks;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<NotificationStore>();
var app = builder.Build();
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
public sealed class NotificationStore { public List<NotificationResponse> Notifications { get; } = new(); }
