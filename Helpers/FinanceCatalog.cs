using FinanceTracker.Models;

namespace FinanceTracker.Helpers;

public static class FinanceCatalog
{
    public static readonly IReadOnlyList<string> ExpenseCategories =
    [
        "Food",
        "Transport",
        "Housing",
        "Utilities",
        "Health",
        "Shopping",
        "Entertainment",
        "Other"
    ];

    public static readonly IReadOnlyList<string> IncomeCategories =
    [
        "Salary",
        "Freelance",
        "Bonus",
        "Refund",
        "Investment",
        "Other"
    ];

    public static IReadOnlyList<string> GetCategories(FinanceEntryType entryType)
    {
        return entryType == FinanceEntryType.Income ? IncomeCategories : ExpenseCategories;
    }

    public static string ResolveCategoryIcon(string category)
    {
        return category switch
        {
            "Salary" => "💼",
            "Freelance" => "🧑‍💻",
            "Bonus" => "✨",
            "Refund" => "↩",
            "Investment" => "📈",
            "Food" => "🍜",
            "Transport" => "🚕",
            "Housing" => "🏠",
            "Utilities" => "⚡",
            "Health" => "🩺",
            "Shopping" => "🛍",
            "Entertainment" => "🎞",
            _ => "🧾"
        };
    }
}
