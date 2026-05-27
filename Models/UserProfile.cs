namespace FinanceTracker.Models;

public sealed record UserProfile(
    string Name,
    string Email,
    List<BankAccount> BankAccounts);
