// GameManager.cs - 변경/추가 파트만
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingleton<GameManager>
{
    public enum GameState { Prepare, Battle, Shop, Win, Lose }
    public GameState CurrentState { get; private set; }

    [Header("웨이브 설정")]
    public int currentWave = 0;
    public float prepareTime = 5f;
    public float battleTime = 20f;   // 전투 타이머 추가
    public float shopTime = 10f;

    [Header("씬 종속 매니저")]
    public ShopManager shopManager;
    public MonsterSpawner monsterSpawner;

    // ⬇️ UI 이벤트
    public event Action<int> OnWaveChanged;                 // 웨이브 시작 시
    public event Action<GameState> OnPhaseChanged;          // 페이즈 전환 시
    public event Action<float, float> OnTimerTick;          // (remain, total)
    public event Action OnTimerEnd;                         // 타이머 종료 시

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
        // 씬이 다시 로딩되었을 때 게임 초기화
        InitializeGame();
    }

    /// <summary>게임 초기화 (씬 로딩 시 호출)</summary>
    private void InitializeGame()
    {
        // 1. 웨이브 초기화
        currentWave = 0;
        CurrentState = GameState.Prepare;

        // 2. 실행 중인 코루틴 중지
        if (waveLoopCoroutine != null)
        {
            StopCoroutine(waveLoopCoroutine);
            waveLoopCoroutine = null;
        }

        // 3. 씬 매니저 바인딩
        BindSceneManagers();

        // 4. 다른 매니저들 초기화
        ResetAllManagers();

        // 5. 게임 상태 설정 및 웨이브 루프 시작
        SetGameState(GameState.Prepare);
        
        // 한도 도달시 패배 이벤트 재구독
        var field = (IMonsterFieldService)MonsterFieldManager.Instance;
        field.OnLimitReached -= () => SetGameState(GameState.Lose);
        field.OnLimitReached += () => SetGameState(GameState.Lose);

        // 6. 웨이브 루프 시작
        waveLoopCoroutine = StartCoroutine(WaveLoop());
        
        Debug.Log("[GameManager] 게임 초기화 완료");
    }

    /// <summary>모든 매니저 초기화</summary>
    private void ResetAllManagers()
    {
        // MonsterFieldManager 초기화
        if (MonsterFieldManager.Instance != null)
        {
            MonsterFieldManager.Instance.ResetCount();
        }

        // CurrencyManager 초기화
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.Reset();
        }

        // PlayerLevelManager 초기화
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.Reset();
        }
    }

    public void BindSceneManagers()
    {
        if (shopManager == null) shopManager = FindAnyObjectByType<ShopManager>();
        if (monsterSpawner == null) monsterSpawner = FindAnyObjectByType<MonsterSpawner>();
        Debug.Log("[GameManager] 씬 매니저 바인딩 완료");
    }

    private IEnumerator WaveLoop()
    {
        while (true)
        {
            // 1) 준비 페이즈 (⬅️ 복구)
            SetGameState(GameState.Prepare);
            OnWaveChanged?.Invoke(currentWave);
            yield return StartCoroutine(RunTimer(prepareTime));

            // 2) 전투 페이즈
            SetGameState(GameState.Battle);
            monsterSpawner?.StartWave(currentWave);

            yield return StartCoroutine(RunTimer(battleTime));
            monsterSpawner?.StopSpawning(); // 추가 스폰 금지

            int alive = MonsterFieldManager.Instance ? MonsterFieldManager.Instance.CurrentCount : 0;
            bool last = monsterSpawner != null && monsterSpawner.IsLastWave(currentWave);

            if (alive > 0)
            {
                SetGameState(GameState.Lose);
                yield break;
            }
            else
            {
                if (last)
                {
                    SetGameState(GameState.Win);
                    yield break;
                }

                // 상점 페이즈를 쓰려면 아래 3줄 주석 해제
                //SetGameState(GameState.Shop);
                //yield return StartCoroutine(RunTimer(shopTime));
                currentWave++;
            }
        }
    }


    /// <summary> duration 동안 매 프레임 OnTimerTick(남은, 전체) 발행. </summary>
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
                // 필요시 별도 처리
                break;
        }
    }

    public bool IsBattlePhase() => CurrentState == GameState.Battle;
}

