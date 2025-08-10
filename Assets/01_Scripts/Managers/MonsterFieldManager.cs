using System;
using UnityEngine;

// ���� ����: "ī��Ʈ��" ���� (���� ���� ��ȯ�� GameManager�� �̺�Ʈ �����ؼ� ó��)
public class MonsterFieldManager : MonoSingleton<MonsterFieldManager>, IMonsterFieldService
{
    [Header("�ʵ� ���� �ѵ�")]
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

    // ����: ����/�������� ��ȯ �� ���� �ʱ�ȭ��
    public void ResetCount()
    {
        CurrentCount = 0;
        OnCountChanged?.Invoke(CurrentCount, fieldLimit);
    }

    // ����: ���̵��� ���� �ѵ� ��Ÿ�� ����
    public void SetFieldLimit(int newLimit, bool clampCount = true)
    {
        fieldLimit = Mathf.Max(1, newLimit);
        if (clampCount && CurrentCount > fieldLimit) CurrentCount = fieldLimit;
        OnCountChanged?.Invoke(CurrentCount, fieldLimit);
    }
}
