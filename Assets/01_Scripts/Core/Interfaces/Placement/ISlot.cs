// ISlot.cs
public interface ISlot
{
    bool HasItem { get; }
    void Place(IPlaceable item);
    IPlaceable Remove();
}
