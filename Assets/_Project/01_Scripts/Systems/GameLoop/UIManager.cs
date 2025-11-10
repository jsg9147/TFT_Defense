using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoSingleton<UIManager>
{
    [Header("UI 패널 참조")]
    public GameObject battlePanel;
    public GameObject winPanel;   // 기존 승패 패널은 유지 (필요시 숨김용)
    public GameObject losePanel;
    public GameObject upgradePanel;
    public GameObject gamblePanel;

    [Header("공통 HUD (전투 중 표시)")]
    [SerializeField] private GameObject battleHUD;

    [Header("승패 패널 (새 시스템)")]
    [Required]
    [SerializeField] private WinLosePanel winLosePanel;

    [Header("전역 입력 차단 (풀스크린 Image+RaycastTarget)")]
    [Required]
    [SerializeField] private GameObject inputBlocker;

    private float _prevTimeScale = 1f;

    protected override void Awake()
    {
        base.Awake();
        HideAllPanels();
        if (inputBlocker) inputBlocker.SetActive(false);
    }

    #region ====== Panel Controls ======

    public void ShowBattleUI()
    {
        HideAllPanels();
        battlePanel?.SetActive(true);
        battleHUD?.SetActive(true);
        if (inputBlocker) inputBlocker.SetActive(false);
        ResumeTime();
    }

    public void ShowUpgradeUI()
    {
        HideAllPanels();
        upgradePanel?.SetActive(true);
    }

    public void ShowGambleUI()
    {
        HideAllPanels();
        gamblePanel?.SetActive(true);
    }

    public void HideAllPanels()
    {
        battlePanel?.SetActive(false);
        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
        upgradePanel?.SetActive(false);
        gamblePanel?.SetActive(false);
    }

    #endregion

    #region ====== Win/Lose Results ======

    public void ShowWinUI()
    {
        ShowResultInternal(true);
    }

    public void ShowLoseUI()
    {
        ShowResultInternal(false);
    }

    private void ShowResultInternal(bool isWin)
    {
        HideAllPanels();
        battleHUD?.SetActive(false);
        if (inputBlocker) inputBlocker.SetActive(true);

        // 게임 결과 데이터 스냅샷 구성
        var snap = ResultSnapshot.CaptureNow(
            currentWave: GameManager.Instance.currentWave,
            elapsed: Time.timeSinceLevelLoad,
            gold: CurrencyManager.Instance?.Gold ?? 0,
            essence: CurrencyManager.Instance?.Essence ?? 0
        );

        PauseTime();

        if (winLosePanel)
        {
            winLosePanel.Show(isWin, snap,
                onRetry: () =>
                {
                    ResumeTime();
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                },
                onNext: () =>
                {
                    ResumeTime();
                    // 다음 스테이지 있으면 여기에 씬 로딩
                    // SceneManager.LoadScene("NextStage");
                },
                onLobby: () =>
                {
                    ResumeTime();
                    SceneManager.LoadScene("Title"); // 로비나 메인 타이틀로 이동
                });
        }
        else
        {
            // WinLosePanel 세팅 안됐을 때 대비
            (isWin ? winPanel : losePanel)?.SetActive(true);
        }
    }

    private void PauseTime()
    {
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void ResumeTime()
    {
        Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
    }

    #endregion
}
