using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

public class UpgradeButtonAutoBuilder : MonoBehaviour
{
    [Header("������ �ҽ�")]
    [SerializeField] private UpgradeConfig upgradeConfig;

    [Tooltip("�� �ʿ� ������ �����ϴ� �� �ó��� (���� config�� key ���)")]
    [SerializeField] private List<JobSynergy> mapJobs;

    [Tooltip("�� �ʿ� ������ �����ϴ� ������ �ó��� (���� config�� key ���)")]
    [SerializeField] private List<OriginSynergy> mapOrigins;

    [Tooltip("ǥ���� �ڽ�Ʈ ��� (���� config.costCurves�� key ���, ��������)")]
    [SerializeField] private List<int> costKeys;

    [Header("������ & �θ�")]
    [SerializeField] private UpgradeAddButton buttonPrefab;
    [SerializeField] private Transform costParent;
    [SerializeField] private Transform jobParent;
    [SerializeField] private Transform originParent;

    [Header("��ư �ɼ�")]
    [SerializeField, Min(1)] private int defaultDelta = 1;

#if ODIN_INSPECTOR
    [Button("Rebuild")]
#endif
    public void Rebuild()
    {
        if (buttonPrefab == null || upgradeConfig == null)
        {
            Debug.LogWarning("[UpgradeButtonAutoBuilder] prefab/config ����");
            return;
        }

        Clear(costParent); Clear(jobParent); Clear(originParent);

        // 1) �ڽ�Ʈ
        var costList = (costKeys != null && costKeys.Count > 0)
            ? new List<int>(costKeys)
            : upgradeConfig.costCurves?.Select(c => c.costKey).OrderBy(x => x).ToList();

        if (costList != null)
        {
            foreach (var c in costList)
            {
                UpgradeAddButton.Create(buttonPrefab, costParent,
                    u => u.ConfigureCost(c, defaultDelta));
            }
        }

        // 2) ��
        IEnumerable<JobSynergy> jobList =
            (mapJobs != null && mapJobs.Count > 0)
            ? mapJobs
            : (upgradeConfig.jobCurves != null && upgradeConfig.jobCurves.Count > 0
                ? upgradeConfig.jobCurves.Keys
                : Enum.GetValues(typeof(JobSynergy)).Cast<JobSynergy>());

        foreach (var j in jobList)
        {
            if (!IsSingleFlag(j) || j == JobSynergy.None) continue;
            // �� �ʿ� �������� �ʴ� �ó����� ���ʿ� ��Ͽ� �� ������ ��
            UpgradeAddButton.Create(buttonPrefab, jobParent,
                u => u.ConfigureJob(j, defaultDelta));
        }

        // 3) ������
        IEnumerable<OriginSynergy> originList =
            (mapOrigins != null && mapOrigins.Count > 0)
            ? mapOrigins
            : (upgradeConfig.originCurves != null && upgradeConfig.originCurves.Count > 0
                ? upgradeConfig.originCurves.Keys
                : Enum.GetValues(typeof(OriginSynergy)).Cast<OriginSynergy>());

        foreach (var o in originList)
        {
            if (!IsSingleFlag(o) || o == OriginSynergy.None) continue;
            UpgradeAddButton.Create(buttonPrefab, originParent,
                u => u.ConfigureOrigin(o, defaultDelta));
        }
    }

    private static void Clear(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; --i)
            DestroyImmediate(t.GetChild(i).gameObject);
    }

    private static bool IsSingleFlag<TEnum>(TEnum value) where TEnum : Enum
    {
        long v = Convert.ToInt64(value);
        return v != 0 && (v & (v - 1)) == 0;
    }

    private void Awake()
    {
        // ���ϸ� �ڵ� ����
        Rebuild();
        Debug.Log($"������ ���� ���۽� ��ư ����, ���� �ν����ͷ� �׳� ���� �س��� ��ư �̻ڰ� �ٹ̱�");
    }
}
