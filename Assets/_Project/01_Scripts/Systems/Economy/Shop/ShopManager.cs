using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    [Header("���� ����")]
    public Transform slotParent;                 // ���Ե��� ���� �θ� ������Ʈ
    public List<UnitData> allUnitDatas;          // ���� ������ ���� ���

    [Header("���� ���")]
    public int unitCost = 2;

    [Header("����ġ ����")]
    public int expCost = 4;    // ��� �Ҹ�
    public int expPerBuy = 4;  // ���� �� ȹ�� ����ġ

    [Header("Ȯ�� UI")]
    public ShopProbabilityUI probabilityUI;
    public List<ShopProbabilityTable> probabilityTables;

    [Header("���� UI(����)")]
    [SerializeField] private Transform costButtonParent;
    [SerializeField] private Transform jobButtonParent;
    [SerializeField] private Transform originButtonParent;

    private readonly Dictionary<int, List<UnitData>> byCost = new();
    private readonly Dictionary<JobSynergy, List<UnitData>> byJob = new();
    private readonly Dictionary<OriginSynergy, List<UnitData>> byOrigin = new();

    private readonly HashSet<int> selectedCosts = new();
    private readonly HashSet<JobSynergy> selectedJobs = new();
    private readonly HashSet<OriginSynergy> selectedOrigins = new();

    private List<ShopSlotUI> currentSlots = new List<ShopSlotUI>();

    void Start()
    {
        RefreshShop();
    }

    /// <summary>
    /// ���� ���� ���� ���� ��ħ (���� ����)
    /// </summary>
    public void RefreshShop()
    {
        ClearCurrentSlots();
        UpdateProbabilityUI();

        List<UnitData> selected = GetRandomUnitListWeighted(5);  // ����ġ ������� ����

        foreach (var data in selected)
        {
            ShopSlotUI slot = UIManager.Instance.ShopSlotPool.GetSlot(slotParent);
            slot.Init(data, OnBuyUnit);
            currentSlots.Add(slot);
        }
    }
    // ���� Ȯ�� ��� ����
    List<UnitData> GetRandomUnitListWeighted(int count)
    {
        List<UnitData> result = new List<UnitData>();

        if (allUnitDatas == null || allUnitDatas.Count == 0)
        {
            Debug.LogWarning("���� �����Ͱ� �����ϴ�.");
            return result;
        }

        int level = PlayerLevelManager.Instance.Level;
        int idx = Mathf.Clamp(level - 1, 0, probabilityTables.Count - 1); // �� 1-��� ���� ����
        if (probabilityTables == null || probabilityTables.Count == 0)
        {
            Debug.LogWarning("Ȯ�� ���̺��� �����ϴ�. �յ� �������� �̽��ϴ�.");
            return GetRandomUnitList_Fallback(count);
        }

        var table = probabilityTables[idx];
        table.Normalize();
        float[] probs = table.GetProbabilities(); // ���� 5, ��=1

        for (int i = 0; i < count; i++)
        {
            int targetCost = PickCostByWeight(probs); // 1~5
            UnitData pick = PickUnitByCostWithFallback(targetCost);
            if (pick != null) result.Add(pick);
        }

        return result;
    }

    // ���� ���� ���(�յ�) ����
    List<UnitData> GetRandomUnitList_Fallback(int count)
    {
        List<UnitData> result = new List<UnitData>();
        for (int i = 0; i < count; i++)
        {
            int rand = Random.Range(0, allUnitDatas.Count);
            result.Add(allUnitDatas[rand]);
        }
        return result;
    }

    int PickCostByWeight(float[] probs) // probs ���� 5, ��=1
    {
        float roll = Random.value; // 0~1
        float acc = 0f;
        for (int i = 0; i < probs.Length; i++)
        {
            acc += probs[i];
            if (roll <= acc) return i + 1; // �ڽ�Ʈ=�ε���+1
        }
        return 1;
    }

    UnitData PickUnitByCostWithFallback(int cost)
    {
        // 1) ��ǥ �ڽ�Ʈ
        var candidate = PickOneOfCost(cost);
        if (candidate != null) return candidate;

        // 2) ���� ����
        for (int c = cost + 1; c <= 5; c++)
        {
            candidate = PickOneOfCost(c);
            if (candidate != null) return candidate;
        }
        // 3) �Ʒ��� ����
        for (int c = cost - 1; c >= 1; c--)
        {
            candidate = PickOneOfCost(c);
            if (candidate != null) return candidate;
        }
        return null;
    }

    UnitData PickOneOfCost(int cost)
    {
        // UnitData�� cost �ʵ尡 �̹� ���� (�� �ڵ� ����)
        var list = allUnitDatas.Where(u => u != null && u.cost == cost).ToList();
        if (list.Count == 0) return null;
        int r = Random.Range(0, list.Count);
        return list[r];
    }

    // �ε��� ���� & �׻� ����ȭ �� UI ������Ʈ
    void UpdateProbabilityUI()
    {
        if (probabilityUI == null || probabilityTables == null || probabilityTables.Count == 0)
        {
            Debug.LogWarning("Ȯ�� UI �Ǵ� Ȯ�� ���̺��� �������� �ʾҽ��ϴ�.");
            return;
        }

        int level = PlayerLevelManager.Instance.Level;
        int idx = level - 1; // 1-��� �� 0-���
        if (idx < 0 || idx >= probabilityTables.Count)
        {
            Debug.LogWarning($"���� �÷��̾� ����({level})�� �ش��ϴ� Ȯ�� ���̺��� �����ϴ�.");
            return;
        }

        var currentTable = probabilityTables[idx];
        currentTable.Normalize();
        probabilityUI.UpdateRates(currentTable.GetProbabilities());
    }

    void ClearCurrentSlots()
    {
        foreach (var slot in currentSlots)
        {
            if (slot != null)
                UIManager.Instance.ShopSlotPool.ReturnSlot(slot); // Ǯ�� ��ȯ
        }
        currentSlots.Clear();
    }


    /// <summary>
    /// ���� ���� �� ����
    /// </summary>
    void OnBuyUnit(UnitData data)
    {
        // CurrencyManager���� ��� üũ
        if (!CurrencyManager.Instance.SpendGold(unitCost))
        {
            Debug.Log("��� ����");
            return;
        }

        bool success = GameManager.Instance.benchManager.TryAddHero(data);
        if (!success)
        {
            Debug.Log("���� ���� ����: ��ġ�� ���� á���ϴ�.");

            // ���� ���� �� ��� ȯ��
            CurrencyManager.Instance.AddGold(unitCost);
            return;
        }

        Debug.Log($"{data.unitName} ���ŵ�");
    }

    /// <summary>
    /// ���� SO���� 5�� ������ ����
    /// </summary>
    List<UnitData> GetRandomUnitList(int count)
    {
        List<UnitData> result = new List<UnitData>();

        if (allUnitDatas == null || allUnitDatas.Count == 0)
        {
            Debug.LogWarning("���� �����Ͱ� �����ϴ�.");
            return result;
        }

        for (int i = 0; i < count; i++)
        {
            int rand = Random.Range(0, allUnitDatas.Count);
            result.Add(allUnitDatas[rand]);
        }

        return result;
    }

    public void BuyExp()
    {
        if (probabilityTables.Count < PlayerLevelManager.Instance.Level)
        {
            Debug.LogWarning("���� �÷��̾� ������ �ش��ϴ� Ȯ�� ���̺��� �����ϴ�. Level :" + PlayerLevelManager.Instance.Level);
            return;
        }

        if (!CurrencyManager.Instance.SpendGold(expCost))
        {
            Debug.Log("��� ����");
            return;
        }

        PlayerLevelManager.Instance.AddExp(expPerBuy);
        UpdateProbabilityUI();
    }
}
