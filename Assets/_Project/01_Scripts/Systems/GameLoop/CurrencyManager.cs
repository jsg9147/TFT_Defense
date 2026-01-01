using UnityEngine;
using System;

public class CurrencyManager : MonoSingleton<CurrencyManager>
{
    [SerializeField] private int initialGold = 100;
    [SerializeField] private int initialEssence = 0;   // �ʱ� ����

    public int Gold { get; private set; } = 0;
    public int Gem { get; private set; } = 0;
    public int Essence { get; private set; } = 0;      // ����

    public event Action<int> OnGoldChanged;
    public event Action<int> OnGemChanged;
    public event Action<int> OnEssenceChanged;         // ���� �̺�Ʈ

    // ���� ���� �� �ʱ� ��� ����, ���߿� ��ŸƮ�� �ƴ϶� ���ӸŴ������� �Ҵ��ϴ� ���� �� ������
    private void Start()
    {
        Gold = initialGold;
        Essence = initialEssence;                      // �ʱ�ȭ
        OnGoldChanged?.Invoke(Gold);
        OnEssenceChanged?.Invoke(Essence);            // UI ����
    }

    public void AddGold(int amount) { Gold += amount; OnGoldChanged?.Invoke(Gold); }
    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount; OnGoldChanged?.Invoke(Gold);
        return true;
    }

    public void AddGem(int amount) { Gem += amount; OnGemChanged?.Invoke(Gem); }

    public void AddEssence(int amount) { Essence += amount; OnEssenceChanged?.Invoke(Essence); }
    public bool SpendEssence(int amount)
    {
        if (Essence < amount) return false;
        Essence -= amount; OnEssenceChanged?.Invoke(Essence);
        return true;
    }

    /// <summary>게임 재시작 시 초기화</summary>
    public void Reset()
    {
        Gold = initialGold;
        Gem = 0;
        Essence = initialEssence;
        OnGoldChanged?.Invoke(Gold);
        OnGemChanged?.Invoke(Gem);
        OnEssenceChanged?.Invoke(Essence);
    }
}
