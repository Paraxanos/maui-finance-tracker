namespace FinanceTracker.Models;

public sealed record BudgetAllocation(
    Guid Id,
    string Category,
    decimal Limit,
    DateTime BudgetMonth,
    DateTime UpdatedAtUtc);
