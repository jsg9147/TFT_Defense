// FieldMonsterCounterUI.cs
using TMPro;
using UnityEngine;

public class FieldMonsterCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private void OnEnable()
    {
        var svc = MonsterFieldManager.Instance;
        svc.OnCountChanged += Handle;
        Handle(svc.CurrentCount, svc.FieldLimit);
    }
    private void OnDisable()
    {
        var svc = FindAnyObjectByType<MonsterFieldManager>();
        if (svc != null) svc.OnCountChanged -= Handle;
    }
    private void Handle(int cur, int limit)
    {
        if (text != null) text.text = $"{cur} / {limit}";
    }
}
