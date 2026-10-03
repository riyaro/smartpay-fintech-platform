namespace SmartPay.BuildingBlocks;
public sealed record HealthResponse(string Service, string Status, DateTimeOffset TimestampUtc);
public static class CurrencyCodes { public const string Sar = "SAR"; }
