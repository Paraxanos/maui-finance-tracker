using System.Text.Json.Serialization;

namespace FinanceTracker.Models;

public sealed record ExportSnapshot(
    [property: JsonPropertyName("schema_version")] string SchemaVersion = "1.0.0",
    [property: JsonPropertyName("app_version")] string AppVersion = "1.0",
    [property: JsonPropertyName("exported_at_utc")] DateTime ExportedAtUtc = default,
    [property: JsonPropertyName("profile")] UserProfile? Profile = null,
    [property: JsonPropertyName("accounts")] List<BankAccount> Accounts = null!,
    [property: JsonPropertyName("transactions")] List<FinanceRecord> Transactions = null!,
    [property: JsonPropertyName("budgets")] List<BudgetAllocation> Budgets = null!,
    [property: JsonPropertyName("summary")] ExportSummary Summary = null!);

public sealed record ExportSummary(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Net,
    int RecordCount);
