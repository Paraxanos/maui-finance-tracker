using System.Text.Json.Serialization;

namespace FinanceTracker.Models;

public sealed record UserProfile(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("bankAccounts")] List<BankAccount> BankAccounts,
    [property: JsonPropertyName("hasCompletedSetup")] bool HasCompletedSetup = false);
