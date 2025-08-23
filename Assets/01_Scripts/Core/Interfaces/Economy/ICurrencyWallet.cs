// ICurrencyWallet.cs
public interface ICurrencyWallet
{
    int Gold { get; }
    bool TrySpendGold(int amount);
    void AddGold(int amount);
}
