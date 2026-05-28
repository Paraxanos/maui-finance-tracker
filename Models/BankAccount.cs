using System.Text.Json.Serialization;

namespace FinanceTracker.Models;

public sealed record BankAccount(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("initialBalance")] decimal InitialBalance,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc);
