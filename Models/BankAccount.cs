namespace FinanceTracker.Models;

public sealed record BankAccount(
    Guid Id,
    string Name,
    decimal InitialBalance,
    DateTime CreatedAtUtc);
