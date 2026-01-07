using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// 네트워크 데이터 동기화 테스트용 스크립트
/// Step 5 테스트를 쉽게 수행할 수 있도록 도와주는 유틸리티
/// </summary>
public class NetworkDataSyncTest : MonoBehaviour
{
    [Header("테스트 UI")]
    [SerializeField] private Button addGoldButton;
    [SerializeField] private Button spendGoldButton;
    [SerializeField] private Button addExpButton;
    [SerializeField] private Button addEssenceButton;
    [SerializeField] private Button spendEssenceButton;
    
    [Header("상태 표시")]
    [SerializeField] private TextMeshProUGUI playerInfoText;
    [SerializeField] private TextMeshProUGUI testLogText;
    
    [Header("테스트 값")]
    [SerializeField] private int testGoldAmount = 50;
    [SerializeField] private int testSpendAmount = 30;
    [SerializeField] private int testExpAmount = 4;
    [SerializeField] private int testEssenceAmount = 5;

    private NetworkPlayer localPlayer;
    private string testLog = "";

    private void Start()
    {
        // 버튼 이벤트 연결
        if (addGoldButton != null)
            addGoldButton.onClick.AddListener(TestAddGold);
        
        if (spendGoldButton != null)
            spendGoldButton.onClick.AddListener(TestSpendGold);
        
        if (addExpButton != null)
            addExpButton.onClick.AddListener(TestAddExp);
        
        if (addEssenceButton != null)
            addEssenceButton.onClick.AddListener(TestAddEssence);
        
        if (spendEssenceButton != null)
            spendEssenceButton.onClick.AddListener(TestSpendEssence);
    }

    private void Update()
    {
        // 로컬 플레이어 찾기
        if (localPlayer == null && NetworkManager.Singleton != null)
        {
            var playerObj = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
            if (playerObj != null)
            {
                localPlayer = playerObj.GetComponent<NetworkPlayer>();
            }
        }

        // 플레이어 정보 업데이트
        UpdatePlayerInfo();
    }

    private void UpdatePlayerInfo()
    {
        if (playerInfoText == null) return;

        if (localPlayer != null)
        {
            playerInfoText.text = $"플레이어 ID: {localPlayer.ClientId}\n" +
                                 $"골드: {localPlayer.Gold}\n" +
                                 $"에센스: {localPlayer.Essence}\n" +
                                 $"레벨: {localPlayer.Level}\n" +
                                 $"경험치: {localPlayer.CurrentExp}";
        }
        else if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsServer)
        {
            // 서버 모드: 첫 번째 플레이어 정보 표시
            var players = NetworkGameManager.Instance.ConnectedPlayers;
            if (players.Count > 0)
            {
                var firstPlayer = players.Values.GetEnumerator();
                firstPlayer.MoveNext();
                var player = firstPlayer.Current;
                if (player != null)
                {
                    playerInfoText.text = $"서버 모드\n" +
                                         $"플레이어 ID: {player.ClientId}\n" +
                                         $"골드: {player.Gold}\n" +
                                         $"에센스: {player.Essence}\n" +
                                         $"레벨: {player.Level}\n" +
                                         $"경험치: {player.CurrentExp}";
                }
            }
            else
            {
                playerInfoText.text = "서버 모드\n연결된 플레이어 없음";
            }
        }
        else
        {
            playerInfoText.text = "네트워크 연결 안됨";
        }

        // 테스트 로그 업데이트
        if (testLogText != null)
        {
            testLogText.text = testLog;
        }
    }

    #region 테스트 메서드

    /// <summary>
    /// 테스트 5-1: 골드 추가 테스트
    /// </summary>
    private void TestAddGold()
    {
        if (localPlayer != null)
        {
            // 클라이언트에서 로컬 플레이어의 골드 추가
            localPlayer.AddGoldServerRpc(testGoldAmount);
            AddTestLog($"[테스트] 골드 추가 요청: +{testGoldAmount}");
        }
        else if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsServer)
        {
            // 서버에서 첫 번째 플레이어의 골드 추가
            var players = NetworkGameManager.Instance.ConnectedPlayers;
            if (players.Count > 0)
            {
                var firstPlayer = players.Values.GetEnumerator();
                firstPlayer.MoveNext();
                var player = firstPlayer.Current;
                if (player != null)
                {
                    player.AddGoldServerRpc(testGoldAmount);
                    AddTestLog($"[서버 테스트] 플레이어 {player.ClientId} 골드 추가: +{testGoldAmount}");
                }
            }
            else
            {
                AddTestLog("[에러] 연결된 플레이어가 없습니다.");
            }
        }
        else
        {
            AddTestLog("[에러] 네트워크가 연결되지 않았거나 플레이어를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 테스트 5-2: 골드 소비 테스트
    /// </summary>
    private void TestSpendGold()
    {
        if (localPlayer != null)
        {
            // 클라이언트에서 로컬 플레이어의 골드 소비
            if (localPlayer.CanSpendGold(testSpendAmount))
            {
                localPlayer.SpendGoldServerRpc(testSpendAmount);
                AddTestLog($"[테스트] 골드 소비 요청: -{testSpendAmount}");
            }
            else
            {
                AddTestLog($"[에러] 골드가 부족합니다. (보유: {localPlayer.Gold}, 필요: {testSpendAmount})");
            }
        }
        else if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsServer)
        {
            // 서버에서 첫 번째 플레이어의 골드 소비
            var players = NetworkGameManager.Instance.ConnectedPlayers;
            if (players.Count > 0)
            {
                var firstPlayer = players.Values.GetEnumerator();
                firstPlayer.MoveNext();
                var player = firstPlayer.Current;
                if (player != null)
                {
                    if (player.CanSpendGold(testSpendAmount))
                    {
                        player.SpendGoldServerRpc(testSpendAmount);
                        AddTestLog($"[서버 테스트] 플레이어 {player.ClientId} 골드 소비: -{testSpendAmount}");
                    }
                    else
                    {
                        AddTestLog($"[에러] 골드가 부족합니다. (보유: {player.Gold}, 필요: {testSpendAmount})");
                    }
                }
            }
            else
            {
                AddTestLog("[에러] 연결된 플레이어가 없습니다.");
            }
        }
        else
        {
            AddTestLog("[에러] 네트워크가 연결되지 않았거나 플레이어를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 테스트 5-3: 경험치 추가 테스트
    /// </summary>
    private void TestAddExp()
    {
        if (localPlayer != null)
        {
            // 클라이언트에서 로컬 플레이어의 경험치 추가
            localPlayer.AddExpServerRpc(testExpAmount);
            AddTestLog($"[테스트] 경험치 추가 요청: +{testExpAmount}");
        }
        else if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsServer)
        {
            // 서버에서 첫 번째 플레이어의 경험치 추가
            var players = NetworkGameManager.Instance.ConnectedPlayers;
            if (players.Count > 0)
            {
                var firstPlayer = players.Values.GetEnumerator();
                firstPlayer.MoveNext();
                var player = firstPlayer.Current;
                if (player != null)
                {
                    player.AddExpServerRpc(testExpAmount);
                    AddTestLog($"[서버 테스트] 플레이어 {player.ClientId} 경험치 추가: +{testExpAmount}");
                }
            }
            else
            {
                AddTestLog("[에러] 연결된 플레이어가 없습니다.");
            }
        }
        else
        {
            AddTestLog("[에러] 네트워크가 연결되지 않았거나 플레이어를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 에센스 추가 테스트
    /// </summary>
    private void TestAddEssence()
    {
        if (localPlayer != null)
        {
            localPlayer.AddEssenceServerRpc(testEssenceAmount);
            AddTestLog($"[테스트] 에센스 추가 요청: +{testEssenceAmount}");
        }
        else if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsServer)
        {
            var players = NetworkGameManager.Instance.ConnectedPlayers;
            if (players.Count > 0)
            {
                var firstPlayer = players.Values.GetEnumerator();
                firstPlayer.MoveNext();
                var player = firstPlayer.Current;
                if (player != null)
                {
                    player.AddEssenceServerRpc(testEssenceAmount);
                    AddTestLog($"[서버 테스트] 플레이어 {player.ClientId} 에센스 추가: +{testEssenceAmount}");
                }
            }
        }
    }

    /// <summary>
    /// 에센스 소비 테스트
    /// </summary>
    private void TestSpendEssence()
    {
        if (localPlayer != null)
        {
            if (localPlayer.CanSpendEssence(testEssenceAmount))
            {
                localPlayer.SpendEssenceServerRpc(testEssenceAmount);
                AddTestLog($"[테스트] 에센스 소비 요청: -{testEssenceAmount}");
            }
            else
            {
                AddTestLog($"[에러] 에센스가 부족합니다. (보유: {localPlayer.Essence}, 필요: {testEssenceAmount})");
            }
        }
        else if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsServer)
        {
            var players = NetworkGameManager.Instance.ConnectedPlayers;
            if (players.Count > 0)
            {
                var firstPlayer = players.Values.GetEnumerator();
                firstPlayer.MoveNext();
                var player = firstPlayer.Current;
                if (player != null)
                {
                    if (player.CanSpendEssence(testEssenceAmount))
                    {
                        player.SpendEssenceServerRpc(testEssenceAmount);
                        AddTestLog($"[서버 테스트] 플레이어 {player.ClientId} 에센스 소비: -{testEssenceAmount}");
                    }
                    else
                    {
                        AddTestLog($"[에러] 에센스가 부족합니다. (보유: {player.Essence}, 필요: {testEssenceAmount})");
                    }
                }
            }
        }
    }

    #endregion

    #region 유틸리티

    private void AddTestLog(string message)
    {
        testLog = $"[{System.DateTime.Now:HH:mm:ss}] {message}\n{testLog}";
        
        // 로그가 너무 길어지면 제한
        if (testLog.Length > 2000)
        {
            testLog = testLog.Substring(0, 2000);
        }

        Debug.Log($"[NetworkDataSyncTest] {message}");
    }

    /// <summary>
    /// 테스트 로그 초기화
    /// </summary>
    public void ClearLog()
    {
        testLog = "";
    }

    #endregion
}

