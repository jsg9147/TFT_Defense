using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

public class UpgradeButtonPanelBuilder : MonoBehaviour
{
    [Header("�ʼ� ����")]
    [SerializeField] private UpgradeConfig upgradeConfig;          // �⺻ � ������
    [SerializeField] private MapSynergyConfig mapConfig;           // (����) �� ���� �ó���
    [SerializeField] private UpgradeAddButton buttonPrefab;        // Axis/Key/Delta ���ÿ� ������
    [SerializeField] private Transform costParent;                 // �ڽ�Ʈ ��ư �θ�
    [SerializeField] private Transform jobParent;                  // �� ��ư �θ�
    [SerializeField] private Transform originParent;               // ������ ��ư �θ�

    [Header("��ư ǥ�� �ɼ�")]
    [SerializeField] private string costLabelFormat = "Cost {0}";
    [SerializeField] private string jobLabelFormat = "{0}";
    [SerializeField] private string originLabelFormat = "{0}";
    [SerializeField, Min(1)] private int defaultDelta = 1;         // +��ư ���� �� ��Ÿ
    [SerializeField, Min(1)] private int defaultMinusDelta = 1;    // -��ư ���� �� ��Ÿ

    [Header("�� ������Ʈ ��� (����)")]
    [SerializeField] private string labelChildPath = "Label";      // ��ư ������ ������ TMP/Text�� ������ ��� ����

#if ODIN_INSPECTOR
    [Button("Rebuild (Play/Editor)")]
#endif
    public void Rebuild()
    {
        if (upgradeConfig == null)
        {
            Debug.LogWarning("[UpgradeButtonPanelBuilder] UpgradeConfig�� ������ϴ�.");
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

        // �� �������̵尡 ������ �� �������
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
            // + ��ư
            CreateButton(costParent, UpgradeAddButton.Axis.Cost,
                         costKey: cost, delta: +defaultDelta,
                         displayText: string.Format(costLabelFormat, cost));

            // - ��ư (���ϸ� �ּ� ����)
            CreateButton(costParent, UpgradeAddButton.Axis.Cost,
                         costKey: cost, delta: -defaultMinusDelta,
                         displayText: string.Format(costLabelFormat, cost) + " (-)");
        }
    }

    private void BuildJobButtons()
    {
        // ǥ�� ��� ���� ����: �� ���� �� ������ Config�� Ű �� ���Ŀ� enum ���� �÷��� ����
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

        // ���÷��� ���� ���� �ʵ� ���� (�����տ� public/SerializeField ����)
        // axis�� �ν����� �����̶��, �������� �ະ�� 3�� ���� �͵� ���.
        // �Ʒ� ����� Awake ���� �����̹Ƿ� ���� ����.
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

        // ��/������ ����(����)
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

    // === ��ƿ ===
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
        // axis�� private�� �ƴϸ� ���� ����. ���� private��� �������� �ະ�� ������.
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
        // �� ���� �� �ڵ� ���� ���ϸ� Ȱ��ȭ
        // Rebuild();
    }
}
