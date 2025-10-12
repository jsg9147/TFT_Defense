using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    [Header("상점 설정")]
    public Transform slotParent;                 // 슬롯들을 담을 부모 오브젝트
    public List<UnitData> allUnitDatas;          // 등장 가능한 유닛 목록

    [Header("상점 비용")]
    public int unitCost = 2;

    [Header("경험치 구매")]
    public int expCost = 4;    // 골드 소모량
    public int expPerBuy = 4;  // 구매 시 획득 경험치

    [Header("확률 UI")]
    public ShopProbabilityUI probabilityUI;
    public List<ShopProbabilityTable> probabilityTables;

    [Header("필터 UI(선택)")]
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
    /// 상점 유닛 슬롯 새로 고침 (리롤 포함)
    /// </summary>
    public void RefreshShop()
    {
        ClearCurrentSlots();
        UpdateProbabilityUI();

        List<UnitData> selected = GetRandomUnitListWeighted(5);  // 가중치 기반으로 변경

        foreach (var data in selected)
        {
            ShopSlotUI slot = UIManager.Instance.ShopSlotPool.GetSlot(slotParent);
            slot.Init(data, OnBuyUnit);
            currentSlots.Add(slot);
        }
    }
    // 레벨 확률 기반 선택
    List<UnitData> GetRandomUnitListWeighted(int count)
    {
        List<UnitData> result = new List<UnitData>();

        if (allUnitDatas == null || allUnitDatas.Count == 0)
        {
            Debug.LogWarning("유닛 데이터가 없습니다.");
            return result;
        }

        int level = PlayerLevelManager.Instance.Level;
        int idx = Mathf.Clamp(level - 1, 0, probabilityTables.Count - 1); // ← 1-기반 레벨 보정
        if (probabilityTables == null || probabilityTables.Count == 0)
        {
            Debug.LogWarning("확률 테이블이 없습니다. 균등 무작위로 뽑습니다.");
            return GetRandomUnitList_Fallback(count);
        }

        var table = probabilityTables[idx];
        table.Normalize();
        float[] probs = table.GetProbabilities(); // 길이 5, 합=1

        for (int i = 0; i < count; i++)
        {
            int targetCost = PickCostByWeight(probs); // 1~5
            UnitData pick = PickUnitByCostWithFallback(targetCost);
            if (pick != null) result.Add(pick);
        }

        return result;
    }

    // 완전 기존 방식(균등) 폴백
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

    int PickCostByWeight(float[] probs) // probs 길이 5, 합=1
    {
        float roll = Random.value; // 0~1
        float acc = 0f;
        for (int i = 0; i < probs.Length; i++)
        {
            acc += probs[i];
            if (roll <= acc) return i + 1; // 코스트=인덱스+1
        }
        return 1;
    }

    UnitData PickUnitByCostWithFallback(int cost)
    {
        // 1) 목표 코스트
        var candidate = PickOneOfCost(cost);
        if (candidate != null) return candidate;

        // 2) 위로 폴백
        for (int c = cost + 1; c <= 5; c++)
        {
            candidate = PickOneOfCost(c);
            if (candidate != null) return candidate;
        }
        // 3) 아래로 폴백
        for (int c = cost - 1; c >= 1; c--)
        {
            candidate = PickOneOfCost(c);
            if (candidate != null) return candidate;
        }
        return null;
    }

    UnitData PickOneOfCost(int cost)
    {
        // UnitData에 cost 필드가 이미 있음 (네 코드 기준)
        var list = allUnitDatas.Where(u => u != null && u.cost == cost).ToList();
        if (list.Count == 0) return null;
        int r = Random.Range(0, list.Count);
        return list[r];
    }

    // 인덱싱 보정 & 항상 정규화 후 UI 업데이트
    void UpdateProbabilityUI()
    {
        if (probabilityUI == null || probabilityTables == null || probabilityTables.Count == 0)
        {
            Debug.LogWarning("확률 UI 또는 확률 테이블이 설정되지 않았습니다.");
            return;
        }

        int level = PlayerLevelManager.Instance.Level;
        int idx = level - 1; // 1-기반 → 0-기반
        if (idx < 0 || idx >= probabilityTables.Count)
        {
            Debug.LogWarning($"현재 플레이어 레벨({level})에 해당하는 확률 테이블이 없습니다.");
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
                UIManager.Instance.ShopSlotPool.ReturnSlot(slot); // 풀로 반환
        }
        currentSlots.Clear();
    }


    /// <summary>
    /// 유닛 구매 시 실행
    /// </summary>
    void OnBuyUnit(UnitData data)
    {
        // CurrencyManager에서 골드 체크
        if (!CurrencyManager.Instance.SpendGold(unitCost))
        {
            Debug.Log("골드 부족");
            return;
        }

        bool success = GameManager.Instance.benchManager.TryAddHero(data);
        if (!success)
        {
            Debug.Log("유닛 구매 실패: 벤치가 가득 찼습니다.");

            // 구매 실패 시 골드 환불
            CurrencyManager.Instance.AddGold(unitCost);
            return;
        }

        Debug.Log($"{data.unitName} 구매됨");
    }

    /// <summary>
    /// 유닛 SO에서 5개 무작위 추출
    /// </summary>
    List<UnitData> GetRandomUnitList(int count)
    {
        List<UnitData> result = new List<UnitData>();

        if (allUnitDatas == null || allUnitDatas.Count == 0)
        {
            Debug.LogWarning("유닛 데이터가 없습니다.");
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
            Debug.LogWarning("현재 플레이어 레벨에 해당하는 확률 테이블이 없습니다. Level :" + PlayerLevelManager.Instance.Level);
            return;
        }

        if (!CurrencyManager.Instance.SpendGold(expCost))
        {
            Debug.Log("골드 부족");
            return;
        }

        PlayerLevelManager.Instance.AddExp(expPerBuy);
        UpdateProbabilityUI();
    }
}
