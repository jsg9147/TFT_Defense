using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SynergySummaryUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Transform rootContainer;   // 왼쪽 패널 Content
    [SerializeField] private GameObject rowPrefab;      // SynergyRowUI 프리팹

    [Header("표시 정책")]
    [SerializeField] private bool hideZeroCount = true;   // 0개 숨김
    [SerializeField] private bool showOnlyActive = false; // 활성만 표시

    [Header("티어 컷 (TFT 스타일)")]
    [SerializeField] private int[] jobThresholds = { 2, 4, 6 }; // 직업
    [SerializeField] private int[] originThresholds = { 1, 3, 5 }; // 기원

    private void OnEnable()
    {
        if (SynergyManager.Instance != null)
        {
            SynergyManager.Instance.OnSynergyChanged += Refresh;
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (SynergyManager.Instance != null)
            SynergyManager.Instance.OnSynergyChanged -= Refresh;
    }

    private void Refresh()
    {
        var sm = SynergyManager.Instance;
        if (sm == null || rootContainer == null || rowPrefab == null) return;

        Clear(rootContainer);
        var entries = new List<Entry>(16);

        // 직업
        foreach (JobSynergy j in Enum.GetValues(typeof(JobSynergy)))
        {
            if (j == JobSynergy.None) continue;
            int count = sm.GetCount(j);
            bool active = sm.IsActive(j);
            if (hideZeroCount && count <= 0) continue;
            if (showOnlyActive && !active) continue;

            entries.Add(new Entry
            {
                label = ToKorean(j),
                count = count,
                active = active,
                thresholds = sm.JobThresholds as int[] ?? new List<int>(sm.JobThresholds).ToArray()
            });
        }

        // 기원
        foreach (OriginSynergy o in Enum.GetValues(typeof(OriginSynergy)))
        {
            if (o == OriginSynergy.None) continue;
            int count = sm.GetCount(o);
            bool active = sm.IsActive(o);
            if (hideZeroCount && count <= 0) continue;
            if (showOnlyActive && !active) continue;

            entries.Add(new Entry
            {
                label = ToKorean(o),
                count = count,
                active = active,
                thresholds = sm.OriginThresholds as int[] ?? new List<int>(sm.OriginThresholds).ToArray()
            });
        }

        entries.Sort(CompareEntries);

        foreach (var e in entries)
        {
            var go = Instantiate(rowPrefab, rootContainer);
            var row = go.GetComponent<SynergyRowUI>();
            if (row != null)
            {
                row.Set(e.label, e.count, e.thresholds); // 기존 그대로
                                                         // (선택) 활성화 하이라이트가 필요하면 row.SetActive(e.active) 같은 API 추가
            }
            else
            {
                var txt = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt) txt.text = $"{e.label}  x{e.count}  ({string.Join(" > ", e.thresholds)})";
            }
        }
    }

    // --- Helpers ---
    private struct Entry
    {
        public string label;
        public int count;
        public bool active;
        public int[] thresholds;
    }

    private static int CompareEntries(Entry a, Entry b)
    {
        if (a.active != b.active) return b.active.CompareTo(a.active);

        int ta = TierOf(a.count, a.thresholds);
        int tb = TierOf(b.count, b.thresholds);
        if (ta != tb) return tb.CompareTo(ta);

        if (a.count != b.count) return b.count.CompareTo(a.count);
        return string.Compare(a.label, b.label, StringComparison.Ordinal);
    }

    private static int TierOf(int count, int[] th)
    {
        if (th == null || th.Length == 0) return 0;
        int tier = 0;
        for (int i = 0; i < th.Length; i++)
        {
            if (count >= th[i]) tier++;
            else break;
        }
        return tier;
    }

    private static void Clear(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private static string ToKorean(JobSynergy j) => j switch
    {
        JobSynergy.Warrior => "전사",
        JobSynergy.Mage => "마법사",
        JobSynergy.Ranger => "레인저",
        JobSynergy.Assassin => "암살자",
        JobSynergy.Guardian => "가디언",
        JobSynergy.Support => "서포터",
        JobSynergy.Engineer => "엔지니어",
        JobSynergy.Summoner => "소환사",
        _ => j.ToString()
    };

    private static string ToKorean(OriginSynergy o) => o switch
    {
        OriginSynergy.Kingdom => "왕국",
        OriginSynergy.Undead => "언데드",
        OriginSynergy.Beast => "야수",
        OriginSynergy.Mech => "메카",
        OriginSynergy.Spirit => "정령",
        OriginSynergy.Void => "공허",
        OriginSynergy.Goblin => "고블린",
        OriginSynergy.Slime => "슬라임",
        _ => o.ToString()
    };
}
