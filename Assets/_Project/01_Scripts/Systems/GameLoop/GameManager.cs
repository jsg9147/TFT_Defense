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
    public float battleTime = 20f;   // ⬅️ 전투 타이머 추가
    public float shopTime = 10f;

    [Header("씬 종속 매니저")]
    public ShopManager shopManager;
    public MonsterSpawner monsterSpawner;

    // ⬇️ UI 이벤트
    public event Action<int> OnWaveChanged;                 // 웨이브 시작 시
    public event Action<GameState> OnPhaseChanged;          // 페이즈 전환 시
    public event Action<float, float> OnTimerTick;          // (remain, total)
    public event Action OnTimerEnd;                         // 타이머 종료 시

    protected override void Awake()
    {
        base.Awake();
        CurrentState = GameState.Prepare;
    }

    private void Start()
    {
        BindSceneManagers();
        SetGameState(GameState.Prepare);
        StartCoroutine(WaveLoop());

        // 한도 도달시 패배
        var field = (IMonsterFieldService)MonsterFieldManager.Instance;
        field.OnLimitReached += () => SetGameState(GameState.Lose);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindSceneManagers();

    public void BindSceneManagers()
    {
        if (shopManager == null) shopManager = FindAnyObjectByType<ShopManager>();
        if (monsterSpawner == null) monsterSpawner = FindAnyObjectByType<MonsterSpawner>();
        Debug.Log("[GameManager] 씬 매니저 바인딩 완료");
    }

    // / <summary> 웨이브 루프: 준비 -> 전투 -> 상점 반복 </summary>
    private IEnumerator WaveLoop()
    {
        while (true)
        {
            // 1) 준비 페이즈
            SetGameState(GameState.Prepare);
            OnWaveChanged?.Invoke(currentWave); 
            yield return StartCoroutine(RunTimer(prepareTime));

            // 2) 전투 페이즈
            SetGameState(GameState.Battle);
            monsterSpawner?.StartWave(currentWave);

            // 전투는 '시간 종료'로 페이즈 이동 (필드는 정리하지 않음)
            yield return StartCoroutine(RunTimer(battleTime));
            monsterSpawner?.StopSpawning(); // 해당 웨이브 추가 스폰만 중단

            // 3) 상점 페이즈
            SetGameState(GameState.Shop);
            yield return StartCoroutine(RunTimer(shopTime));

            currentWave++;
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
            // GameManager.cs (SetGameState 내부)
            case GameState.Shop:
                UIManager.Instance.ShowShopUI();
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
