namespace FinanceTracker.Models;

public sealed record FinanceRecord(
    Guid Id,
    string Title,
    string Category,
    string Note,
    decimal Amount,
    FinanceEntryType EntryType,
    DateTime EntryDate,
    bool IsCleared,
    DateTime CreatedAtUtc);
