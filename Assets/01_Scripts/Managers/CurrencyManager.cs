using UnityEngine;
using System;
using Unity.VisualScripting;

public class CurrencyManager : MonoSingleton<CurrencyManager>
{

    [SerializeField] private int initialGold = 100; // �ʱ� ��� ����

    public int Gold { get; private set; } = 0;
    public int Gem { get; private set; } = 0;

    public event Action<int> OnGoldChanged;
    public event Action<int> OnGemChanged;

    // ���� ���� �� �ʱ� ��� ����, ���߿� ��ŸƮ�� �ƴ϶� ���ӸŴ������� �Ҵ��ϴ� ���� �� ������
    private void Start()
    {
        Gold = initialGold; // ���� ���� �� �ʱ� ��� ����
        OnGoldChanged?.Invoke(Gold); // �ʱ� ��� UI ������Ʈ
    }

    // ��� ȹ��
    public void AddGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    // ��� ���
    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    // ���� ���� ���
    public void AddGem(int amount)
    {
        Gem += amount;
        OnGemChanged?.Invoke(Gem);
    }
}
