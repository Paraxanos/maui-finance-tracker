namespace FinanceTracker.ViewModels;

public sealed class BankAccountSelectionItem
{
    public Guid? Id { get; }
    public string Name { get; }

    public BankAccountSelectionItem(Guid? id, string name)
    {
        Id = id;
        Name = name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BankAccountSelectionItem other)
        {
            return false;
        }
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id?.GetHashCode() ?? 0;
    }

    public override string ToString() => Name;
}
