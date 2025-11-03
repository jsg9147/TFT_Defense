using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance;

    [Header("소환 설정")]
    [Tooltip("소환 시 소모되는 골드")]
    public int summonCost = 2;

    [Header("프리미엄 소환")]
    public int premiumSummonEssenceCost = 1;  // 정수 기반 비용

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
    public void SummonPremiumOnce() => SummonPremiumMultiple(1);
    public void SummonMultiple(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!TrySummonOne_AutoPlace()) break;
        }
    }
    public void SummonPremiumMultiple(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!TrySummonOnePremium_AutoPlace()) break;
        }
    }
    /// <summary>
    /// 자동 배치: 첫 번째 빈 칸을 찾아 즉시 생성/배치. 실패 시 환불.
    /// </summary>
    bool TrySummonOne_AutoPlace()
    {
        var gm = GridManager.Instance;

        // 1) 칸 확인
        if (gm == null || !gm.HasPlaceableCell())
        {
            Debug.Log("배치할 칸이 없습니다.");
            return false;
        }

        // 2) 비용 차감
        if (!CurrencyManager.Instance.SpendGold(summonCost))
        {
            Debug.Log("골드 부족");
            return false;
        }

        // 3) 유닛 결정
        UnitData pick = RollOneUnit();
        if (pick == null)
        {
            Debug.LogWarning("유닛 풀 비어있음");
            CurrencyManager.Instance.AddGold(summonCost); // 환불
            return false;
        }

        // 4) 즉시 소환
        if (TrySpawnUnitAtFirstFreeCell(pick, out var spawned))
        {
            OnSummonSuccess(pick);
            return true;
        }

        // 실패 시 환불
        CurrencyManager.Instance.AddGold(summonCost);
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

    bool TrySummonOnePremium_AutoPlace()
    {
        var gm = GridManager.Instance;
        if (gm == null || !gm.HasPlaceableCell()) return false;

        if (!CurrencyManager.Instance.SpendEssence(premiumSummonEssenceCost))
        {
            Debug.Log("정수 부족");
            return false;
        }

        UnitData pick = RollOneUnit(); // (필요하면 여기서 4~5코스트로 제한하는 Roll 구현으로 교체)

        if (TrySpawnUnitAtFirstFreeCell(pick, out var spawned))
        {
            OnSummonSuccess(pick);
            return true;
        }

        // 실패 시 환불
        CurrencyManager.Instance.AddEssence(premiumSummonEssenceCost);
        return false;
    }


    private bool TrySpawnUnitAtFirstFreeCell(UnitData data, out Unit spawned)
    {
        spawned = null;

        if (data == null || data.unitPrefab == null)
        {
            Debug.LogWarning("[Spawn] UnitData 또는 unitPrefab 누락");
            return false;
        }
        var gm = GridManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[Spawn] GridManager 없음");
            return false;
        }
        if (!gm.TryFindFirstPlaceable(out var cell))
        {
            Debug.Log("[Spawn] 배치할 칸 없음");
            return false;
        }

        // 1) 인스턴스 생성
        var go = Instantiate(data.unitPrefab);
        var unit = go.GetComponent<Unit>();
        if (unit == null) unit = go.AddComponent<Unit>();
        unit.Init(data);

        // 2) Grid 배치 (실패 시 정리)
        if (!gm.TryPlaceUnit(unit, cell))
        {
            Destroy(go);
            Debug.Log("[Spawn] 배치 실패");
            return false;
        }

        spawned = unit;
        return true;
    }

    /// <summary>
    /// 비용 차감 없이, 코스트 범위를 가중치로 재정규화해 1개 유닛을 자동 배치.
    /// 성공 시 true, 실패 시 false (칸 없거나 풀 없음 등)
    /// </summary>
    public bool TrySummonFree_ByCostRange_AutoPlace(int minCost, int maxCost)
    {
        var gm = GridManager.Instance;
        if (gm == null || !gm.HasPlaceableCell())
        {
            Debug.Log("[Summon] 칸 없음");
            return false;
        }

        UnitData pick = RollOneUnit_ByCostRangeWeighted(minCost, maxCost);
        if (pick == null)
        {
            // 풀 비어있을 때 폴백 (전체에서 균등)
            pick = PickUniform(allUnitDatas);
            if (pick == null)
            {
                Debug.LogWarning("[Summon] 풀 비어있음");
                return false;
            }
        }

        if (TrySpawnUnitAtFirstFreeCell(pick, out var spawned))
        {
            OnSummonSuccess(pick);
            return true;
        }
        return false;
    }

    // === 확률표를 범위 내로 재정규화한 뒤 코스트 픽 → 유닛 픽
    private UnitData RollOneUnit_ByCostRangeWeighted(int minCost, int maxCost)
    {
        if (allUnitDatas == null || allUnitDatas.Count == 0) return null;

        float[] probs = GetCurrentLevelProbabilities(); // 길이 5, 합=1 (없으면 균등)
        if (probs == null || probs.Length != 5)
            return PickUnitByCostRange(minCost, maxCost);

        // 범위 밖은 0으로 만들고 재정규화
        float sum = 0f;
        for (int i = 0; i < probs.Length; i++)
        {
            int cost = i + 1;
            if (cost < minCost || cost > maxCost) probs[i] = 0f;
            sum += probs[i];
        }
        if (sum <= 0f)
            return PickUnitByCostRange(minCost, maxCost);

        for (int i = 0; i < probs.Length; i++) probs[i] /= sum;

        int chosenCost = PickCostByWeight(probs);
        // 해당 코스트 없으면 범위 전체에서 폴백
        var list = allUnitDatas.Where(u => u != null && u.cost == chosenCost).ToList();
        if (list.Count == 0) return PickUnitByCostRange(minCost, maxCost);

        return list[Random.Range(0, list.Count)];
    }

    private float[] GetCurrentLevelProbabilities()
    {
        if (probabilityTables != null && probabilityTables.Count > 0)
        {
            int level = PlayerLevelManager.Instance.Level;
            int idx = Mathf.Clamp(level - 1, 0, probabilityTables.Count - 1);
            var table = probabilityTables[idx];
            table.Normalize();
            var p = table.GetProbabilities();
            if (p != null && p.Length == 5) return p;
        }
        // 폴백: 균등
        return new float[] { 0.2f, 0.2f, 0.2f, 0.2f, 0.2f };
    }

    private UnitData PickUnitByCostRange(int minCost, int maxCost)
    {
        if (allUnitDatas == null || allUnitDatas.Count == 0) return null;
        var pool = allUnitDatas.Where(u => u != null && u.cost >= minCost && u.cost <= maxCost).ToList();
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }
}

