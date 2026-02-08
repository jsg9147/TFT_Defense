// GameManager.cs
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingleton<GameManager>
{
    public enum GameState { Prepare, Battle, Shop, Win, Lose }
    public GameState CurrentState { get; private set; }

    private const int MaxPlayers = 2;

    [Header("웨이브 설정")]
    public int currentWave = 0;
    public float prepareTime = 5f;
    public float battleTime = 20f;
    public float shopTime = 10f;

    [Header("씬 종속 매니저")]
    public ShopManager shopManager;
    public MonsterSpawner monsterSpawner;

    // UI 이벤트
    public event Action<int> OnWaveChanged;
    public event Action<GameState> OnPhaseChanged;
    public event Action<float, float> OnTimerTick;
    public event Action OnTimerEnd;

    /// <summary>특정 플레이어가 패배했을 때 (playerIndex)</summary>
    public event Action<int> OnPlayerLose;

    private Coroutine waveLoopCoroutine;

    protected override void Awake()
    {
        base.Awake();
        CurrentState = GameState.Prepare;
    }

    private void Start()
    {
        InitializeGame();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeGame();
    }

    /// <summary>네트워크 모드에서 서버인지 확인. 네트워크가 없으면 true (싱글플레이)</summary>
    private bool IsServerOrSinglePlayer()
    {
        var nm = NetworkManager.Singleton;
        return nm == null || !nm.IsClient || nm.IsServer;
    }

    /// <summary>게임 초기화 (씬 로딩 시 호출)</summary>
    private void InitializeGame()
    {
        currentWave = 0;
        CurrentState = GameState.Prepare;

        if (waveLoopCoroutine != null)
        {
            StopCoroutine(waveLoopCoroutine);
            waveLoopCoroutine = null;
        }

        BindSceneManagers();
        ResetAllManagers();
        SetGameState(GameState.Prepare);

        // 필드 한도 도달 시 해당 플레이어 패배
        var field = MonsterFieldManager.Instance;
        if (field != null)
        {
            field.OnLimitReached -= HandlePlayerLimitReached;
            field.OnLimitReached += HandlePlayerLimitReached;
        }

        // 서버(또는 싱글플레이)에서만 웨이브 루프 실행
        if (IsServerOrSinglePlayer())
        {
            waveLoopCoroutine = StartCoroutine(WaveLoop());
        }

        Debug.Log("[GameManager] 게임 초기화 완료");
    }

    private void HandlePlayerLimitReached(int playerIndex)
    {
        Debug.Log($"[GameManager] Player {playerIndex} 필드 한도 도달 → 패배");
        OnPlayerLose?.Invoke(playerIndex);
        SetGameState(GameState.Lose);
    }

    private void ResetAllManagers()
    {
        if (MonsterFieldManager.Instance != null)
            MonsterFieldManager.Instance.ResetCount();

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.Reset();

        if (PlayerLevelManager.Instance != null)
            PlayerLevelManager.Instance.Reset();
    }

    public void BindSceneManagers()
    {
        if (shopManager == null) shopManager = FindAnyObjectByType<ShopManager>();
        if (monsterSpawner == null) monsterSpawner = FindAnyObjectByType<MonsterSpawner>();
        Debug.Log("[GameManager] 씬 매니저 바인딩 완료");
    }

    /// <summary>서버에서만 실행되는 웨이브 루프</summary>
    private IEnumerator WaveLoop()
    {
        while (true)
        {
            // 1) 준비 페이즈
            SetGameState(GameState.Prepare);
            OnWaveChanged?.Invoke(currentWave);
            monsterSpawner?.PrepareWave();
            yield return StartCoroutine(RunTimer(prepareTime));

            // 2) 전투 페이즈 (그룹 단위로 반복)
            int groupCount = monsterSpawner.GetGroupCount(currentWave);
            for (int i = 0; i < groupCount; i++)
            {
                SetGameState(GameState.Battle);

                // 모든 플레이어 보드에 동시 스폰 (서버에서만 실행)
                for (int p = 0; p < MaxPlayers; p++)
                {
                    monsterSpawner.SpawnWaveGroup(currentWave, i, p);
                }

                WaveGroup currentGroup = monsterSpawner.GetWaveGroup(currentWave, i);
                float currentBattleTime = currentGroup.battleDuration > 0 ? currentGroup.battleDuration : battleTime;
                yield return StartCoroutine(RunTimer(currentBattleTime));

                monsterSpawner.StopSpawning();
                yield return null;

                Debug.Log($"[GameManager] 그룹 {i} 클리어 여부: {(monsterSpawner.AliveCount == 0 ? "성공" : "남은 몬스터 있음")}");
            }

            // 3) 모든 그룹 클리어 후
            Debug.Log($"[GameManager] 웨이브 {currentWave} 모든 그룹 클리어!");
            bool isLastWave = monsterSpawner != null && monsterSpawner.IsLastWave(currentWave);
            if (isLastWave)
            {
                SetGameState(GameState.Win);
                yield break;
            }
            else
            {
                currentWave++;
            }
        }
    }

    private IEnumerator RunTimer(float duration)
    {
        float remain = duration;
        OnTimerTick?.Invoke(remain, duration);

        while (remain > 0f)
        {
            remain -= Time.deltaTime;
            OnTimerTick?.Invoke(Mathf.Max(0f, remain), duration);
            yield return null;
        }

        OnTimerEnd?.Invoke();
    }

    public void SetGameState(GameState state)
    {
        CurrentState = state;
        OnPhaseChanged?.Invoke(state);

        switch (state)
        {
            case GameState.Shop:
                monsterSpawner?.StopSpawning();
                break;
            case GameState.Battle:
                UIManager.Instance.ShowBattleUI();
                break;
            case GameState.Win:
                UIManager.Instance.ShowWinUI();
                monsterSpawner?.StopSpawning();
                break;
            case GameState.Lose:
                UIManager.Instance.ShowLoseUI();
                monsterSpawner?.StopSpawning();
                break;
            case GameState.Prepare:
            default:
                break;
        }
    }

    public bool IsBattlePhase() => CurrentState == GameState.Battle;
}
