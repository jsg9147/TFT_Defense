using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Reflection;

public class Unit : MonoBehaviour
{
    [Header("Range Detector(자동 생성/연결)")]
    [SerializeField] private UnitRangeDetector rangeDetectorPrefab; // 비워도 자동 생성 가능
    [SerializeField] private UnitRangeDetector rangeDetectorRef;

    [Header("기본 구성")]
    public CanvasGroup canvasGroup;
    public Transform firePoint;

    [Header("기본 데이터")]
    public UnitData data;
    public GameObject bulletPrefab; // 구버전 호환용 (없으면 data.projectilePrefab 사용)

    [Header("상태")]
    public int starLevel = 1;

    // === 디버그 토글 ===
    [Header("Debug")]
    [SerializeField] private bool logOnApply = false;     // 적용 한 줄 요약
    [SerializeField] private bool logBreakdown = false;    // 축별(코스트/직업/오리진) 기여도 상세

    private float attackCooldown;
    private float lastAttackTime;
    private readonly List<Monster> monstersInRange = new();

    // === 추가: 업그레이드 배율 캐시 ===
    private float _atkMul = 1f;
    private float _aspdMul = 1f;

#if UNITY_EDITOR
    private void Reset() { EnsureRangeDetector(); }
    private void OnValidate() { if (Application.isEditor && !Application.isPlaying) EnsureRangeDetector(); }
#endif
    // === 추가: 이벤트 구독/해제 ===
    private void OnEnable()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradeChanged += HandleUpgradeChanged;

        // 유닛이 활성화될 때도 한 번 적용 (data가 이미 세팅되어 있으면 바로 반영)
        ApplyUpgradesNow();
    }

    private void OnDisable()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradeChanged -= HandleUpgradeChanged;
    }

    private void HandleUpgradeChanged()
    {
        ApplyUpgradesNow();
    }


    private void Start()
    {
        // 기존
        // attackCooldown = 1f / Mathf.Max(0.01f, data.attackSpeed);

        // === 변경: 배율 포함(초기엔 _aspdMul=1f 이지만 이벤트 직후에도 일관성 유지) ===
        attackCooldown = 1f / Mathf.Max(0.01f, data.attackSpeed * _aspdMul);

        var col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = data.range;
        }
    }


    public void Init(UnitData unitData)
    {
        data = unitData;

        if(canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>();
        ApplyUpgradesNow();

        // (선택) 콜라이더 반경도 갱신하고 싶으면:
        var col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = data.range;
        }
        EnsureRangeDetector();
    }
    private void EnsureRangeDetector()
    {
        if (!rangeDetectorRef)
        {
            // 자식에서 먼저 찾기
            rangeDetectorRef = GetComponentInChildren<UnitRangeDetector>(true);
            if (!rangeDetectorRef)
            {
                // 없으면 생성
                var go = new GameObject("RangeDetector");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;

                rangeDetectorRef = go.AddComponent<UnitRangeDetector>();
                rangeDetectorRef.unit = this;

                // 필수 컴포넌트는 UnitRangeDetector가 Require로 자동 부착됨
            }
        }
        // 반경 동기화
        rangeDetectorRef.unit = this;
        rangeDetectorRef.SyncRadius();
    }

    private void Update()
    {
        // 무효 타겟 정리
        monstersInRange.RemoveAll(m => m == null || !m.gameObject.activeInHierarchy);
        if (monstersInRange.Count == 0) return;

        if (Time.time < lastAttackTime + attackCooldown) return;

        // 패턴 결정
        if (Has(data.types, UnitType.Area))
            FireArea();
        else if (Has(data.types, UnitType.MultiShot))
            FireMultiShot();
        else if (Has(data.types, UnitType.Chain))
            FireChain();
        else // 기본: 단일
            FireSingleShot();

        lastAttackTime = Time.time;
    }

    // ===== 패턴 구현 =====

    // Unit.cs 변경 포인트만 발췌

    // === 발사 로직 ===
    private void FireSingleShot()
    {
        var target = SelectPrimaryTarget();
        if (!target) return;

        if (ShouldUseProjectile())
        {
            SpawnProjectile(target.transform);   // 즉시 데미지 X
        }
        else
        {
            ApplyHit(target);                    // 투사체 미사용(근접 등)일 때만 즉시 데미지
        }
    }

    private void FireMultiShot()
    {
        var targets = GetSortedTargetsByDistance().Take(Mathf.Max(1, data.multishotCount)).ToList();
        if (targets.Count == 0) return;

        if (ShouldUseProjectile())
        {
            foreach (var t in targets)
                SpawnProjectile(t.transform);    // 즉시 데미지 X
        }
        else
        {
            foreach (var t in targets)
                ApplyHit(t);
        }
    }

    private void FireArea()
    {
        var center = SelectPrimaryTarget();
        if (!center) return;

        if (ShouldUseProjectile())
        {
            // AOE는 탄이 명중한 지점에서 Bullet이 반경 타격 처리
            SpawnProjectile(center.transform);
        }
        else
        {
            var hits = Physics2D.OverlapCircleAll(center.transform.position, data.areaRadius)
                                .Select(c => c.GetComponent<Monster>())
                                .Where(m => m != null).ToList();
            foreach (var m in hits) ApplyHit(m);
        }
    }

    private void FireChain()
    {
        var first = SelectPrimaryTarget();
        if (!first) return;

        if (ShouldUseProjectile())
        {
            // 체인은 첫 타겟으로 쏘고, 이후 바운스는 Bullet이 처리
            SpawnProjectile(first.transform);
        }
        else
        {
            var current = first;
            var visited = new HashSet<Monster>() { current };
            for (int i = 0; i < Mathf.Max(1, data.chainCount); i++)
            {
                ApplyHit(current);
                var next = FindNearestMonster(current.transform.position, data.chainRange, visited);
                if (next == null) break;
                visited.Add(next);
                current = next;
            }
        }
    }

    // === 투사체 스폰 ===
    // (서명 변경) _legacyDamage 제거, attacker(this) 전달
    private void SpawnProjectile(Transform target)
    {
        var prefab = data.projectilePrefab != null ? data.projectilePrefab : bulletPrefab;
        if (!prefab || !firePoint) return;

        var go = Instantiate(prefab, firePoint.position, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        if (b != null)
            b.Initialize(target, /*payload:*/ default, /*attacker:*/ this); // payload는 Bullet에서 명중 시 생성
    }

    // === Bullet이 쓸 수 있도록 공개 ===
    public DamagePayload BuildImpactPayload()
    {
        int baseDmg = GetAttackDamage();
        var type = Has(data.types, UnitType.Magic) ? DamageType.Magic
                 : Has(data.types, UnitType.Elemental) ? DamageType.Magic
                 : DamageType.Physical;

        return new DamagePayload
        {
            BaseDamage = baseDmg,
            Type = type,
            CritChance = 0.1f,
            CritMultiplier = 1.5f,
            ArmorPen = 0f,
            MagicPen = 0f,
            Source = this
        };
    }


    // ===== 타겟/유틸 =====

    private Monster SelectPrimaryTarget()
    {
        // 간단: 가장 가까운 타겟
        return GetSortedTargetsByDistance().FirstOrDefault();
    }

    private IEnumerable<Monster> GetSortedTargetsByDistance()
    {
        var pos = transform.position;
        return monstersInRange.OrderBy(m => Vector2.SqrMagnitude((Vector2)m.transform.position - (Vector2)pos));
    }

    private Monster FindNearestMonster(Vector3 from, float range, HashSet<Monster> except)
    {
        Monster best = null;
        float bestDist = float.MaxValue;

        foreach (var m in monstersInRange)
        {
            if (m == null || except.Contains(m)) continue;
            float d = Vector2.Distance(from, m.transform.position);
            if (d <= range && d < bestDist)
            {
                bestDist = d;
                best = m;
            }
        }
        return best;
    }

    private bool ShouldUseProjectile()
    {
        // 근접이든 원거리든 일단 발사체 우선 사용하고 싶다면 true 반환
        // 지금은 prefab 유무로 판단
        return (data.projectilePrefab != null) || (bulletPrefab != null && data.projectilePrefab == null);
    }

    private void SpawnProjectile(Transform target, int _legacyDamage)
    {
        var prefab = data.projectilePrefab != null ? data.projectilePrefab : bulletPrefab;
        if (!prefab || !firePoint) return;

        var go = Instantiate(prefab, firePoint.position, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        if (b != null) b.Initialize(target, BuildBasicPayload());
    }


    private void ApplyHit(Monster target)
    {
        if (!target) return;

        var payload = BuildBasicPayload();  
        target.TakeDamage(payload);          

        if (Has(data.types, UnitType.Poison))
            StartCoroutine(CoPoison(target));
    }
    // ===== Unit.cs 안에 넣는 최종본 =====
    private DamagePayload BuildBasicPayload()
    {
        // 1) 기본 타입 판정
        DamageType type =
            Has(data.types, UnitType.Magic) ? DamageType.Magic :
            Has(data.types, UnitType.Elemental) ? DamageType.Magic : // 원소는 초기에 마법 취급
            Has(data.types, UnitType.Area) ? DamageType.Area :
                                                   DamageType.Physical;

        // 2) 기본 공격력
        float dmg = GetAttackDamage();

        // 3) 시너지 스냅샷 받아서 보정 (없으면 기본값)
        var snap = (SynergyManager.Instance != null)
            ? SynergyManager.Instance.GetSnapshotFor(this)
            : SynergySnapshot.Default;

        // 4) 타입별 배율 적용 + 평딜 가산
        dmg += snap.flatAdd;
        switch (type)
        {
            case DamageType.Physical: dmg *= snap.physMul; break;
            case DamageType.Magic: dmg *= snap.magicMul; break;
            case DamageType.Area: dmg *= snap.areaMul; break;
            case DamageType.True: dmg *= snap.trueMul; break;
        }

        // 5) 크리/관통 보정
        float critChance = Mathf.Clamp01(0.10f + snap.critAdd);   // 기본 10% + 시너지
        float critMultiplier = Mathf.Max(1f, 1.50f + snap.critMultAdd);
        float armorPen = Mathf.Clamp01(snap.armorPenAdd);
        float magicPen = Mathf.Clamp01(snap.magicPenAdd);

        // 6) 페이로드 구성
        return new DamagePayload
        {
            BaseDamage = Mathf.Max(1, Mathf.RoundToInt(dmg)),
            Type = type,
            CritChance = critChance,
            CritMultiplier = critMultiplier,
            ArmorPen = armorPen,
            MagicPen = magicPen,
            Source = this
        };
    }

    private DamageType DetermineDamageType()
    {
        if (Has(data.types, UnitType.Magic))
            return DamageType.Magic;
        if (Has(data.types, UnitType.Elemental))
            return DamageType.True;

        // AOE 패턴이면 피해 타입도 Area로 태깅(방어 50% 규칙 적용 목적)
        if (Has(data.types, UnitType.Area))
            return DamageType.Area;

        return DamageType.Physical;
    }

    // Poison(DoT)도 페이로드로 전송
    private System.Collections.IEnumerator CoPoison(Monster target)
    {
        int ticks = Mathf.Max(1, data.poisonTickCount);
        for (int i = 0; i < ticks; i++)
        {
            if (!target) yield break;

            var payload = new DamagePayload
            {
                BaseDamage = Mathf.Max(1, data.poisonDamagePerTick),
                Type = DamageType.True, // 초기엔 고정피해로 처리
                Source = this.gameObject
            };

            if (target is IDamageable dmg)
                dmg.TakeDamage(payload);

            yield return new WaitForSeconds(Mathf.Max(0.05f, data.poisonTickInterval));
        }
    }

    private int GetAttackDamage()
    {
        int baseDmg = data.baseAttack + (starLevel - 1) * 5;

        // === 추가: 업그레이드 고정 증가량 ===
        int flat = 0;
        if (UpgradeManager.Instance != null)
            flat = UpgradeManager.Instance.GetFinalAttackFlat(data);

        float scaled = (baseDmg + flat) * Mathf.Max(0f, _atkMul);
        return Mathf.Max(1, Mathf.RoundToInt(scaled));
    }


    // 범용 플래그 체크
    private static bool Has(UnitType mask, UnitType flag) => (mask & flag) == flag;

    // 사거리 감시
    public void AddMonsterInRange(Monster monster)
    {
        if (monster != null && !monstersInRange.Contains(monster))
            monstersInRange.Add(monster);
    }

    public void RemoveMonsterInRange(Monster monster)
    {
        if (monster != null) monstersInRange.Remove(monster);
    }

    public void SetHighlight(bool active)
    {
        if (canvasGroup != null) canvasGroup.alpha = active ? 0.7f : 1f;
    }

    // === 추가: 업그레이드 적용 본체 ===
    private void ApplyUpgradesNow()
    {
        if (data == null || UpgradeManager.Instance == null) return;

        var (atkMul, aspdMul) = UpgradeManager.Instance.GetFinalMultipliers(data);
        _atkMul = Mathf.Max(0f, atkMul);
        _aspdMul = Mathf.Max(0.0001f, aspdMul);

        // 공속 상한(선택)
        float aps = data.attackSpeed * _aspdMul;
        aps = Mathf.Min(aps, 5f); // 예: 초당 5타 상한

        attackCooldown = 1f / Mathf.Max(0.01f, aps);  // ← 이 줄만 남기고, 중복 계산 줄은 제거

        if (logOnApply || logBreakdown) LogUpgradeApplied();
    }
    // === 실제 로그 출력 ===
    private void LogUpgradeApplied()
    {
        var um = UpgradeManager.Instance;
        if (um == null) return;

        int baseDmg = data.baseAttack + (starLevel - 1) * 5;
        int flat = 0;
        if (um != null) flat = um.GetFinalAttackFlat(data);   // Flat 안 쓰면 0 반환

        float finalDmg = (baseDmg + flat) * _atkMul;
        float aps = Mathf.Min(data.attackSpeed * _aspdMul, 5f);
        float cd = 1f / Mathf.Max(0.01f, aps);

        if (logOnApply)
        {
            Debug.Log(
                $"[Upgrade][{name}] cost={data.cost}, jobs={data.jobs}, origins={data.origins} | " +
                $"ATK {baseDmg} + {flat} → {finalDmg:F1} (x{_atkMul:F2}), " +
                $"APS {data.attackSpeed:F2} → {aps:F2} (x{_aspdMul:F2}) | CD {cd:F3}s");
        }

        if (logBreakdown)
        {
            var bd = um.BuildBreakdown(data); // 아래 2) 참고
            var lines = string.Join("\n   ", bd.lines);
            Debug.Log(
                $"[Upgrade-Breakdown][{name}] sums: +ATK% {bd.atkPctSum:P1}, +ASPD% {bd.aspdPctSum:P1}, +Flat {bd.flatSum}\n   {lines}");
        }

        if (logBreakdown)
        {
            var bd = um.BuildBreakdown(data); // 이미 쓰고 있는 브레이크다운
            var lines = string.Join("\n   ", bd.lines);
            Debug.Log(
                $"[Upgrade-Breakdown][{name}] sums: +ATK% {bd.atkPctSum:P1}, +ASPD% {bd.aspdPctSum:P1}, +Flat {bd.flatSum}\n   {lines}");
#if UNITY_EDITOR
            LogCurvePresenceEditor();   // ← 커브 유무/스테이지 한 줄 요약
#endif
        }

    }

#if UNITY_EDITOR
    private void LogCurvePresenceEditor()
    {
        try
        {
            var um = UpgradeManager.Instance;
            if (um == null || data == null) return;

            // UpgradeManager의 private 'config' 읽기
            var cfgField = typeof(UpgradeManager).GetField("config",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var cfg = cfgField?.GetValue(um) as UpgradeConfig;
            bool hasCfg = cfg != null;

            bool hasCost = hasCfg && cfg.FindCostCurve(data.cost).HasValue;
            int stCost = um.GetCostStage(data.cost);

            var jobFlags = System.Enum.GetValues(typeof(JobSynergy))
                .Cast<JobSynergy>()
                .Where(f => f != JobSynergy.None && data.jobs.HasFlag(f));

            var originFlags = System.Enum.GetValues(typeof(OriginSynergy))
                .Cast<OriginSynergy>()
                .Where(f => f != OriginSynergy.None && data.origins.HasFlag(f));

            var sb = new System.Text.StringBuilder();
            sb.Append($"[Upgrade-CurvePresence][{name}] cfg={(hasCfg ? "OK" : "NULL")} | ");
            sb.Append($"Cost {data.cost}: s{stCost}, curve={hasCost} ; ");

            foreach (var jf in jobFlags)
            {
                bool has = hasCfg && cfg.jobCurves != null && cfg.jobCurves.ContainsKey(jf);
                int st = um.GetJobStage(jf);
                sb.Append($"Job {jf}: s{st}, curve={has} ; ");
            }
            foreach (var of in originFlags)
            {
                bool has = hasCfg && cfg.originCurves != null && cfg.originCurves.ContainsKey(of);
                int st = um.GetOriginStage(of);
                sb.Append($"Origin {of}: s{st}, curve={has} ; ");
            }

            sb.Append($" | atkMul={_atkMul:F2}, aspdMul={_aspdMul:F2}");
            Debug.Log(sb.ToString());
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Upgrade-CurvePresence] failed: {e.Message}");
        }
    }
#endif

}
