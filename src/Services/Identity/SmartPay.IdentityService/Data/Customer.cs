namespace SmartPay.IdentityService.Data;

public sealed class Customer
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string KycStatus { get; set; } = "PendingKyc";
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
