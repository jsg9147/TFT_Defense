using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Sirenix.OdinInspector;

public class WinLosePanel : MonoBehaviour
{
    [FoldoutGroup("FX & Root")]
    [SerializeField] private CanvasGroup cg;
    [FoldoutGroup("FX & Root")]
    [SerializeField] private GameObject winFx;
    [FoldoutGroup("FX & Root")]
    [SerializeField] private GameObject loseFx;

    [FoldoutGroup("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [FoldoutGroup("Texts")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [FoldoutGroup("Texts")]
    [SerializeField] private TextMeshProUGUI waveText;
    [FoldoutGroup("Texts")]
    [SerializeField] private TextMeshProUGUI timeText;
    [FoldoutGroup("Texts")]
    [SerializeField] private TextMeshProUGUI goldText;
    [FoldoutGroup("Texts")]
    [SerializeField] private TextMeshProUGUI essenceText;

    [FoldoutGroup("Buttons")]
    [SerializeField] private Button retryButton;
    [FoldoutGroup("Buttons")]
    [SerializeField] private Button nextButton;
    [FoldoutGroup("Buttons")]
    [SerializeField] private Button lobbyButton;

    private Action _onRetry;
    private Action _onNext;
    private Action _onLobby;

    void Awake()
    {
        HideInstant();
        if (retryButton) retryButton.onClick.AddListener(() => _onRetry?.Invoke());
        if (nextButton) nextButton.onClick.AddListener(() => _onNext?.Invoke());
        if (lobbyButton) lobbyButton.onClick.AddListener(() => _onLobby?.Invoke());
    }

    public void Show(bool isWin, ResultSnapshot snap, Action onRetry, Action onNext, Action onLobby)
    {
        _onRetry = onRetry;
        _onNext = onNext;
        _onLobby = onLobby;

        if (winFx) winFx.SetActive(isWin);
        if (loseFx) loseFx.SetActive(!isWin);

        if (titleText) titleText.text = isWin ? "승리!" : "패배…";
        if (subtitleText) subtitleText.text = isWin ? "깔끔했다!" : "다음엔 더 잘해보자";

        if (waveText) waveText.text = $"도달 웨이브 : {snap.reachedWave}";
        if (timeText) timeText.text = $"플레이 시간 : {FormatTime(snap.elapsedSeconds)}";
        if (goldText) goldText.text = $"보유 골드 : {snap.goldCurrent}";
        if (essenceText) essenceText.text = $"보유 정수 : {snap.essenceCurrent}";

        // ‘다음’ 버튼은 스테이지가 있을 때만(임시로 비활성)
        if (nextButton) nextButton.gameObject.SetActive(false);

        gameObject.SetActive(true);
        FadeIn();
    }

    public void HideInstant()
    {
        if (cg)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    private void FadeIn()
    {
        if (!cg) return;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        // 애니메이션 쓰면 DOTween/TMPro Gradient 등 적용 추천
    }

    private string FormatTime(float sec)
    {
        int m = Mathf.FloorToInt(sec / 60f);
        int s = Mathf.FloorToInt(sec % 60f);
        return $"{m:00}:{s:00}";
    }
}
