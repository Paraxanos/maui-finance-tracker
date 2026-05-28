using System.Globalization;
using FinanceTracker.Models;

namespace FinanceTracker.Helpers;

public static class FinanceMath
{
    public static decimal TotalIncome(IEnumerable<FinanceRecord> records)
    {
        return records
            .Where(item => item.EntryType == FinanceEntryType.Income)
            .Sum(item => item.Amount);
    }

    public static decimal TotalExpenses(IEnumerable<FinanceRecord> records)
    {
        return records
            .Where(item => item.EntryType == FinanceEntryType.Expense)
            .Sum(item => item.Amount);
    }

    public static decimal Net(IEnumerable<FinanceRecord> records)
    {
        return TotalIncome(records) - TotalExpenses(records);
    }

    public static decimal Balance(
        IEnumerable<FinanceRecord> records,
        IEnumerable<BankAccount> accounts,
        Guid? accountId = null)
    {
        var filteredRecords = accountId.HasValue
            ? records.Where(item => item.BankAccountId == accountId.Value)
            : records;

        var initialBalance = accountId.HasValue
            ? accounts.FirstOrDefault(account => account.Id == accountId.Value)?.InitialBalance ?? 0m
            : accounts.Sum(account => account.InitialBalance);

        return initialBalance + Net(filteredRecords);
    }

    public static decimal SignedAmount(FinanceRecord record)
    {
        return record.EntryType == FinanceEntryType.Income
            ? record.Amount
            : -record.Amount;
    }

    public static string Currency(decimal amount)
    {
        return amount.ToString("C2", CultureInfo.CurrentCulture);
    }

    public static string SignedCurrency(decimal amount)
    {
        var prefix = amount > 0 ? "+" : amount < 0 ? "-" : string.Empty;
        return $"{prefix}{Math.Abs(amount).ToString("C2", CultureInfo.CurrentCulture)}";
    }
}
