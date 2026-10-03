using SmartPay.BuildingBlocks;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IdentityStore>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new HealthResponse("Identity", "Healthy", DateTimeOffset.UtcNow)));

app.MapPost("/customers", (CreateCustomerRequest request, CustomerStore store) => {
    if (string.IsNullOrWhiteSpace(request.Email)) return Results.BadRequest(new { error = "Email is required." });
    var customer = new CustomerResponse(Guid.NewGuid(), request.Email.Trim(), "PendingKyc");
    store.Customers[customer.Id] = customer;
    return Results.Created($"/customers/{customer.Id}", customer);
});
app.MapGet("/customers/{id:guid}", (Guid id, CustomerStore store) => store.Customers.TryGetValue(id, out var item) ? Results.Ok(item) : Results.NotFound());

app.Run();

public sealed record CreateCustomerRequest(string Email);
public sealed record CustomerResponse(Guid Id, string Email, string KycStatus);
public sealed class CustomerStore { public Dictionary<Guid, CustomerResponse> Customers { get; } = new(); }
