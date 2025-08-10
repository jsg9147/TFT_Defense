using System;
using UnityEngine;

// 전역 서비스: "카운트만" 관리 (게임 상태 전환은 GameManager가 이벤트 구독해서 처리)
public class MonsterFieldManager : MonoSingleton<MonsterFieldManager>, IMonsterFieldService
{
    [Header("필드 누적 한도")]
    [SerializeField] private int fieldLimit = 50;
    public int FieldLimit => fieldLimit;

    public int CurrentCount { get; private set; }

    public event Action<int, int> OnCountChanged;
    public event Action OnLimitReached;

    public void Register(Monster m)
    {
        CurrentCount++;
        OnCountChanged?.Invoke(CurrentCount, fieldLimit);
        if (CurrentCount >= fieldLimit)
            OnLimitReached?.Invoke();
    }

    public void Unregister(Monster m)
    {
        if (CurrentCount > 0) CurrentCount--;
        OnCountChanged?.Invoke(CurrentCount, fieldLimit);
    }

    // 선택: 리셋/스테이지 전환 시 점수 초기화용
    public void ResetCount()
    {
        CurrentCount = 0;
        OnCountChanged?.Invoke(CurrentCount, fieldLimit);
    }

    // 선택: 난이도에 따라 한도 런타임 조정
    public void SetFieldLimit(int newLimit, bool clampCount = true)
    {
        fieldLimit = Mathf.Max(1, newLimit);
        if (clampCount && CurrentCount > fieldLimit) CurrentCount = fieldLimit;
        OnCountChanged?.Invoke(CurrentCount, fieldLimit);
    }
}
