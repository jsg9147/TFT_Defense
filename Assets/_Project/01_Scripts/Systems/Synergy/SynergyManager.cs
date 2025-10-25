using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // 추가

public class SynergyManager : MonoSingleton<SynergyManager>
{
    [Header("중복 집계 옵션")]
    [SerializeField] private bool countSameUnitDataOnce = true; // 같은 유닛(같은 UnitData)은 1회만 카운트

    [Header("티어 컷 (TFT 스타일) — 단일 소스")]
    [SerializeField] private int[] jobThresholds = { 2, 4, 6 };
    [SerializeField] private int[] originThresholds = { 1, 3, 5 };

    public readonly Dictionary<JobSynergy, int> JobCounts = new();
    public readonly Dictionary<OriginSynergy, int> OriginCounts = new();

    public IReadOnlyList<int> JobThresholds => jobThresholds;
    public IReadOnlyList<int> OriginThresholds => originThresholds;

    private readonly HashSet<JobSynergy> activeJobs = new();
    private readonly HashSet<OriginSynergy> activeOrigins = new();

    private readonly List<Unit> roster = new(); // 벤치+필드 소속 유닛

    public event Action OnSynergyChanged;

    public int TierOfJob(int count) => TierOf(count, jobThresholds);
    public int TierOfOrigin(int count) => TierOf(count, originThresholds);

    protected override void Awake()
    {
        base.Awake();
        // 필요하면 DontDestroyOnLoad(this.gameObject);  // 여러 씬을 돌릴 때만
    }

    private void OnEnable()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnPhaseChanged += OnPhaseChanged;
        }

        // Bench/필드 이벤트 연결 (네 시스템에 맞춰 실제 이벤트에 바인딩)
        // BenchManager.OnUnitAdded += RegisterUnit;
        // BenchManager.OnUnitRemoved += UnregisterUnit;
        // Unit.OnEvolved += _ => DebouncedRecalc();
    }

    private void OnDisable()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    private static int TierOf(int count, IReadOnlyList<int> th)
    {
        int tier = 0;
        if (th == null) return 0;
        for (int i = 0; i < th.Count; i++)
        {
            if (count >= th[i]) tier++;
            else break;
        }
        return tier;
    }

    private void OnPhaseChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Battle)
        {
            Recalculate();
            ApplySynergies();
        }
        else if (state == GameManager.GameState.Shop || state == GameManager.GameState.Prepare)
        {
            RemoveSynergies(); // 정책에 따라 유지/해제 결정
        }
    }

    public void RegisterUnit(Unit u)
    {
        if (!u) return;
        if (roster.Contains(u)) return;
        roster.Add(u);
        DebouncedRecalc();
    }

    // 유닛 제거
    public void UnregisterUnit(Unit u)
    {
        if (!u) return;
        if (roster.Remove(u))
            DebouncedRecalc();
    }

    void DebouncedRecalc()
    {
        // 필요하면 코루틴으로 1프레임 모아치기
        Recalculate();
        OnSynergyChanged?.Invoke();
    }

    public void Recalculate()
    {
        JobCounts.Clear();
        OriginCounts.Clear();

        // 1) 유효 유닛만 뽑고
        var units = roster.Where(u => u != null && u.data != null);

        // 2) 같은 UnitData는 1개만 남기기 (옵션)
        IEnumerable<UnitData> unitDatas = countSameUnitDataOnce
            ? units.Select(u => u.data).Distinct()     // 같은 SO는 한 번만
            : units.Select(u => u.data);               // 예전 방식(복수 카운트)

        // 3) Distinct된 UnitData들을 기준으로 시너지 카운트
        foreach (var data in unitDatas)
        {
            var jd = data.jobs;
            var od = data.origins;

            foreach (JobSynergy j in Enum.GetValues(typeof(JobSynergy)))
            {
                if (j == JobSynergy.None) continue;
                if (jd.HasAny(j))
                    JobCounts[j] = JobCounts.TryGetValue(j, out var c) ? c + 1 : 1;
            }

            foreach (OriginSynergy o in Enum.GetValues(typeof(OriginSynergy)))
            {
                if (o == OriginSynergy.None) continue;
                if (od.HasAny(o))
                    OriginCounts[o] = OriginCounts.TryGetValue(o, out var c) ? c + 1 : 1;
            }
        }

        activeJobs.Clear();
        foreach (var kv in JobCounts)
            if (MeetsJobTier(kv.Key, kv.Value)) activeJobs.Add(kv.Key);

        activeOrigins.Clear();
        foreach (var kv in OriginCounts)
            if (MeetsOriginTier(kv.Key, kv.Value)) activeOrigins.Add(kv.Key);
    }

    // 시너지 추가데미지 계산
    public SynergySnapshot GetSnapshotFor(Unit u)
    {
        var s = SynergySnapshot.Default;

        // --- 직업 예시(2/4/6 단순 배율) ---
        int mage = GetCount(JobSynergy.Mage);
        if (mage >= 2) s.magicMul *= 1.10f;
        if (mage >= 4) s.magicMul *= 1.15f;   // 총 1.265x
        if (mage >= 6) s.magicMul *= 1.20f;   // 총 1.518x

        int warrior = GetCount(JobSynergy.Warrior);
        if (warrior >= 2) s.physMul *= 1.10f;
        if (warrior >= 4) s.physMul *= 1.15f;

        int ranger = GetCount(JobSynergy.Ranger);
        if (ranger >= 2) s.critAdd += 0.05f;
        if (ranger >= 4) s.critMultAdd += 0.10f;

        int assassin = GetCount(JobSynergy.Assassin);
        if (assassin >= 2) s.armorPenAdd += 0.25f;

        // --- 기원 예시(1/3/5 트리거) ---
        if (IsActive(OriginSynergy.Mech)) s.armorPenAdd += 0.10f;
        if (IsActive(OriginSynergy.Void)) s.magicPenAdd += 0.10f;
        if (IsActive(OriginSynergy.Slime) && u.data.jobs.HasAny(JobSynergy.Summoner))
            s.flatAdd += 3f;

        return s;
    }

    bool MeetsJobTier(JobSynergy flag, int count) => count >= 2; // 2/4/6은 나중에 테이블화
    bool MeetsOriginTier(OriginSynergy flag, int c) => c >= 1;     // 1/3/5도 테이블로

    void ApplySynergies()
    {
        // 전역 보정치 구성해서 유닛에 전달하거나, 데미지 페이로드 만들 때 참조
        // 예) Mage 활성 → GlobalMagicMul = 1.10f;
    }

    void RemoveSynergies()
    {
        // 전투 종료/상점 진입 시 초기화 정책
    }

    public bool IsActive(JobSynergy j) => activeJobs.Contains(j);
    public bool IsActive(OriginSynergy o) => activeOrigins.Contains(o);
    public int GetCount(JobSynergy j) => JobCounts.TryGetValue(j, out var c) ? c : 0;
    public int GetCount(OriginSynergy o) => OriginCounts.TryGetValue(o, out var c) ? c : 0;
}
public struct SynergySnapshot
{
    public float physMul;     // 물리 피해 배율
    public float magicMul;    // 마법 피해 배율
    public float areaMul;     // 광역 피해 배율
    public float trueMul;     // 트루 피해 배율
    public float flatAdd;     // 고정 피해 가산

    public float critAdd;       // 치확 가산(0~1)
    public float critMultAdd;   // 치피 가산(예: +0.2 = 1.7배)
    public float armorPenAdd;   // 물리 관통(0~1)
    public float magicPenAdd;   // 마법 관통(0~1)

    public static SynergySnapshot Default => new SynergySnapshot
    {
        physMul = 1f,
        magicMul = 1f,
        areaMul = 1f,
        trueMul = 1f,
        flatAdd = 0f,
        critAdd = 0f,
        critMultAdd = 0f,
        armorPenAdd = 0f,
        magicPenAdd = 0f
    };
}