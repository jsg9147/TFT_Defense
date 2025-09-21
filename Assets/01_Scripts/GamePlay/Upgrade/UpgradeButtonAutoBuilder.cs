using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

public class UpgradeButtonAutoBuilder : MonoBehaviour
{
    [Header("데이터 소스")]
    [SerializeField] private UpgradeConfig upgradeConfig;

    [Tooltip("이 맵에 실제로 등장하는 잡 시너지 (비우면 config의 key 사용)")]
    [SerializeField] private List<JobSynergy> mapJobs;

    [Tooltip("이 맵에 실제로 등장하는 오리진 시너지 (비우면 config의 key 사용)")]
    [SerializeField] private List<OriginSynergy> mapOrigins;

    [Tooltip("표시할 코스트 목록 (비우면 config.costCurves의 key 사용, 오름차순)")]
    [SerializeField] private List<int> costKeys;

    [Header("프리팹 & 부모")]
    [SerializeField] private UpgradeAddButton buttonPrefab;
    [SerializeField] private Transform costParent;
    [SerializeField] private Transform jobParent;
    [SerializeField] private Transform originParent;

    [Header("버튼 옵션")]
    [SerializeField, Min(1)] private int defaultDelta = 1;

#if ODIN_INSPECTOR
    [Button("Rebuild")]
#endif
    public void Rebuild()
    {
        if (buttonPrefab == null || upgradeConfig == null)
        {
            Debug.LogWarning("[UpgradeButtonAutoBuilder] prefab/config 누락");
            return;
        }

        Clear(costParent); Clear(jobParent); Clear(originParent);

        // 1) 코스트
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

        // 2) 잡
        IEnumerable<JobSynergy> jobList =
            (mapJobs != null && mapJobs.Count > 0)
            ? mapJobs
            : (upgradeConfig.jobCurves != null && upgradeConfig.jobCurves.Count > 0
                ? upgradeConfig.jobCurves.Keys
                : Enum.GetValues(typeof(JobSynergy)).Cast<JobSynergy>());

        foreach (var j in jobList)
        {
            if (!IsSingleFlag(j) || j == JobSynergy.None) continue;
            // 이 맵에 등장하지 않는 시너지는 애초에 목록에 안 넣으면 됨
            UpgradeAddButton.Create(buttonPrefab, jobParent,
                u => u.ConfigureJob(j, defaultDelta));
        }

        // 3) 오리진
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
        // 원하면 자동 빌드
        Rebuild();
        Debug.Log($"지금은 게임 시작시 버튼 생성, 추후 인스펙터로 그냥 생성 해놓고 버튼 이쁘게 꾸미기");
    }
}
