using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

public class UpgradeButtonPanelBuilder : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private UpgradeConfig upgradeConfig;          // 기본 곡선 데이터
    [SerializeField] private MapSynergyConfig mapConfig;           // (선택) 맵 전용 시너지
    [SerializeField] private UpgradeAddButton buttonPrefab;        // Axis/Key/Delta 세팅용 프리팹
    [SerializeField] private Transform costParent;                 // 코스트 버튼 부모
    [SerializeField] private Transform jobParent;                  // 잡 버튼 부모
    [SerializeField] private Transform originParent;               // 오리진 버튼 부모

    [Header("버튼 표시 옵션")]
    [SerializeField] private string costLabelFormat = "Cost {0}";
    [SerializeField] private string jobLabelFormat = "{0}";
    [SerializeField] private string originLabelFormat = "{0}";
    [SerializeField, Min(1)] private int defaultDelta = 1;         // +버튼 생성 시 델타
    [SerializeField, Min(1)] private int defaultMinusDelta = 1;    // -버튼 생성 시 델타

    [Header("라벨 컴포넌트 경로 (선택)")]
    [SerializeField] private string labelChildPath = "Label";      // 버튼 프리팹 하위에 TMP/Text가 있으면 경로 지정

#if ODIN_INSPECTOR
    [Button("Rebuild (Play/Editor)")]
#endif
    public void Rebuild()
    {
        if (upgradeConfig == null)
        {
            Debug.LogWarning("[UpgradeButtonPanelBuilder] UpgradeConfig가 비었습니다.");
            return;
        }

        ClearChildren(costParent);
        ClearChildren(jobParent);
        ClearChildren(originParent);

        BuildCostButtons();
        BuildJobButtons();
        BuildOriginButtons();
    }

    private void BuildCostButtons()
    {
        var list = upgradeConfig.costCurves?.Select(c => c.costKey).ToList();
        if (list == null || list.Count == 0) return;

        // 맵 오버라이드가 있으면 그 순서대로
        if (mapConfig != null && mapConfig.HasCostOrder)
        {
            var ordered = mapConfig.costOrderOverride.Where(k => list.Contains(k)).ToList();
            var rest = list.Where(k => !ordered.Contains(k)).OrderBy(k => k);
            list = ordered.Concat(rest).ToList();
        }
        else
        {
            list.Sort();
        }

        foreach (int cost in list)
        {
            // + 버튼
            CreateButton(costParent, UpgradeAddButton.Axis.Cost,
                         costKey: cost, delta: +defaultDelta,
                         displayText: string.Format(costLabelFormat, cost));

            // - 버튼 (원하면 주석 해제)
            CreateButton(costParent, UpgradeAddButton.Axis.Cost,
                         costKey: cost, delta: -defaultMinusDelta,
                         displayText: string.Format(costLabelFormat, cost) + " (-)");
        }
    }

    private void BuildJobButtons()
    {
        // 표시 대상 집합 결정: 맵 설정 → 없으면 Config의 키 → 최후엔 enum 단일 플래그 전부
        IEnumerable<JobSynergy> source;
        if (mapConfig != null && mapConfig.HasJobList)
            source = mapConfig.jobs;
        else if (upgradeConfig.jobCurves != null && upgradeConfig.jobCurves.Count > 0)
            source = upgradeConfig.jobCurves.Keys;
        else
            source = Enum.GetValues(typeof(JobSynergy)).Cast<JobSynergy>();

        foreach (var key in source)
        {
            if (!IsSingleFlag(key) || key == JobSynergy.None) continue;
            var label = jobLabelFormat.Replace("{0}", key.ToString());

            CreateButton(jobParent, UpgradeAddButton.Axis.Job,
                         jobKey: key, delta: +defaultDelta,
                         displayText: label,
                         icon: mapConfig?.jobIcons != null && mapConfig.jobIcons.TryGetValue(key, out var s) ? s : null);

            CreateButton(jobParent, UpgradeAddButton.Axis.Job,
                         jobKey: key, delta: -defaultMinusDelta,
                         displayText: label + " (-)",
                         icon: null);
        }
    }

    private void BuildOriginButtons()
    {
        IEnumerable<OriginSynergy> source;
        if (mapConfig != null && mapConfig.HasOriginList)
            source = mapConfig.origins;
        else if (upgradeConfig.originCurves != null && upgradeConfig.originCurves.Count > 0)
            source = upgradeConfig.originCurves.Keys;
        else
            source = Enum.GetValues(typeof(OriginSynergy)).Cast<OriginSynergy>();

        foreach (var key in source)
        {
            if (!IsSingleFlag(key) || key == OriginSynergy.None) continue;
            var label = originLabelFormat.Replace("{0}", key.ToString());

            CreateButton(originParent, UpgradeAddButton.Axis.Origin,
                         originKey: key, delta: +defaultDelta,
                         displayText: label,
                         icon: mapConfig?.originIcons != null && mapConfig.originIcons.TryGetValue(key, out var s) ? s : null);

            CreateButton(originParent, UpgradeAddButton.Axis.Origin,
                         originKey: key, delta: -defaultMinusDelta,
                         displayText: label + " (-)",
                         icon: null);
        }
    }

    private void CreateButton(Transform parent,
                              UpgradeAddButton.Axis axis,
                              int costKey = 0,
                              JobSynergy jobKey = JobSynergy.None,
                              OriginSynergy originKey = OriginSynergy.None,
                              int delta = 1,
                              string displayText = null,
                              Sprite icon = null)
    {
        if (buttonPrefab == null || parent == null) return;

        var go = Instantiate(buttonPrefab.gameObject, parent);
        var uab = go.GetComponent<UpgradeAddButton>();
        if (uab == null) return;

        // 리플렉션 없이 직렬 필드 세팅 (프리팹에 public/SerializeField 있음)
        // axis는 인스펙터 고정이라면, 프리팹을 축별로 3종 쓰는 것도 방법.
        // 아래 방식은 Awake 이전 생성이므로 문제 없음.
        SetAxis(uab, axis);

        switch (axis)
        {
            case UpgradeAddButton.Axis.Cost:
                SetField(uab, "costKey", costKey);
                break;
            case UpgradeAddButton.Axis.Job:
                SetField(uab, "jobKey", jobKey);
                break;
            case UpgradeAddButton.Axis.Origin:
                SetField(uab, "originKey", originKey);
                break;
        }
        SetField(uab, "delta", delta);

        // 라벨/아이콘 세팅(선택)
        if (!string.IsNullOrEmpty(labelChildPath))
        {
            var labelTr = go.transform.Find(labelChildPath);
            if (labelTr != null)
            {
                var tmp = labelTr.GetComponent<TMPro.TMP_Text>();
                if (tmp != null) tmp.text = displayText ?? go.name;
            }
        }
        if (icon != null)
        {
            var img = go.GetComponentInChildren<UnityEngine.UI.Image>();
            if (img != null) img.sprite = icon;
        }
    }

    // === 유틸 ===
    private static void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            DestroyImmediate(t.GetChild(i).gameObject);
    }

    private static bool IsSingleFlag<TEnum>(TEnum value) where TEnum : Enum
    {
        long v = Convert.ToInt64(value);
        return v != 0 && (v & (v - 1)) == 0;
    }

    private static void SetAxis(UpgradeAddButton uab, UpgradeAddButton.Axis axis)
    {
        // axis는 private이 아니면 직접 대입. 만약 private라면 프리팹을 축별로 나누자.
        var fi = typeof(UpgradeAddButton).GetField("axis",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        fi?.SetValue(uab, axis);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var fi = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        fi?.SetValue(target, value);
    }

    private void Awake()
    {
        // 씬 진입 시 자동 구성 원하면 활성화
        // Rebuild();
    }
}
