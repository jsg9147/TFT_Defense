using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance;

    [Header("소환 설정")]
    [Tooltip("소환 시 소모되는 골드")]
    public int summonCost = 2;

    [Header("경험치 구매")]
    public int expCost = 4;
    public int expPerBuy = 4;

    [Header("확률 설정")]
    public List<ShopProbabilityTable> probabilityTables;
    public ShopProbabilityUI probabilityUI;

    [Header("소환 풀 (등장 가능한 유닛)")]
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
    /// 자동 배치: 첫 번째 빈 칸을 찾아 즉시 생성/배치. 실패 시 환불.
    /// </summary>
    bool TrySummonOne_AutoPlace()
    {
        var gm = GridManager.Instance;
        var upm = UnitPlacementManager.Instance;

        if (gm == null || upm == null)
        {
            Debug.LogWarning("[Summon] GridManager/UnitPlacementManager 필요");
            return false;
        }

        // 1) 배치할 칸 있는지 선확인
        if (!gm.TryFindFirstPlaceable(out var cell))
        {
            Debug.Log("배치할 칸이 없습니다.");
            return false;
        }

        // 2) 골드 차감
        if (!CurrencyManager.Instance.SpendGold(summonCost))
        {
            Debug.Log("골드 부족");
            return false;
        }

        // 3) 유닛 선택
        UnitData pick = RollOneUnit();
        if (pick == null)
        {
            Debug.LogWarning("유닛 풀 비어있음");
            CurrencyManager.Instance.AddGold(summonCost); // 환불
            return false;
        }

        // 4) 선택 세팅 후 즉시 배치 시도
        upm.SetSelectedUnit(pick);
        if (upm.TryPlaceAtCell(cell))
        {
            OnSummonSuccess(pick);
            return true;
        }

        // 5) 배치 실패: 환불 + 취소
        CurrencyManager.Instance.AddGold(summonCost);
        upm.CancelPlacement();
        Debug.Log("배치 실패로 환불");
        return false;
    }

    void OnSummonSuccess(UnitData data)
    {
        Debug.Log($"[Summon] {data.unitName} 자동 배치 완료");
        // TODO: 도감 등록 / “New!” 연출 / 레시피 체크 / 사운드
    }

    // ─ 확률 로직 동일 ─
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
            Debug.Log("골드 부족");
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
