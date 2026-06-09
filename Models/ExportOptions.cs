namespace FinanceTracker.Models;

public sealed record ExportOptions(
    DateTime StartDate,
    DateTime EndDate,
    Guid? AccountId = null,
    bool IncludeProfile = true,
    bool IncludeBudgets = true);
