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

    [Header("도박(정수 / Essence)")]
    public int lowGambleEssenceCost = 1;   // 하급 도박: 1 정수
    public int highGambleEssenceCost = 5;  // 고급 도박: 5 정수

    [Header("도박 성공 확률 (0~1)")]
    [Range(0f, 1f)] public float lowGambleSuccessRate = 0.6f;   // 하급 성공 확률 (예시)
    [Range(0f, 1f)] public float highGambleSuccessRate = 0.35f; // 고급 성공 확률 (예시)

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

    public event System.Action<bool> OnGambleLowResult;   // true=성공, false=실패
    public event System.Action<bool> OnGambleHighResult;

    private SummonManager _summon;

    void Awake() 
    { 
        _summon = SummonManager.Instance; 
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
    // === 도박 진입: 하급(1~3코스트) ===
    public void GambleLow()
    {
        var gm = GridManager.Instance;
        if (!gm.HasPlaceableCell())
        {
            Debug.Log("배치할 칸이 없습니다.");
            OnGambleLowResult?.Invoke(false);
            return;
        }

        if (!CurrencyManager.Instance.SpendEssence(lowGambleEssenceCost))
        {
            Debug.Log("정수가 부족합니다.");
            OnGambleLowResult?.Invoke(false);
            return;
        }

        // 성공/실패만 판정
        if (!RollSuccess(lowGambleSuccessRate))
        {
            Debug.Log("[GambleLow] 실패 (정수 소모, 보상 없음)");
            OnGambleLowResult?.Invoke(false);
            return;
        }

        // 성공 → 실제 소환은 SummonManager가 담당 (1~3코)
        bool ok = SummonManager.Instance.TrySummonFree_ByCostRange_AutoPlace(1, 3);
        if (!ok)
        {
            // 배치/풀 문제 등으로 소환 실패 시 환불(선택)
            CurrencyManager.Instance.AddEssence(lowGambleEssenceCost);
            Debug.LogWarning("[GambleLow] 소환 실패 → 정수 환불");
            OnGambleLowResult?.Invoke(false);
            return;
        }

        Debug.Log("[GambleLow] 성공! 유닛 소환 완료");
        OnGambleLowResult?.Invoke(true);
    }
    // === 도박 진입: 고급(4~5코스트) ===
    public void GambleHigh()
    {
        var gm = GridManager.Instance;
        if (!gm.HasPlaceableCell())
        {
            Debug.Log("배치할 칸이 없습니다.");
            OnGambleHighResult?.Invoke(false);
            return;
        }

        if (!CurrencyManager.Instance.SpendEssence(highGambleEssenceCost))
        {
            Debug.Log("정수가 부족합니다.");
            OnGambleHighResult?.Invoke(false);
            return;
        }

        // 성공/실패만 판정
        if (!RollSuccess(highGambleSuccessRate))
        {
            Debug.Log("[GambleHigh] 실패 (정수 소모, 보상 없음)");
            OnGambleHighResult?.Invoke(false);
            return;
        }

        // 성공 → 실제 소환은 SummonManager가 담당 (4~5코)
        bool ok = SummonManager.Instance.TrySummonFree_ByCostRange_AutoPlace(4, 5);
        if (!ok)
        {
            CurrencyManager.Instance.AddEssence(highGambleEssenceCost);
            Debug.LogWarning("[GambleHigh] 소환 실패 → 정수 환불");
            OnGambleHighResult?.Invoke(false);
            return;
        }

        Debug.Log("[GambleHigh] 성공! 유닛 소환 완료");
        OnGambleHighResult?.Invoke(true);
    }
    // === 성공 판정 ===
    private bool RollSuccess(float successRate01)
    {
        float r = Random.value; // [0,1)
        return r <= Mathf.Clamp01(successRate01);
    }

    // === 코스트 범위에서 "현재 레벨 확률표"를 범위 내로 재정규화해서 코스트 선택 후 유닛 픽 ===
    private UnitData PickUnitByCostRangeWeighted(int minCost, int maxCost)
    {
        // 1) 현재 레벨 확률(길이 5)을 가져오고 범위 밖은 0으로 만든 뒤 재정규화
        float[] probs = GetCurrentLevelProbabilities(); // 정규화 보장(합=1) 또는 폴백 생성
        if (probs == null || probs.Length != 5)
        {
            // 테이블이 없다면 균등 확률로 범위 내 코스트 선택
            int costUniform = PickCostUniformInRange(minCost, maxCost);
            return PickOneOfCost(costUniform) ?? PickUnitByCostRange(minCost, maxCost);
        }

        float sum = 0f;
        for (int i = 0; i < probs.Length; i++)
        {
            int cost = i + 1;
            if (cost < minCost || cost > maxCost) probs[i] = 0f;
            sum += probs[i];
        }

        if (sum <= 0f)
        {
            // 범위 내 가중치가 모두 0이면 범위 내 균등
            int costUniform = PickCostUniformInRange(minCost, maxCost);
            return PickOneOfCost(costUniform) ?? PickUnitByCostRange(minCost, maxCost);
        }

        // 재정규화
        for (int i = 0; i < probs.Length; i++) probs[i] /= sum;

        // 2) 코스트 선택
        int chosenCost = PickCostByWeight(probs);

        // 3) 해당 코스트 유닛 중 1개 선택 (없으면 범위 전체 폴백)
        return PickOneOfCost(chosenCost) ?? PickUnitByCostRange(minCost, maxCost);
    }

    private int PickCostUniformInRange(int minCost, int maxCost)
    {
        minCost = Mathf.Clamp(minCost, 1, 5);
        maxCost = Mathf.Clamp(maxCost, 1, 5);
        if (minCost > maxCost) (minCost, maxCost) = (maxCost, minCost);
        return Random.Range(minCost, maxCost + 1); // max 포함
    }

    // 현재 레벨 확률 테이블에서 길이 5의 확률 벡터를 가져옴(합=1).
    // 없으면 균등 확률 반환.
    private float[] GetCurrentLevelProbabilities()
    {
        if (probabilityTables != null && probabilityTables.Count > 0)
        {
            int level = PlayerLevelManager.Instance.Level;
            int idx = Mathf.Clamp(level - 1, 0, probabilityTables.Count - 1);
            var table = probabilityTables[idx];
            table.Normalize();
            var p = table.GetProbabilities(); // 길이 5
            if (p != null && p.Length == 5) return p;
        }
        // 폴백: 균등
        return new float[] { 0.2f, 0.2f, 0.2f, 0.2f, 0.2f };
    }

    private UnitData PickUnitByCostRange(int minCost, int maxCost)
    {
        if (allUnitDatas == null || allUnitDatas.Count == 0) return null;

        // 1) 범위 내 우선
        var pool = allUnitDatas
            .Where(u => u != null && u.cost >= minCost && u.cost <= maxCost)
            .ToList();

        if (pool.Count > 0)
            return pool[Random.Range(0, pool.Count)];

        // 2) 폴백: 전체에서 아무거나
        var all = allUnitDatas.Where(u => u != null).ToList();
        if (all.Count > 0)
            return all[Random.Range(0, all.Count)];

        return null;
    }
}
