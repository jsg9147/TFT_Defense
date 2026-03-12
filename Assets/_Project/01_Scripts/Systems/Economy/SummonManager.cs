using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

/// <summary>
/// 유닛 소환 시스템 관리자
/// 골드/에센스를 사용하여 유닛을 소환하고, 네트워크/싱글 플레이 모드를 자동으로 지원합니다.
/// </summary>
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

    [Header("네트워크 동기화")]
    [SerializeField] private UnitDataRegistry unitDataRegistry;

    void Awake() => Instance = this;

    void Start()
    {
        unitDataRegistry?.Initialize();
        UpdateProbabilityUI();
    }

    #region 네트워크 유틸리티

    /// <summary>
    /// 로컬 플레이어의 NetworkPlayer 컴포넌트를 가져옵니다.
    /// </summary>
    /// <returns>로컬 플레이어의 NetworkPlayer, 없으면 null</returns>
    private NetworkPlayer GetLocalNetworkPlayer()
    {
        if (NetworkManager.Singleton == null) return null;
        
        var playerObj = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
        if (playerObj == null) return null;
        
        return playerObj.GetComponent<NetworkPlayer>();
    }

    /// <summary>
    /// 현재 네트워크 모드인지 확인합니다.
    /// </summary>
    /// <returns>네트워크 모드면 true, 싱글 플레이면 false</returns>
    private bool IsNetworkMode()
    {
        return NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsNetworkMode();
    }

    #endregion

    #region 통화 관리 (네트워크/싱글 플레이 자동 분기)

    /// <summary>
    /// 골드를 차감합니다. 네트워크 모드에서는 NetworkPlayer의 ServerRPC를 사용하고,
    /// 싱글 플레이에서는 CurrencyManager를 사용합니다.
    /// </summary>
    /// <param name="amount">차감할 골드 양</param>
    /// <returns>차감 성공 여부</returns>
    private bool TrySpendGold(int amount)
    {
        if (IsNetworkMode())
        {
            var player = GetLocalNetworkPlayer();
            if (player == null)
            {
                Debug.LogWarning("[Summon] 네트워크 플레이어를 찾을 수 없습니다.");
                return false;
            }
            
            if (!player.CanSpendGold(amount))
            {
                return false;
            }
            
            player.SpendGoldServerRpc(amount);
            return true;
        }
        else
        {
            // 싱글 플레이: 기존 CurrencyManager 사용
            if (CurrencyManager.Instance == null)
            {
                Debug.LogWarning("[Summon] CurrencyManager를 찾을 수 없습니다.");
                return false;
            }
            
            return CurrencyManager.Instance.SpendGold(amount);
        }
    }

    /// <summary>
    /// 골드를 추가합니다. 네트워크 모드에서는 NetworkPlayer의 ServerRPC를 사용하고,
    /// 싱글 플레이에서는 CurrencyManager를 사용합니다.
    /// </summary>
    /// <param name="amount">추가할 골드 양</param>
    private void AddGold(int amount)
    {
        if (IsNetworkMode())
        {
            var player = GetLocalNetworkPlayer();
            if (player == null)
            {
                Debug.LogWarning("[Summon] 네트워크 플레이어를 찾을 수 없습니다.");
                return;
            }
            
            player.AddGoldServerRpc(amount);
        }
        else
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddGold(amount);
            }
        }
    }

    /// <summary>
    /// 에센스를 차감합니다. 네트워크 모드에서는 NetworkPlayer의 ServerRPC를 사용하고,
    /// 싱글 플레이에서는 CurrencyManager를 사용합니다.
    /// </summary>
    /// <param name="amount">차감할 에센스 양</param>
    /// <returns>차감 성공 여부</returns>
    private bool TrySpendEssence(int amount)
    {
        if (IsNetworkMode())
        {
            var player = GetLocalNetworkPlayer();
            if (player == null)
            {
                Debug.LogWarning("[Summon] 네트워크 플레이어를 찾을 수 없습니다.");
                return false;
            }
            
            if (!player.CanSpendEssence(amount))
            {
                return false;
            }
            
            player.SpendEssenceServerRpc(amount);
            return true;
        }
        else
        {
            // 싱글 플레이: 기존 CurrencyManager 사용
            if (CurrencyManager.Instance == null)
            {
                Debug.LogWarning("[Summon] CurrencyManager를 찾을 수 없습니다.");
                return false;
            }
            
            return CurrencyManager.Instance.SpendEssence(amount);
        }
    }

    /// <summary>
    /// 에센스를 추가합니다. 네트워크 모드에서는 NetworkPlayer의 ServerRPC를 사용하고,
    /// 싱글 플레이에서는 CurrencyManager를 사용합니다.
    /// </summary>
    /// <param name="amount">추가할 에센스 양</param>
    private void AddEssence(int amount)
    {
        if (IsNetworkMode())
        {
            var player = GetLocalNetworkPlayer();
            if (player == null)
            {
                Debug.LogWarning("[Summon] 네트워크 플레이어를 찾을 수 없습니다.");
                return;
            }
            
            player.AddEssenceServerRpc(amount);
        }
        else
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddEssence(amount);
            }
        }
    }

    /// <summary>
    /// 현재 플레이어 레벨을 가져옵니다. 네트워크 모드에서는 NetworkPlayer를,
    /// 싱글 플레이에서는 PlayerLevelManager를 사용합니다.
    /// </summary>
    /// <returns>플레이어 레벨 (기본값: 1)</returns>
    private int GetPlayerLevel()
    {
        if (IsNetworkMode())
        {
            var player = GetLocalNetworkPlayer();
            if (player != null)
            {
                return player.Level;
            }
        }
        
        // 싱글 플레이 또는 네트워크 플레이어를 찾을 수 없을 때
        if (PlayerLevelManager.Instance != null)
        {
            return PlayerLevelManager.Instance.Level;
        }
        
        return 1; // 기본값
    }

    #endregion

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

        // 2) 비용 차감 (네트워크/싱글 플레이 자동 분기)
        if (!TrySpendGold(summonCost))
        {
            Debug.Log("골드 부족");
            return false;
        }

        // 3) 유닛 결정
        UnitData pick = RollOneUnit();
        if (pick == null)
        {
            Debug.LogWarning("유닛 풀 비어있음");
            AddGold(summonCost); // 환불
            return false;
        }

        // 4) 즉시 소환
        if (TrySpawnUnitAtFirstFreeCell(pick, out var spawned))
        {
            OnSummonSuccess(pick);
            return true;
        }

        // 실패 시 환불
        AddGold(summonCost);
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

        int level = GetPlayerLevel();
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
        if (!TrySpendGold(expCost))
        {
            Debug.Log("골드 부족");
            return;
        }

        // 경험치 추가도 네트워크 모드면 NetworkPlayer 사용
        if (IsNetworkMode())
        {
            var player = GetLocalNetworkPlayer();
            if (player != null)
            {
                player.AddExpServerRpc(expPerBuy);
            }
        }
        else
        {
            PlayerLevelManager.Instance.AddExp(expPerBuy);
        }
        
        UpdateProbabilityUI();
    }

    void UpdateProbabilityUI()
    {
        if (probabilityUI == null || probabilityTables == null || probabilityTables.Count == 0)
            return;

        int level = GetPlayerLevel();
        int idx = Mathf.Clamp(level - 1, 0, probabilityTables.Count - 1);

        var table = probabilityTables[idx];
        table.Normalize();
        probabilityUI.UpdateRates(table.GetProbabilities());
    }

    bool TrySummonOnePremium_AutoPlace()
    {
        var gm = GridManager.Instance;
        if (gm == null || !gm.HasPlaceableCell()) return false;

        if (!TrySpendEssence(premiumSummonEssenceCost))
        {
            Debug.Log("에센스 부족");
            return false;
        }

        UnitData pick = RollOneUnit(); // (필요하면 여기서 4~5코스트로 제한하는 Roll 구현으로 교체)

        if (TrySpawnUnitAtFirstFreeCell(pick, out var spawned))
        {
            OnSummonSuccess(pick);
            return true;
        }

        // 실패 시 환불
        AddEssence(premiumSummonEssenceCost);
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

        // 멀티플레이: 서버 RPC로 소환 위임
        if (IsNetworkMode())
        {
            var player = GetLocalNetworkPlayer();
            if (player == null)
            {
                Debug.LogWarning("[Spawn] 네트워크 플레이어를 찾을 수 없습니다.");
                return false;
            }

            int idx = UnitDataRegistry.Instance != null ? UnitDataRegistry.Instance.GetIndex(data) : -1;
            if (idx < 0)
            {
                Debug.LogWarning($"[Spawn] UnitDataRegistry에 '{data.unitName}' 없음. 레지스트리를 확인하세요.");
                return false;
            }

            player.RequestSummonUnitServerRpc(idx, summonCost);
            spawned = null;
            return true; // 낙관적 반환, 실패 시 서버가 골드 환불
        }

        // 싱글플레이: 로컬 직접 생성
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
            int level = GetPlayerLevel();
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

