using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Reflection;

public class Unit : MonoBehaviour
{
    [Header("Range Detector(�ڵ� ����/����)")]
    [SerializeField] private UnitRangeDetector rangeDetectorPrefab; // ����� �ڵ� ���� ����
    [SerializeField] private UnitRangeDetector rangeDetectorRef;

    [Header("�⺻ ����")]
    public CanvasGroup canvasGroup;
    public Transform firePoint;

    [Header("�⺻ ������")]
    public UnitData data;
    public GameObject bulletPrefab; // ������ ȣȯ�� (������ data.projectilePrefab ���)

    [Header("����")]
    public int starLevel = 1;

    // === ����� ��� ===
    [Header("Debug")]
    [SerializeField] private bool logOnApply = false;     // ���� �� �� ���
    [SerializeField] private bool logBreakdown = false;    // �ະ(�ڽ�Ʈ/����/������) �⿩�� ��

    private float attackCooldown;
    private float lastAttackTime;
    private readonly List<Monster> monstersInRange = new();

    // === �߰�: ���׷��̵� ���� ĳ�� ===
    private float _atkMul = 1f;
    private float _aspdMul = 1f;

#if UNITY_EDITOR
    private void Reset() { EnsureRangeDetector(); }
    private void OnValidate() { if (Application.isEditor && !Application.isPlaying) EnsureRangeDetector(); }
#endif
    // === �߰�: �̺�Ʈ ����/���� ===
    private void OnEnable()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradeChanged += HandleUpgradeChanged;

        // ������ Ȱ��ȭ�� ���� �� �� ���� (data�� �̹� ���õǾ� ������ �ٷ� �ݿ�)
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
        // ����
        // attackCooldown = 1f / Mathf.Max(0.01f, data.attackSpeed);

        // === ����: ���� ����(�ʱ⿣ _aspdMul=1f ������ �̺�Ʈ ���Ŀ��� �ϰ��� ����) ===
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

        // (����) �ݶ��̴� �ݰ浵 �����ϰ� ������:
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
            // �ڽĿ��� ���� ã��
            rangeDetectorRef = GetComponentInChildren<UnitRangeDetector>(true);
            if (!rangeDetectorRef)
            {
                // ������ ����
                var go = new GameObject("RangeDetector");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;

                rangeDetectorRef = go.AddComponent<UnitRangeDetector>();
                rangeDetectorRef.unit = this;

                // �ʼ� ������Ʈ�� UnitRangeDetector�� Require�� �ڵ� ������
            }
        }
        // �ݰ� ����ȭ
        rangeDetectorRef.unit = this;
        rangeDetectorRef.SyncRadius();
    }

    private void Update()
    {
        // ��ȿ Ÿ�� ����
        monstersInRange.RemoveAll(m => m == null || !m.gameObject.activeInHierarchy);
        if (monstersInRange.Count == 0) return;

        if (Time.time < lastAttackTime + attackCooldown) return;

        // ���� ����
        if (Has(data.types, UnitType.Area))
            FireArea();
        else if (Has(data.types, UnitType.MultiShot))
            FireMultiShot();
        else if (Has(data.types, UnitType.Chain))
            FireChain();
        else // �⺻: ����
            FireSingleShot();

        lastAttackTime = Time.time;
    }

    // ===== ���� ���� =====

    // Unit.cs ���� ����Ʈ�� ����

    // === �߻� ���� ===
    private void FireSingleShot()
    {
        var target = SelectPrimaryTarget();
        if (!target) return;

        if (ShouldUseProjectile())
        {
            SpawnProjectile(target.transform);   // ��� ������ X
        }
        else
        {
            ApplyHit(target);                    // ����ü �̻��(���� ��)�� ���� ��� ������
        }
    }

    private void FireMultiShot()
    {
        var targets = GetSortedTargetsByDistance().Take(Mathf.Max(1, data.multishotCount)).ToList();
        if (targets.Count == 0) return;

        if (ShouldUseProjectile())
        {
            foreach (var t in targets)
                SpawnProjectile(t.transform);    // ��� ������ X
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
            // AOE�� ź�� ������ �������� Bullet�� �ݰ� Ÿ�� ó��
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
            // ü���� ù Ÿ������ ���, ���� �ٿ�� Bullet�� ó��
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

    // === ����ü ���� ===
    // (���� ����) _legacyDamage ����, attacker(this) ����
    private void SpawnProjectile(Transform target)
    {
        var prefab = data.projectilePrefab != null ? data.projectilePrefab : bulletPrefab;
        if (!prefab || !firePoint) return;

        var go = Instantiate(prefab, firePoint.position, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        if (b != null)
            b.Initialize(target, /*payload:*/ default, /*attacker:*/ this); // payload�� Bullet���� ���� �� ����
    }

    // === Bullet�� �� �� �ֵ��� ���� ===
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


    // ===== Ÿ��/��ƿ =====

    private Monster SelectPrimaryTarget()
    {
        // ����: ���� ����� Ÿ��
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
        // �����̵� ���Ÿ��� �ϴ� �߻�ü �켱 ����ϰ� �ʹٸ� true ��ȯ
        // ������ prefab ������ �Ǵ�
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
    // ===== Unit.cs �ȿ� �ִ� ������ =====
    private DamagePayload BuildBasicPayload()
    {
        // 1) �⺻ Ÿ�� ����
        DamageType type =
            Has(data.types, UnitType.Magic) ? DamageType.Magic :
            Has(data.types, UnitType.Elemental) ? DamageType.Magic : // ���Ҵ� �ʱ⿡ ���� ���
            Has(data.types, UnitType.Area) ? DamageType.Area :
                                                   DamageType.Physical;

        // 2) �⺻ ���ݷ�
        float dmg = GetAttackDamage();

        // 3) �ó��� ������ �޾Ƽ� ���� (������ �⺻��)
        var snap = (SynergyManager.Instance != null)
            ? SynergyManager.Instance.GetSnapshotFor(this)
            : SynergySnapshot.Default;

        // 4) Ÿ�Ժ� ���� ���� + ��� ����
        dmg += snap.flatAdd;
        switch (type)
        {
            case DamageType.Physical: dmg *= snap.physMul; break;
            case DamageType.Magic: dmg *= snap.magicMul; break;
            case DamageType.Area: dmg *= snap.areaMul; break;
            case DamageType.True: dmg *= snap.trueMul; break;
        }

        // 5) ũ��/���� ����
        float critChance = Mathf.Clamp01(0.10f + snap.critAdd);   // �⺻ 10% + �ó���
        float critMultiplier = Mathf.Max(1f, 1.50f + snap.critMultAdd);
        float armorPen = Mathf.Clamp01(snap.armorPenAdd);
        float magicPen = Mathf.Clamp01(snap.magicPenAdd);

        // 6) ���̷ε� ����
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

        // AOE �����̸� ���� Ÿ�Ե� Area�� �±�(��� 50% ��Ģ ���� ����)
        if (Has(data.types, UnitType.Area))
            return DamageType.Area;

        return DamageType.Physical;
    }

    // Poison(DoT)�� ���̷ε�� ����
    private System.Collections.IEnumerator CoPoison(Monster target)
    {
        int ticks = Mathf.Max(1, data.poisonTickCount);
        for (int i = 0; i < ticks; i++)
        {
            if (!target) yield break;

            var payload = new DamagePayload
            {
                BaseDamage = Mathf.Max(1, data.poisonDamagePerTick),
                Type = DamageType.True, // �ʱ⿣ �������ط� ó��
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

        // === �߰�: ���׷��̵� ���� ������ ===
        int flat = 0;
        if (UpgradeManager.Instance != null)
            flat = UpgradeManager.Instance.GetFinalAttackFlat(data);

        float scaled = (baseDmg + flat) * Mathf.Max(0f, _atkMul);
        return Mathf.Max(1, Mathf.RoundToInt(scaled));
    }


    // ���� �÷��� üũ
    private static bool Has(UnitType mask, UnitType flag) => (mask & flag) == flag;

    // ��Ÿ� ����
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

    // === �߰�: ���׷��̵� ���� ��ü ===
    private void ApplyUpgradesNow()
    {
        if (data == null || UpgradeManager.Instance == null) return;

        var (atkMul, aspdMul) = UpgradeManager.Instance.GetFinalMultipliers(data);
        _atkMul = Mathf.Max(0f, atkMul);
        _aspdMul = Mathf.Max(0.0001f, aspdMul);

        // ���� ����(����)
        float aps = data.attackSpeed * _aspdMul;
        aps = Mathf.Min(aps, 5f); // ��: �ʴ� 5Ÿ ����

        attackCooldown = 1f / Mathf.Max(0.01f, aps);  // �� �� �ٸ� �����, �ߺ� ��� ���� ����

        if (logOnApply || logBreakdown) LogUpgradeApplied();
    }
    // === ���� �α� ��� ===
    private void LogUpgradeApplied()
    {
        var um = UpgradeManager.Instance;
        if (um == null) return;

        int baseDmg = data.baseAttack + (starLevel - 1) * 5;
        int flat = 0;
        if (um != null) flat = um.GetFinalAttackFlat(data);   // Flat �� ���� 0 ��ȯ

        float finalDmg = (baseDmg + flat) * _atkMul;
        float aps = Mathf.Min(data.attackSpeed * _aspdMul, 5f);
        float cd = 1f / Mathf.Max(0.01f, aps);

        if (logOnApply)
        {
            Debug.Log(
                $"[Upgrade][{name}] cost={data.cost}, jobs={data.jobs}, origins={data.origins} | " +
                $"ATK {baseDmg} + {flat} �� {finalDmg:F1} (x{_atkMul:F2}), " +
                $"APS {data.attackSpeed:F2} �� {aps:F2} (x{_aspdMul:F2}) | CD {cd:F3}s");
        }

        if (logBreakdown)
        {
            var bd = um.BuildBreakdown(data); // �Ʒ� 2) ����
            var lines = string.Join("\n   ", bd.lines);
            Debug.Log(
                $"[Upgrade-Breakdown][{name}] sums: +ATK% {bd.atkPctSum:P1}, +ASPD% {bd.aspdPctSum:P1}, +Flat {bd.flatSum}\n   {lines}");
        }

        if (logBreakdown)
        {
            var bd = um.BuildBreakdown(data); // �̹� ���� �ִ� �극��ũ�ٿ�
            var lines = string.Join("\n   ", bd.lines);
            Debug.Log(
                $"[Upgrade-Breakdown][{name}] sums: +ATK% {bd.atkPctSum:P1}, +ASPD% {bd.aspdPctSum:P1}, +Flat {bd.flatSum}\n   {lines}");
#if UNITY_EDITOR
            LogCurvePresenceEditor();   // �� Ŀ�� ����/�������� �� �� ���
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

            // UpgradeManager�� private 'config' �б�
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
