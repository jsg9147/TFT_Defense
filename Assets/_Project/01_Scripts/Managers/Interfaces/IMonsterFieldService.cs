// IMonsterFieldService.cs
using System;

public interface IMonsterFieldService
{
    int CurrentCount { get; }
    int FieldLimit { get; }

    event Action<int, int> OnCountChanged;
    event Action OnLimitReached;

    void Register(Monster m);
    void Unregister(Monster m);
}
