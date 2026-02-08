using System;
using UnityEngine;

/// <summary>
/// 플레이어별 몬스터 필드 누적 카운트 관리.
/// 한도 초과 시 해당 플레이어의 패배 이벤트를 발행한다.
/// </summary>
public class MonsterFieldManager : MonoSingleton<MonsterFieldManager>, IMonsterFieldService
{
    private const int MaxPlayers = 2;

    [Header("필드 누적 한도")]
    [SerializeField] private int fieldLimit = 50;

    private readonly int[] fieldCounts = new int[MaxPlayers];

    public int GetCurrentCount(int playerIndex) => fieldCounts[ClampIndex(playerIndex)];
    public int GetFieldLimit(int playerIndex) => fieldLimit;

    /// <summary>(playerIndex, currentCount, fieldLimit)</summary>
    public event Action<int, int, int> OnCountChanged;

    /// <summary>한도 도달한 playerIndex</summary>
    public event Action<int> OnLimitReached;

    public void Register(Monster m, int playerIndex)
    {
        int idx = ClampIndex(playerIndex);
        fieldCounts[idx]++;
        OnCountChanged?.Invoke(idx, fieldCounts[idx], fieldLimit);

        if (fieldCounts[idx] >= fieldLimit)
            OnLimitReached?.Invoke(idx);

        Debug.Log($"[MonsterField] 등록: {m.name}, Player {idx}, Count: {fieldCounts[idx]}/{fieldLimit}");
    }

    public void Unregister(Monster m, int playerIndex)
    {
        int idx = ClampIndex(playerIndex);
        if (fieldCounts[idx] > 0) fieldCounts[idx]--;
        OnCountChanged?.Invoke(idx, fieldCounts[idx], fieldLimit);
    }

    public void ResetCount()
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            fieldCounts[i] = 0;
            OnCountChanged?.Invoke(i, 0, fieldLimit);
        }
    }

    public void SetFieldLimit(int newLimit, bool clampCount = true)
    {
        fieldLimit = Mathf.Max(1, newLimit);
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (clampCount && fieldCounts[i] > fieldLimit)
                fieldCounts[i] = fieldLimit;
            OnCountChanged?.Invoke(i, fieldCounts[i], fieldLimit);
        }
    }

    private static int ClampIndex(int playerIndex) => Mathf.Clamp(playerIndex, 0, MaxPlayers - 1);
}
