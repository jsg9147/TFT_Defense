using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class Unit : MonoBehaviour
{
    [Header("�⺻ ����")]
    public SpriteRenderer unitSprite;
    public TextMeshPro nameText;
    public TextMeshPro starText;
    public Transform firePoint;

    [Header("�⺻ ������")]
    public UnitData data;
    public GameObject bulletPrefab; // ������ ȣȯ�� (������ data.projectilePrefab ���)

    [Header("����")]
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
        if (starText) starText.text = $"�� {starLevel}��";
    }

    private void Update()
    {
        if (!isPlaced) return;

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
    private DamagePayload BuildBasicPayload()
    {
        int baseDmg = GetAttackDamage();
        var type = Has(data.types, UnitType.Magic) ? DamageType.Magic
                 : Has(data.types, UnitType.Elemental) ? DamageType.Magic // ���ҵ� ������ ���� ���
                 : DamageType.Physical;

        return new DamagePayload
        {
            BaseDamage = baseDmg,
            Type = type,
            CritChance = 0.1f,        // �ʿ� �� UnitData/����/�ó������� �������
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
        // ���� ���� ��
        return data.baseAttack + (starLevel - 1) * 5;
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
        if (unitSprite != null) unitSprite.color = active ? Color.yellow : Color.white;
    }
}
