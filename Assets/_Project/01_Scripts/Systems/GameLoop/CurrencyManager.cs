using UnityEngine;
using System;
using Unity.VisualScripting;

public class CurrencyManager : MonoSingleton<CurrencyManager>
{

    [SerializeField] private int initialGold = 100; // 초기 골드 설정

    public int Gold { get; private set; } = 0;
    public int Gem { get; private set; } = 0;

    public event Action<int> OnGoldChanged;
    public event Action<int> OnGemChanged;

    // 게임 시작 시 초기 골드 설정, 나중에 스타트가 아니라 게임매니저에서 할당하는 쪽이 더 좋을듯
    private void Start()
    {
        Gold = initialGold; // 게임 시작 시 초기 골드 설정
        OnGoldChanged?.Invoke(Gold); // 초기 골드 UI 업데이트
    }

    // 골드 획득
    public void AddGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    // 골드 사용
    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    // 젬도 같은 방식
    public void AddGem(int amount)
    {
        Gem += amount;
        OnGemChanged?.Invoke(Gem);
    }
}
