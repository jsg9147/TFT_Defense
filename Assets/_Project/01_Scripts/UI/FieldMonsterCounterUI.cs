// FieldMonsterCounterUI.cs
using TMPro;
using UnityEngine;

public class FieldMonsterCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    [Tooltip("표시할 플레이어 인덱스 (0 또는 1)")]
    [SerializeField] private int playerIndex = 0;

    private void OnEnable()
    {
        var svc = MonsterFieldManager.Instance;
        svc.OnCountChanged += Handle;
        Handle(playerIndex, svc.GetCurrentCount(playerIndex), svc.GetFieldLimit(playerIndex));
    }

    private void OnDisable()
    {
        var svc = FindAnyObjectByType<MonsterFieldManager>();
        if (svc != null) svc.OnCountChanged -= Handle;
    }

    private void Handle(int pIndex, int cur, int limit)
    {
        // 자기 플레이어의 카운트만 표시
        if (pIndex != playerIndex) return;
        if (text != null) text.text = $"{cur} / {limit}";
    }
}
