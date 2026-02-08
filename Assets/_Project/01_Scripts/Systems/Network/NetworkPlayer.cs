using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 네트워크 플레이어를 나타내는 클래스
/// 각 클라이언트마다 하나씩 생성되며, 플레이어별 게임 데이터를 관리
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("플레이어 정보")]
    [SerializeField] private string playerName = "Player";

    // 플레이어 인덱스 (0 또는 1, 서버에서 접속 순서대로 할당)
    private NetworkVariable<int> playerIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>이 플레이어의 보드/경로/필드 인덱스 (0 또는 1)</summary>
    public int PlayerIndex => playerIndex.Value;

    // 네트워크 동기화 변수들
    private NetworkVariable<int> gold = new NetworkVariable<int>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<int> essence = new NetworkVariable<int>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<int> gem = new NetworkVariable<int>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<int> level = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<int> currentExp = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 플레이어 ID (NetworkObject의 OwnerClientId)
    public ulong ClientId => OwnerClientId;
    public string PlayerName => playerName;

    // 프로퍼티로 데이터 접근
    public int Gold => gold.Value;
    public int Essence => essence.Value;
    public int Gem => gem.Value;
    public int Level => level.Value;
    public int CurrentExp => currentExp.Value;

    // 이벤트
    public event Action<int> OnGoldChanged;
    public event Action<int> OnEssenceChanged;
    public event Action<int> OnGemChanged;
    public event Action<int> OnLevelChanged;
    public event Action<int> OnExpChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 서버에서 초기값 설정
        if (IsServer)
        {
            InitializePlayerData();
        }

        // 네트워크 변수 변경 이벤트 구독
        gold.OnValueChanged += (oldValue, newValue) => OnGoldChanged?.Invoke(newValue);
        essence.OnValueChanged += (oldValue, newValue) => OnEssenceChanged?.Invoke(newValue);
        gem.OnValueChanged += (oldValue, newValue) => OnGemChanged?.Invoke(newValue);
        level.OnValueChanged += (oldValue, newValue) => OnLevelChanged?.Invoke(newValue);
        currentExp.OnValueChanged += (oldValue, newValue) => OnExpChanged?.Invoke(newValue);

        // NetworkGameManager에 플레이어 등록
        if (IsServer && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RegisterPlayer(OwnerClientId, this);
        }

        Debug.Log($"[NetworkPlayer] 플레이어 스폰: {OwnerClientId} ({playerName})");
    }

    public override void OnNetworkDespawn()
    {
        // NetworkGameManager에서 플레이어 제거
        if (IsServer && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.UnregisterPlayer(OwnerClientId);
        }

        // 이벤트 구독 해제
        gold.OnValueChanged -= (oldValue, newValue) => OnGoldChanged?.Invoke(newValue);
        essence.OnValueChanged -= (oldValue, newValue) => OnEssenceChanged?.Invoke(newValue);
        gem.OnValueChanged -= (oldValue, newValue) => OnGemChanged?.Invoke(newValue);
        level.OnValueChanged -= (oldValue, newValue) => OnLevelChanged?.Invoke(newValue);
        currentExp.OnValueChanged -= (oldValue, newValue) => OnExpChanged?.Invoke(newValue);

        base.OnNetworkDespawn();
    }

    /// <summary>
    /// 서버에서 플레이어 데이터 초기화
    /// </summary>
    private void InitializePlayerData()
    {
        gold.Value = 100;
        essence.Value = 0;
        gem.Value = 0;
        level.Value = 1;
        currentExp.Value = 0;

        // 접속 순서에 따라 playerIndex 할당 (0, 1)
        if (NetworkGameManager.Instance != null)
            playerIndex.Value = NetworkGameManager.Instance.GetConnectedPlayerCount();
        else
            playerIndex.Value = 0;

        Debug.Log($"[NetworkPlayer] 플레이어 데이터 초기화: ClientId={OwnerClientId}, PlayerIndex={playerIndex.Value}");
    }

    #region 통화 관리 (서버 RPC)

    /// <summary>
    /// 골드 추가 (서버에서만 호출 가능)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddGoldServerRpc(int amount)
    {
        if (!IsServer) return;

        gold.Value += amount;
        Debug.Log($"[NetworkPlayer] 골드 추가: {OwnerClientId} +{amount} (현재: {gold.Value})");
    }

    /// <summary>
    /// 골드 소비 (서버에서만 호출 가능)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void SpendGoldServerRpc(int amount)
    {
        if (!IsServer) return;

        // RpcInvokePermission.Owner로 이미 소유자만 호출 가능하므로 검증 불필요

        if (gold.Value >= amount)
        {
            gold.Value -= amount;
            Debug.Log($"[NetworkPlayer] 골드 소비: {OwnerClientId} -{amount} (현재: {gold.Value})");
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] 골드 부족: {OwnerClientId} (필요: {amount}, 보유: {gold.Value})");
        }
    }

    /// <summary>
    /// 에센스 추가 (서버에서만 호출 가능)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddEssenceServerRpc(int amount)
    {
        if (!IsServer) return;

        essence.Value += amount;
        Debug.Log($"[NetworkPlayer] 에센스 추가: {OwnerClientId} +{amount} (현재: {essence.Value})");
    }

    /// <summary>
    /// 에센스 소비 (서버에서만 호출 가능)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void SpendEssenceServerRpc(int amount)
    {
        if (!IsServer) return;

        // RpcInvokePermission.Owner로 이미 소유자만 호출 가능하므로 검증 불필요

        if (essence.Value >= amount)
        {
            essence.Value -= amount;
            Debug.Log($"[NetworkPlayer] 에센스 소비: {OwnerClientId} -{amount} (현재: {essence.Value})");
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] 에센스 부족: {OwnerClientId} (필요: {amount}, 보유: {essence.Value})");
        }
    }

    /// <summary>
    /// 골드 소비 가능 여부 확인 (클라이언트에서 호출)
    /// </summary>
    public bool CanSpendGold(int amount)
    {
        return gold.Value >= amount;
    }

    /// <summary>
    /// 에센스 소비 가능 여부 확인 (클라이언트에서 호출)
    /// </summary>
    public bool CanSpendEssence(int amount)
    {
        return essence.Value >= amount;
    }

    #endregion

    #region 레벨 관리 (서버 RPC)

    /// <summary>
    /// 경험치 추가 (서버에서만 호출 가능)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddExpServerRpc(int amount)
    {
        if (!IsServer) return;

        currentExp.Value += amount;
        Debug.Log($"[NetworkPlayer] 경험치 추가: {OwnerClientId} +{amount} (현재: {currentExp.Value})");

        // 레벨업 체크 (간단한 구현, 나중에 확장 가능)
        CheckLevelUp();
    }

    /// <summary>
    /// 레벨업 체크 (서버에서만 실행)
    /// </summary>
    private void CheckLevelUp()
    {
        // TODO: PlayerLevelManager의 expThresholds를 참조하도록 확장 필요
        // 지금은 간단하게 레벨 10까지, 레벨당 4 경험치 필요로 가정
        const int expPerLevel = 4;
        const int maxLevel = 10;

        int newLevel = Mathf.Min(maxLevel, 1 + (currentExp.Value / expPerLevel));
        
        if (newLevel > level.Value)
        {
            level.Value = newLevel;
            Debug.Log($"[NetworkPlayer] 레벨업: {OwnerClientId} -> Level {level.Value}");
        }
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 플레이어 이름 설정
    /// </summary>
    public void SetPlayerName(string name)
    {
        playerName = name;
    }

    /// <summary>
    /// 로컬 플레이어인지 확인
    /// </summary>
    public new bool IsLocalPlayer()
    {
        return IsOwner;
    }

    #endregion
}

