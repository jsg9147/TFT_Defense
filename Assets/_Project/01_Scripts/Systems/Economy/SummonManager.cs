using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance;

    [Header("��ȯ ����")]
    [Tooltip("��ȯ �� �Ҹ�Ǵ� ���")]
    public int summonCost = 2;

    [Header("����ġ ����")]
    public int expCost = 4;
    public int expPerBuy = 4;

    [Header("Ȯ�� ����")]
    public List<ShopProbabilityTable> probabilityTables;
    public ShopProbabilityUI probabilityUI;

    [Header("��ȯ Ǯ (���� ������ ����)")]
    public List<UnitData> allUnitDatas;

    void Awake() => Instance = this;

    void Start()
    {
        UpdateProbabilityUI();
    }

    public void SummonOnce() => SummonMultiple(1);

    public void SummonMultiple(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!TrySummonOne_AutoPlace()) break;
        }
    }

    /// <summary>
    /// �ڵ� ��ġ: ù ��° �� ĭ�� ã�� ��� ����/��ġ. ���� �� ȯ��.
    /// </summary>
    bool TrySummonOne_AutoPlace()
    {
        var gm = GridManager.Instance;
        var upm = UnitPlacementManager.Instance;

        if (gm == null || upm == null)
        {
            Debug.LogWarning("[Summon] GridManager/UnitPlacementManager �ʿ�");
            return false;
        }

        // 1) ��ġ�� ĭ �ִ��� ��Ȯ��
        if (!gm.TryFindFirstPlaceable(out var cell))
        {
            Debug.Log("��ġ�� ĭ�� �����ϴ�.");
            return false;
        }

        // 2) ��� ����
        if (!CurrencyManager.Instance.SpendGold(summonCost))
        {
            Debug.Log("��� ����");
            return false;
        }

        // 3) ���� ����
        UnitData pick = RollOneUnit();
        if (pick == null)
        {
            Debug.LogWarning("���� Ǯ �������");
            CurrencyManager.Instance.AddGold(summonCost); // ȯ��
            return false;
        }

        // 4) ���� ���� �� ��� ��ġ �õ�
        upm.SetSelectedUnit(pick);
        if (upm.TryPlaceAtCell(cell))
        {
            OnSummonSuccess(pick);
            return true;
        }

        // 5) ��ġ ����: ȯ�� + ���
        CurrencyManager.Instance.AddGold(summonCost);
        upm.CancelPlacement();
        Debug.Log("��ġ ���з� ȯ��");
        return false;
    }

    void OnSummonSuccess(UnitData data)
    {
        Debug.Log($"[Summon] {data.unitName} �ڵ� ��ġ �Ϸ�");
        // TODO: ���� ��� / ��New!�� ���� / ������ üũ / ����
    }

    // �� Ȯ�� ���� ���� ��
    UnitData RollOneUnit()
    {
        if (allUnitDatas == null || allUnitDatas.Count == 0)
            return null;

        int level = PlayerLevelManager.Instance.Level;
        if (probabilityTables == null || probabilityTables.Count == 0)
            return PickUniform(allUnitDatas);

        int idx = Mathf.Clamp(level - 1, 0, probabilityTables.Count - 1);
        var table = probabilityTables[idx];
        table.Normalize();
        float[] probs = table.GetProbabilities();

        int targetCost = PickCostByWeight(probs);
        return PickUnitByCostWithFallback(targetCost);
    }

    int PickCostByWeight(float[] probs)
    {
        float roll = Random.value;
        float acc = 0f;
        for (int i = 0; i < probs.Length; i++)
        {
            acc += probs[i];
            if (roll <= acc) return i + 1;
        }
        return 1;
    }

    UnitData PickUnitByCostWithFallback(int cost)
    {
        var list = allUnitDatas?.Where(u => u != null && u.cost == cost).ToList();
        if (list == null || list.Count == 0)
        {
            for (int c = cost + 1; c <= 5; c++)
            {
                list = allUnitDatas.Where(u => u != null && u.cost == c).ToList();
                if (list.Count > 0) break;
            }
        }
        if (list == null || list.Count == 0) return PickUniform(allUnitDatas);
        return list[Random.Range(0, list.Count)];
    }

    UnitData PickUniform(List<UnitData> list)
    {
        if (list == null || list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    public void BuyExp()
    {
        if (!CurrencyManager.Instance.SpendGold(expCost))
        {
            Debug.Log("��� ����");
            return;
        }

        PlayerLevelManager.Instance.AddExp(expPerBuy);
        UpdateProbabilityUI();
    }

    void UpdateProbabilityUI()
    {
        if (probabilityUI == null || probabilityTables == null || probabilityTables.Count == 0)
            return;

        int level = PlayerLevelManager.Instance.Level;
        int idx = Mathf.Clamp(level - 1, 0, probabilityTables.Count - 1);

        var table = probabilityTables[idx];
        table.Normalize();
        probabilityUI.UpdateRates(table.GetProbabilities());
    }
}
