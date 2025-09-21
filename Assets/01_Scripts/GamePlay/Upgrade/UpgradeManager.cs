using System;
using System.Collections.Generic;
using System.Linq;             // Dump용
using UnityEngine;

public class UpgradeManager : MonoSingleton<UpgradeManager>
{
    [Header("설정")]
    [SerializeField] private UpgradeConfig config;
    public UpgradeConfig CurrentConfig => config;

    [Header("Debug")]
    [SerializeField] private bool logOnSet = true;   // 단계 변경 로그
    [SerializeField] private bool logOnCalc = true;  // 배율/Flat 계산 로그

    private int _rev = 0; // 변경 Revision

    [SerializeField] private bool logStageChange = false;

    // 상태
    private readonly Dictionary<int, int> costStage = new();
    private readonly Dictionary<JobSynergy, int> jobStage = new();
    private readonly Dictionary<OriginSynergy, int> originStage = new();

    public event Action OnUpgradeChanged;

    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"[UpgradeManager] Awake on {name} | config={(config ? config.name : "NULL")}");
        // 초기 알림은 필요 시만
    }

    // 스테이지 여러개 둘때 사용
    public void SetConfig(UpgradeConfig cfg, bool resetStages)
    {
        config = cfg;
        if (resetStages) { costStage.Clear(); jobStage.Clear(); originStage.Clear(); }
        OnUpgradeChanged?.Invoke();  // 유닛들 즉시 재계산
    }

    public int GetCostStage(int cost) => costStage.TryGetValue(cost, out var s) ? s : 0;
    public int GetJobStage(JobSynergy key) => jobStage.TryGetValue(key, out var s) ? s : 0;
    public int GetOriginStage(OriginSynergy key) => originStage.TryGetValue(key, out var s) ? s : 0;

    public void SetCostStage(int cost, int stage)
    {
        int prev = GetCostStage(cost);
        costStage[cost] = Mathf.Clamp(stage, 0, 5);
        if (logOnSet) Debug.Log($"[UpgradeManager] SetCostStage cost={cost} {prev}→{costStage[cost]}");
        if (logStageChange) Debug.Log($"[Upgrade-Set] Cost {cost} → s{costStage[cost]}");
        NotifyChanged($"cost={cost}");
    }
    public void SetJobStage(JobSynergy key, int stage)
    {
        int prev = GetJobStage(key);
        jobStage[key] = Mathf.Clamp(stage, 0, 5);
        if (logOnSet) Debug.Log($"[UpgradeManager] SetJobStage {key} {prev}→{jobStage[key]}");
        NotifyChanged($"job={key}");
    }
    public void SetOriginStage(OriginSynergy key, int stage)
    {
        int prev = GetOriginStage(key);
        originStage[key] = Mathf.Clamp(stage, 0, 5);
        if (logOnSet) Debug.Log($"[UpgradeManager] SetOriginStage {key} {prev}→{originStage[key]}");
        NotifyChanged($"origin={key}");
    }

    public void AddCostStage(int cost, int delta = 1) => SetCostStage(cost, GetCostStage(cost) + delta);
    public void AddJobStage(JobSynergy key, int delta = 1) => SetJobStage(key, GetJobStage(key) + delta);
    public void AddOriginStage(OriginSynergy key, int d = 1) => SetOriginStage(key, GetOriginStage(key) + d);

    public void ResetAll()
    {
        costStage.Clear(); jobStage.Clear(); originStage.Clear();
        NotifyChanged("reset");
    }

    private void NotifyChanged(string reason)
    {
        unchecked { _rev++; }
        if (logOnSet)
        {
            Debug.Log($"[UpgradeManager] Changed(reason={reason}) rev={_rev} | {DumpStages()} | subs={OnUpgradeChanged?.GetInvocationList()?.Length ?? 0}");
        }
        OnUpgradeChanged?.Invoke();
    }

    private string DumpStages()
    {
        string c = costStage.Count > 0 ? string.Join(",", costStage.Select(kv => $"{kv.Key}=s{kv.Value}")) : "-";
        string j = jobStage.Count > 0 ? string.Join(",", jobStage.Select(kv => $"{kv.Key}=s{kv.Value}")) : "-";
        string o = originStage.Count > 0 ? string.Join(",", originStage.Select(kv => $"{kv.Key}=s{kv.Value}")) : "-";
        return $"cost[{c}] job[{j}] origin[{o}]";
    }

    // === 계산부 ===
    public (float attackMul, float attackSpeedMul) GetFinalMultipliers(UnitData data)
    {
        float atkPctSum = 0f, aspdPctSum = 0f;
        if (config == null || data == null)
        {
            if (logOnCalc) Debug.Log($"[UpgradeManager] GetFinalMultipliers: config/data NULL (unit={(data ? data.unitName : "NULL")})");
            return (1f, 1f);
        }

        // 1) 코스트
        var costCurve = config.FindCostCurve(data.cost);
        if (costCurve.HasValue)
        {
            int stage = GetCostStage(data.cost);
            var b = costCurve.Value.GetBonus(stage);
            atkPctSum += b.atkPct; aspdPctSum += b.aspdPct;
            if (logOnCalc) Debug.Log($"[UpgradeManager] Mul-Cost unit={data.unitName} cost={data.cost} s{stage} → +ATK% {b.atkPct:P1}, +ASPD% {b.aspdPct:P1}");
        }
        else if (logOnCalc)
        {
            Debug.Log($"[UpgradeManager] Mul-Cost unit={data.unitName} cost={data.cost} curve=NONE");
        }

        // 2) Job
        foreach (JobSynergy flag in Enum.GetValues(typeof(JobSynergy)))
        {
            if (flag == JobSynergy.None || !data.jobs.HasFlag(flag)) continue;
            int stage = GetJobStage(flag);
            if (stage <= 0) continue;
            if (config.jobCurves != null && config.jobCurves.TryGetValue(flag, out var curve))
            {
                var b = curve.GetBonus(stage);
                atkPctSum += b.atkPct; aspdPctSum += b.aspdPct;
                if (logOnCalc) Debug.Log($"[UpgradeManager] Mul-Job  unit={data.unitName} {flag} s{stage} → +ATK% {b.atkPct:P1}, +ASPD% {b.aspdPct:P1}");
            }
        }

        // 3) Origin
        foreach (OriginSynergy flag in Enum.GetValues(typeof(OriginSynergy)))
        {
            if (flag == OriginSynergy.None || !data.origins.HasFlag(flag)) continue;
            int stage = GetOriginStage(flag);
            if (stage <= 0) continue;
            if (config.originCurves != null && config.originCurves.TryGetValue(flag, out var curve))
            {
                var b = curve.GetBonus(stage);
                atkPctSum += b.atkPct; aspdPctSum += b.aspdPct;
                if (logOnCalc) Debug.Log($"[UpgradeManager] Mul-Origin unit={data.unitName} {flag} s{stage} → +ATK% {b.atkPct:P1}, +ASPD% {b.aspdPct:P1}");
            }
        }

        var result = (1f + atkPctSum, 1f + aspdPctSum);
        if (logOnCalc) Debug.Log($"[UpgradeManager] Mul-Sum   unit={data.unitName} → xATK {result.Item1:F2}, xASPD {result.Item2:F2}");
        return result;
    }

    public int GetFinalAttackFlat(UnitData data)
    {
        Debug.Log(gameObject.name);
        if (config == null || data == null)
        {
            if (logOnCalc) Debug.Log($"[UpgradeManager] GetFinalAttackFlat: config/data NULL (unit={(data ? data.unitName : "NULL")})");
            return 0;
        }
        int sum = 0;

        var cc = config.FindCostCurve(data.cost);
        if (cc.HasValue)
        {
            int st = GetCostStage(data.cost);
            int f = cc.Value.GetFlatAttack(st);
            sum += f;
            if (logOnCalc) Debug.Log($"[UpgradeManager] Flat-Cost unit={data.unitName} cost={data.cost} s{st} → +{f}");
        }

        foreach (JobSynergy flag in Enum.GetValues(typeof(JobSynergy)))
        {
            if (flag == JobSynergy.None || !data.jobs.HasFlag(flag)) continue;
            int st = GetJobStage(flag);
            if (st <= 0) continue;
            if (config.jobCurves != null && config.jobCurves.TryGetValue(flag, out var curve))
            {
                int f = curve.GetFlatAttack(st);
                sum += f;
                if (logOnCalc) Debug.Log($"[UpgradeManager] Flat-Job  unit={data.unitName} {flag} s{st} → +{f}");
            }
        }

        foreach (OriginSynergy flag in Enum.GetValues(typeof(OriginSynergy)))
        {
            if (flag == OriginSynergy.None || !data.origins.HasFlag(flag)) continue;
            int st = GetOriginStage(flag);
            if (st <= 0) continue;
            if (config.originCurves != null && config.originCurves.TryGetValue(flag, out var curve))
            {
                int f = curve.GetFlatAttack(st);
                sum += f;
                if (logOnCalc) Debug.Log($"[UpgradeManager] Flat-Origin unit={data.unitName} {flag} s{st} → +{f}");
            }
        }

        if (logOnCalc) Debug.Log($"[UpgradeManager] Flat-Sum  unit={data.unitName} → +{sum}");
        return sum;
    }

    public struct UpgradeBreakdown
    {
        public float atkPctSum;
        public float aspdPctSum;
        public int flatSum;
        public List<string> lines;
    }

    public UpgradeBreakdown BuildBreakdown(UnitData data)
    {
        var r = new UpgradeBreakdown { lines = new List<string>() };
        if (config == null || data == null) return r;

        // 1) 코스트
        var costCurve = config.FindCostCurve(data.cost);
        if (costCurve.HasValue)
        {
            int st = GetCostStage(data.cost);
            var b = costCurve.Value.GetBonus(st);
            int f = costCurve.Value.GetFlatAttack(st);
            r.atkPctSum += b.atkPct;
            r.aspdPctSum += b.aspdPct;
            r.flatSum += f;
            if (st > 0)
                r.lines.Add($"Cost {data.cost} (s{st}): Flat +{f}, ATK +{b.atkPct:P1}, ASPD +{b.aspdPct:P1}");
        }

        // 2) Job
        foreach (JobSynergy flag in Enum.GetValues(typeof(JobSynergy)))
        {
            if (flag == JobSynergy.None || !data.jobs.HasFlag(flag)) continue;
            int st = GetJobStage(flag);
            if (st <= 0) continue;

            if (config.jobCurves != null && config.jobCurves.TryGetValue(flag, out var curve))
            {
                var b = curve.GetBonus(st);
                int f = curve.GetFlatAttack(st);
                r.atkPctSum += b.atkPct;
                r.aspdPctSum += b.aspdPct;
                r.flatSum += f;
                r.lines.Add($"Job {flag} (s{st}): Flat +{f}, ATK +{b.atkPct:P1}, ASPD +{b.aspdPct:P1}");
            }
        }

        // 3) Origin
        foreach (OriginSynergy flag in Enum.GetValues(typeof(OriginSynergy)))
        {
            if (flag == OriginSynergy.None || !data.origins.HasFlag(flag)) continue;
            int st = GetOriginStage(flag);
            if (st <= 0) continue;

            if (config.originCurves != null && config.originCurves.TryGetValue(flag, out var curve))
            {
                var b = curve.GetBonus(st);
                int f = curve.GetFlatAttack(st);
                r.atkPctSum += b.atkPct;
                r.aspdPctSum += b.aspdPct;
                r.flatSum += f;
                r.lines.Add($"Origin {flag} (s{st}): Flat +{f}, ATK +{b.atkPct:P1}, ASPD +{b.aspdPct:P1}");
            }
        }

        return r;
    }
}
