using FinanceTracker.Models;

namespace FinanceTracker.Services;

public interface IFinanceDataService
{
    IReadOnlyList<FinanceRecord> Transactions { get; }

    IReadOnlyList<BudgetAllocation> Budgets { get; }

    event EventHandler? TransactionsChanged;

    event EventHandler? BudgetsChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task AddTransactionAsync(FinanceRecord record, CancellationToken cancellationToken = default);

    Task UpdateTransactionAsync(FinanceRecord record, CancellationToken cancellationToken = default);

    Task DeleteTransactionAsync(Guid id, CancellationToken cancellationToken = default);

    Task SetBudgetAsync(BudgetAllocation budget, CancellationToken cancellationToken = default);

    Task DeleteBudgetAsync(string category, DateTime budgetMonth, CancellationToken cancellationToken = default);
}
