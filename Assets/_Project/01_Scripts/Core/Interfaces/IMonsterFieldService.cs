using System;

public interface IMonsterFieldService
{
    int GetCurrentCount(int playerIndex);
    int GetFieldLimit(int playerIndex);

    /// <summary>(playerIndex, currentCount, fieldLimit)</summary>
    event Action<int, int, int> OnCountChanged;

    /// <summary>한도 도달한 playerIndex</summary>
    event Action<int> OnLimitReached;

    void Register(Monster m, int playerIndex);
    void Unregister(Monster m, int playerIndex);
}
