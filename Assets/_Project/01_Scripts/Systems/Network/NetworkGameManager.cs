using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 네트워크 게임의 중앙 관리자
/// Unity Netcode for GameObjects의 NetworkManager를 래핑하여 게임별 기능 제공
/// </summary>
public class NetworkGameManager : MonoSingleton<NetworkGameManager>
{
    [Header("네트워크 설정")]
    [SerializeField] private ushort port = 7777;
    [SerializeField] private int maxPlayers = 4;
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;

    // 네트워크 이벤트
    public event Action<ulong> OnClientConnected;      // 클라이언트 연결 시
    public event Action<ulong> OnClientDisconnected;   // 클라이언트 연결 해제 시
    public event Action OnServerStarted;               // 서버 시작 시
    public event Action OnServerStopped;               // 서버 종료 시
    public event Action OnClientStarted;                // 클라이언트 시작 시
    public event Action OnClientStopped;               // 클라이언트 종료 시

    // 플레이어 관리
    private readonly Dictionary<ulong, NetworkPlayer> connectedPlayers = new();
    public IReadOnlyDictionary<ulong, NetworkPlayer> ConnectedPlayers => connectedPlayers;

    // 네트워크 상태
    public bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    public bool IsClient => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;
    public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
    public bool IsConnected => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;

    protected override void Awake()
    {
        base.Awake();
        
        // NetworkManager가 없으면 경고
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[NetworkGameManager] NetworkManager가 씬에 없습니다. NetworkManager 프리팹을 씬에 추가하세요.");
        }
    }

    private void Start()
    {
        // NetworkManager 이벤트 구독
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.OnClientStarted += HandleClientStarted;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnClientStarted -= HandleClientStarted;
        }
    }

    #region 서버 관리

    /// <summary>
    /// 호스트로 게임 시작 (서버 + 클라이언트)
    /// </summary>
    public bool StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkGameManager] NetworkManager가 없습니다.");
            return false;
        }

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("[NetworkGameManager] 이미 네트워크가 시작되어 있습니다.");
            return false;
        }

        bool result = NetworkManager.Singleton.StartHost();
        
        if (result && enableDebugLogs)
        {
            Debug.Log("[NetworkGameManager] 호스트로 게임 시작");
        }

        return result;
    }

    /// <summary>
    /// 서버만 시작 (전용 서버)
    /// </summary>
    public bool StartServer()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkGameManager] NetworkManager가 없습니다.");
            return false;
        }

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("[NetworkGameManager] 이미 네트워크가 시작되어 있습니다.");
            return false;
        }

        bool result = NetworkManager.Singleton.StartServer();
        
        if (result && enableDebugLogs)
        {
            Debug.Log("[NetworkGameManager] 서버 시작");
        }

        return result;
    }

    /// <summary>
    /// 클라이언트로 연결
    /// </summary>
    public bool StartClient(string ipAddress = "127.0.0.1", ushort port = 7777)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkGameManager] NetworkManager가 없습니다.");
            return false;
        }

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("[NetworkGameManager] 이미 네트워크가 시작되어 있습니다.");
            return false;
        }

        // Unity Transport 설정
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(ipAddress, port);
        }

        bool result = NetworkManager.Singleton.StartClient();
        
        if (result && enableDebugLogs)
        {
            Debug.Log($"[NetworkGameManager] 클라이언트로 연결 시도: {ipAddress}:{port}");
        }

        return result;
    }

    /// <summary>
    /// 네트워크 종료
    /// </summary>
    public void Shutdown()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            
            if (enableDebugLogs)
            {
                Debug.Log("[NetworkGameManager] 네트워크 종료");
            }

            // 플레이어 목록 초기화
            connectedPlayers.Clear();
        }
    }

    #endregion

    #region 이벤트 핸들러

    private void HandleServerStarted()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[NetworkGameManager] 서버가 시작되었습니다.");
        }

        OnServerStarted?.Invoke();
    }

    private void HandleClientStarted()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[NetworkGameManager] 클라이언트가 시작되었습니다.");
        }

        OnClientStarted?.Invoke();
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[NetworkGameManager] 클라이언트 연결: {clientId}");
        }

        // 서버에서만 플레이어 등록
        if (IsServer)
        {
            // NetworkPlayer는 나중에 생성될 예정 (Step 1-2)
            // 지금은 연결만 추적
        }

        OnClientConnected?.Invoke(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[NetworkGameManager] 클라이언트 연결 해제: {clientId}");
        }

        // 플레이어 목록에서 제거
        if (connectedPlayers.ContainsKey(clientId))
        {
            connectedPlayers.Remove(clientId);
        }

        OnClientDisconnected?.Invoke(clientId);
    }

    #endregion

    #region 플레이어 관리

    /// <summary>
    /// 플레이어 등록 (서버에서만 호출)
    /// </summary>
    public void RegisterPlayer(ulong clientId, NetworkPlayer player)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[NetworkGameManager] 플레이어 등록은 서버에서만 가능합니다.");
            return;
        }

        if (connectedPlayers.ContainsKey(clientId))
        {
            Debug.LogWarning($"[NetworkGameManager] 플레이어 {clientId}는 이미 등록되어 있습니다.");
            return;
        }

        connectedPlayers[clientId] = player;

        if (enableDebugLogs)
        {
            Debug.Log($"[NetworkGameManager] 플레이어 등록: {clientId}");
        }
    }

    /// <summary>
    /// 플레이어 제거 (서버에서만 호출)
    /// </summary>
    public void UnregisterPlayer(ulong clientId)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[NetworkGameManager] 플레이어 제거는 서버에서만 가능합니다.");
            return;
        }

        if (connectedPlayers.Remove(clientId) && enableDebugLogs)
        {
            Debug.Log($"[NetworkGameManager] 플레이어 제거: {clientId}");
        }
    }

    /// <summary>
    /// 플레이어 가져오기
    /// </summary>
    public NetworkPlayer GetPlayer(ulong clientId)
    {
        connectedPlayers.TryGetValue(clientId, out NetworkPlayer player);
        return player;
    }

    /// <summary>
    /// 현재 플레이어 ID 가져오기 (클라이언트에서만 유효)
    /// </summary>
    public ulong GetLocalClientId()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            return NetworkManager.Singleton.LocalClientId;
        }
        return 0;
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 네트워크 모드인지 확인 (싱글 플레이 vs 멀티 플레이)
    /// </summary>
    public bool IsNetworkMode()
    {
        return NetworkManager.Singleton != null && 
               (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient);
    }

    /// <summary>
    /// 연결된 플레이어 수
    /// </summary>
    public int GetConnectedPlayerCount()
    {
        return connectedPlayers.Count;
    }

    #endregion
}

