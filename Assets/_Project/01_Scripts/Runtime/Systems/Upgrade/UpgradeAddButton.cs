using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class UpgradeAddButton : MonoBehaviour
{
    public enum Axis { Cost, Job, Origin }
    [Header("UI 세팅")]
    [SerializeField] private TMP_Text labelText;
    //[SerializeField] private Image iconImage;

    [Header("어느 축에 적용할지")]
    [SerializeField] private Axis axis = Axis.Cost;

    [Header("키 값 (축에 따라 사용)")]
    [SerializeField, Min(1)] private int costKey = 1;
    [SerializeField] private JobSynergy jobKey = JobSynergy.None;
    [SerializeField] private OriginSynergy originKey = OriginSynergy.None;

    [Header("증감량 (감소는 음수)")]
    [SerializeField] private int delta = 1;

    [Header("옵션")]
    [SerializeField] private Button bindButton;   // 비우면 자기 자신에서 찾아 바인딩

    private void Reset()
    {
        bindButton = GetComponent<Button>();
        AutoBindUI();
    }

    private void Awake()
    {
        if (bindButton == null) bindButton = GetComponent<Button>();
        if (bindButton != null) bindButton.onClick.AddListener(Apply);
        AutoBindUI();
    }

    private void OnDestroy()
    {
        if (bindButton != null) bindButton.onClick.RemoveListener(Apply);
    }

    /// <summary>인스펙터에서 테스트용</summary>
    [ContextMenu("Apply Once")]
    public void Apply() => ApplyInternal(delta);
    private void AutoBindUI()
    {
        // 프리팹에서 labelText 안 넣어뒀으면 하위에서 자동 검색
        if (labelText == null)
            labelText = GetComponentInChildren<TMP_Text>(true);
        //if (iconImage == null)
        //    iconImage = GetComponentInChildren<Image>(true);
    }


    /// <summary>외부 UI에서 델타를 넘겨 호출하고 싶을 때 사용(슬라이더/스텝 등)</summary>
    public void ApplyWithDelta(int d) => ApplyInternal(d);

    private void ApplyInternal(int d)
    {
        var mgr = UpgradeManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[UpgradeAddButton] UpgradeManager.Instance가 없습니다.");
            return;
        }
        switch (axis)
        {
            case Axis.Cost:
                mgr.AddCostStage(costKey, d);
                break;

            case Axis.Job:
                if (!IsSingleFlag(jobKey))
                {
                    Debug.LogWarning("[UpgradeAddButton] JobSynergy는 단일 플래그만 선택하세요.");
                    return;
                }
                if (jobKey == JobSynergy.None) return;
                mgr.AddJobStage(jobKey, d);
                break;

            case Axis.Origin:
                if (!IsSingleFlag(originKey))
                {
                    Debug.LogWarning("[UpgradeAddButton] OriginSynergy는 단일 플래그만 선택하세요.");
                    return;
                }
                if (originKey == OriginSynergy.None) return;
                mgr.AddOriginStage(originKey, d);
                break;
        }
    }

    // [Flags] enum 단일 플래그인지 검사 (0이나 복합값 방지)
    private static bool IsSingleFlag<TEnum>(TEnum value) where TEnum : Enum
    {
        long v = Convert.ToInt64(value);
        return v != 0 && (v & (v - 1)) == 0;
    }

    public void Configure(Axis axis, int costKey = 0,
                          JobSynergy jobKey = JobSynergy.None,
                          OriginSynergy originKey = OriginSynergy.None,
                          int delta = 1,
                          Button clickTarget = null,
                          string label = null,
                          Sprite icon = null)
    {
        this.axis = axis;
        this.delta = delta;

        switch (axis)
        {
            case Axis.Cost:
                this.costKey = Mathf.Max(1, costKey);
                break;
            case Axis.Job:
                this.jobKey = jobKey;
                break;
            case Axis.Origin:
                this.originKey = originKey;
                break;
        }

        // 바인딩 버튼 교체 옵션
        if (clickTarget != null)
        {
            if (bindButton != null) bindButton.onClick.RemoveListener(Apply);
            bindButton = clickTarget;
            bindButton.onClick.AddListener(Apply);
        }

        // 라벨 세팅 (넘겨준 label이 없으면 기본 포맷)
        if (labelText == null) AutoBindUI();
        if (labelText != null)
        {
            labelText.text = label ?? GetDefaultLabel();
        }

        // (선택) 아이콘 세팅
        //if (iconImage != null && icon != null)
        //    iconImage.sprite = icon;
    }

    public void ConfigureCost(int cost, int delta = 1, Button clickTarget = null, string label = null, Sprite icon = null)
        => Configure(Axis.Cost, costKey: cost, delta: delta, clickTarget: clickTarget, label: label, icon: icon);

    public void ConfigureJob(JobSynergy job, int delta = 1, Button clickTarget = null, string label = null, Sprite icon = null)
        => Configure(Axis.Job, jobKey: job, delta: delta, clickTarget: clickTarget, label: label, icon: icon);

    public void ConfigureOrigin(OriginSynergy origin, int delta = 1, Button clickTarget = null, string label = null, Sprite icon = null)
        => Configure(Axis.Origin, originKey: origin, delta: delta, clickTarget: clickTarget, label: label, icon: icon);


    // 프리팹 스폰 + 설정까지 한 번에
    public static UpgradeAddButton Create(UpgradeAddButton prefab, Transform parent,
                                          Action<UpgradeAddButton> configure)
    {
        var go = UnityEngine.Object.Instantiate(prefab.gameObject, parent);
        var uab = go.GetComponent<UpgradeAddButton>();
        configure?.Invoke(uab);
        return uab;
    }

    private string GetDefaultLabel()
    {
        return axis switch
        {
            Axis.Cost => $"Cost {costKey}",
            Axis.Job => jobKey.ToString(),
            Axis.Origin => originKey.ToString(),
            _ => name
        };
    }
}
