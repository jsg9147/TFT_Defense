using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class Unit : MonoBehaviour
{
    [Header("기본 구성")]
    public SpriteRenderer unitSprite;
    public TextMeshPro nameText;
    public TextMeshPro starText;
    public Transform firePoint;

    [Header("기본 데이터")]
    public UnitData data;
    public GameObject bulletPrefab; // 구버전 호환용 (없으면 data.projectilePrefab 사용)

    [Header("상태")]
    public int starLevel = 1;
    public bool isPlaced = false;

    private float attackCooldown;
    private float lastAttackTime;
    private readonly List<Monster> monstersInRange = new();

    private void Start()
    {
        attackCooldown = 1f / Mathf.Max(0.01f, data.attackSpeed);

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
        if (unitSprite) unitSprite.sprite = data.icon;
        if (nameText) nameText.text = data.unitName;
        if (starText) starText.text = $"★ {starLevel}성";
    }

    private void Update()
    {
        if (!isPlaced) return;

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
    private DamagePayload BuildBasicPayload()
    {
        int baseDmg = GetAttackDamage();
        var type = Has(data.types, UnitType.Magic) ? DamageType.Magic
                 : Has(data.types, UnitType.Elemental) ? DamageType.Magic // 원소도 당장은 마법 취급
                 : DamageType.Physical;

        return new DamagePayload
        {
            BaseDamage = baseDmg,
            Type = type,
            CritChance = 0.1f,        // 필요 시 UnitData/성급/시너지에서 끌어오기
            CritMultiplier = 1.5f,
            ArmorPen = 0f,
            MagicPen = 0f,
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
        // 성급 보정 등
        return data.baseAttack + (starLevel - 1) * 5;
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
        if (unitSprite != null) unitSprite.color = active ? Color.yellow : Color.white;
    }
}
