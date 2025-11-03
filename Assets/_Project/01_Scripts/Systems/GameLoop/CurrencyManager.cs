using UnityEngine;
using System;

public class CurrencyManager : MonoSingleton<CurrencyManager>
{
    [SerializeField] private int initialGold = 100;
    [SerializeField] private int initialEssence = 0;   // 초기 정수

    public int Gold { get; private set; } = 0;
    public int Gem { get; private set; } = 0;
    public int Essence { get; private set; } = 0;      // 정수

    public event Action<int> OnGoldChanged;
    public event Action<int> OnGemChanged;
    public event Action<int> OnEssenceChanged;         // 정수 이벤트

    // 게임 시작 시 초기 골드 설정, 나중에 스타트가 아니라 게임매니저에서 할당하는 쪽이 더 좋을듯
    private void Start()
    {
        Gold = initialGold;
        Essence = initialEssence;                      // 초기화
        OnGoldChanged?.Invoke(Gold);
        OnEssenceChanged?.Invoke(Essence);            // UI 갱신
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
}
