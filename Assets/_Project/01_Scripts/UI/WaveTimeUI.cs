// WaveTimerUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveTimerUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Slider timeBar; // 0~1

    private void OnEnable()
    {
        var gm = GameManager.Instance;
        gm.OnWaveChanged += HandleWaveChanged;
        gm.OnPhaseChanged += HandlePhaseChanged;
        gm.OnTimerTick += HandleTimerTick;
        gm.OnTimerEnd += HandleTimerEnd;

        // 초기 표시
        HandleWaveChanged(gm.currentWave);
        HandlePhaseChanged(gm.CurrentState);
        HandleTimerTick(0, 1);
    }

    private void OnDisable()
    {
        var gm = GameManager.Instance;
        gm.OnWaveChanged -= HandleWaveChanged;
        gm.OnPhaseChanged -= HandlePhaseChanged;
        gm.OnTimerTick -= HandleTimerTick;
        gm.OnTimerEnd -= HandleTimerEnd;
    }

    private void HandleWaveChanged(int wave)
    {
        // 내부는 0부터, 표시용은 +1
        if (waveText) waveText.text = $"Wave {wave + 1}";
    }

    private void HandlePhaseChanged(GameManager.GameState state)
    {
        if (!phaseText) return;
        switch (state)
        {
            case GameManager.GameState.Prepare: phaseText.text = "Prepare"; break;
            case GameManager.GameState.Battle: phaseText.text = "Battle"; break;
            case GameManager.GameState.Shop: phaseText.text = "Shop"; break;
            case GameManager.GameState.Win: phaseText.text = "Win"; break;
            case GameManager.GameState.Lose: phaseText.text = "Lose"; break;
        }
    }

    private void HandleTimerTick(float remain, float total)
    {
        int sec = Mathf.CeilToInt(remain);
        int m = sec / 60;
        int s = sec % 60;

        if (timeText) timeText.text = $"{m:00}:{s:00}";

        float ratio = total > 0f ? Mathf.Clamp01(remain / total) : 0f;
        if (timeBar) timeBar.value = ratio;
    }

    private void HandleTimerEnd()
    {
        if (timeText) timeText.text = "00:00";
        if (timeBar) timeBar.value = 0f;
    }
}
