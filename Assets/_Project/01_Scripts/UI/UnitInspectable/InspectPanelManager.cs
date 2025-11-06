// InspectPanelManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InspectPanelManager : MonoSingleton<InspectPanelManager>
{
    [Header("Refs")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform panel;      // 전체 패널
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI subtitle; // 태그/코스트/성급
    [SerializeField] private TextMeshProUGUI stats;    // ATK/APS/Range/HP 등
    [SerializeField] private TextMeshProUGUI synergy;  // 시너지 요약
    [SerializeField] private TextMeshProUGUI upgrades; // 업그레이드 상세
    [SerializeField] private Button pinButton;
    [SerializeField] private GameObject detailArea;    // 핀 시 확장

    private IInspectable current;
    private bool pinned;

    protected override void Awake()
    {
        base.Awake();
        panel.gameObject.SetActive(false);
        if (pinButton) pinButton.onClick.AddListener(() => TogglePin());
        if (UpgradeManager.Instance) UpgradeManager.Instance.OnUpgradeChanged += TryRefresh;
    }

    void OnDestroy()
    {
        if (UpgradeManager.Instance) UpgradeManager.Instance.OnUpgradeChanged -= TryRefresh;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            TryPickUnderMouse();
        }

        if (panel.gameObject.activeSelf && !pinned && current != null && current.FollowTarget)
        {
            // 화면 따라다니기
            Vector2 screenPos = Camera.main.WorldToScreenPoint(current.FollowTarget.position + Vector3.up * 0.8f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform, screenPos, rootCanvas.worldCamera, out var local);
            panel.anchoredPosition = local;
        }

        if (Input.GetKeyDown(KeyCode.Space) && panel.gameObject.activeSelf)
            TogglePin();
    }

    private void TryPickUnderMouse()
    {
        Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.Raycast(wp, Vector2.zero, 0.1f);
        if (!hit.collider)
        {
            Hide();
            return;
        }

        var insp = hit.collider.GetComponentInParent<IInspectable>();
        if (insp != null)
            Show(insp);
        else
            Hide();
    }

    public void Show(IInspectable insp)
    {
        current = insp;
        pinned = false;
        detailArea?.SetActive(false);
        panel.gameObject.SetActive(true);
        TryRefresh();
        // 즉시 위치
        if (current?.FollowTarget)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(current.FollowTarget.position + Vector3.up * 0.8f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform, screenPos, rootCanvas.worldCamera, out var local);
            panel.anchoredPosition = local;
        }
    }

    public void Hide()
    {
        panel.gameObject.SetActive(false);
        current = null;
        pinned = false;
    }

    private void TogglePin()
    {
        pinned = !pinned;
        detailArea?.SetActive(pinned);
    }

    private void TryRefresh()
    {
        if (!panel.gameObject.activeSelf || current == null) return;
        var d = current.BuildData();

        if (icon) icon.sprite = d.icon;
        if (title) title.text = d.name;

        // 유닛/몬스터 공용 서브타이틀
        string star = d.star > 0 ? $"★{d.star}" : "";
        string cost = d.cost > 0 ? $"Cost {d.cost}" : "";
        subtitle.text = $"{star} {cost}".Trim();

        // 스탯 텍스트
        // 유닛이면 ATK/APS/Range, 몬스터면 HP/MoveSpd/Gold
        string s = "";
        if (d.attack > 0 || d.aps > 0)
            s += $"ATK {d.attack}  APS {d.aps:F2}  Range {d.range:F1}\n";
        if (d.hpMax > 0)
            s += $"HP {d.hp}/{d.hpMax}  Move {d.moveSpd:F2}  Gold +{d.gold}\n";
        s += d.tags;
        stats.text = s.Trim();

        synergy.text = string.IsNullOrEmpty(d.synergySummary) ? "" : d.synergySummary;
        upgrades.text = string.IsNullOrEmpty(d.upgradeBreakdown) ? "" : d.upgradeBreakdown;
    }
}
