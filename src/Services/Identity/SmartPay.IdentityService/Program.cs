using Microsoft.EntityFrameworkCore;
using SmartPay.BuildingBlocks;
using SmartPay.IdentityService.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<SmartPay.IdentityService.Data.Customer>,
    Microsoft.AspNetCore.Identity.PasswordHasher<SmartPay.IdentityService.Data.Customer>>();


builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("IdentityDb")));

var app = builder.Build();

// For the initial local prototype. We'll replace this with EF migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/health", () =>
    Results.Ok(new HealthResponse("Identity", "Healthy", DateTimeOffset.UtcNow)));

app.MapPost("/customers", async (
    CreateCustomerRequest request,
    IdentityDbContext db,
    Microsoft.AspNetCore.Identity.IPasswordHasher<SmartPay.IdentityService.Data.Customer> passwordHasher) =>
{
    if (string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { error = "Password is required." });

    if (request.Password.Length < 8)
        return Results.BadRequest(new { error = "Password must be at least 8 characters." });

    if (string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new { error = "Email is required." });

    var email = request.Email.Trim().ToLowerInvariant();

    var emailExists = await db.Customers.AnyAsync(x => x.Email == email);
    if (emailExists)
        return Results.Conflict(new { error = "A customer with this email already exists." });

    var customer = new Customer
    {
        Id = Guid.NewGuid(),
        Email = email,
        KycStatus = "PendingKyc",
        PasswordHash = string.Empty,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    customer.PasswordHash = passwordHasher.HashPassword(customer, request.Password);
    
    db.Customers.Add(customer);
    await db.SaveChangesAsync();

    var response = new CustomerResponse(
        customer.Id,
        customer.Email,
        customer.KycStatus);

    return Results.Created($"/customers/{customer.Id}", response);
});

app.MapPost("/login", async (
    LoginRequest request,
    IdentityDbContext db,
    Microsoft.AspNetCore.Identity.IPasswordHasher<SmartPay.IdentityService.Data.Customer> passwordHasher) =>
{
    var email = request.Email.Trim().ToLowerInvariant();

    var customer = await db.Customers
        .FirstOrDefaultAsync(x => x.Email == email);

    if (customer is null)
        return Results.Unauthorized();

    var result = passwordHasher.VerifyHashedPassword(
        customer,
        customer.PasswordHash,
        request.Password);

    if (result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        customerId = customer.Id,
        email = customer.Email,
        kycStatus = customer.KycStatus
    });
});

app.MapGet("/customers/{id:guid}", async (
    Guid id,
    IdentityDbContext db) =>
{
    var customer = await db.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == id);

    if (customer is null)
        return Results.NotFound();

    return Results.Ok(new CustomerResponse(
        customer.Id,
        customer.Email,
        customer.KycStatus));
});

app.Run();

public sealed record CreateCustomerRequest(string Email, string Password);

public sealed record CustomerResponse(
    Guid Id,
    string Email,
    string KycStatus);


public sealed record LoginRequest(string Email, string Password);
