// Bullet.cs 전체 대체 예시

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;

    private Transform target;
    private DamagePayload payload;   // (옵션) 외부에서 주는 고정 페이로드. 없으면 attacker로 생성
    private Unit attacker;           // 명중 시점 계산용
    private float maxLife = 5f;
    private float life;

    public void Initialize(Transform target, DamagePayload payload, Unit attacker = null)
    {
        this.target = target;
        this.payload = payload;
        this.attacker = attacker;

        if (this.payload.Source == null)
            this.payload.Source = attacker ? attacker.gameObject : gameObject;

        life = 0f;
    }

    private void Update()
    {
        life += Time.deltaTime;
        if (life > maxLife || target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        if (target == null) { Destroy(gameObject); return; }

        // 1) 명중 시점 페이로드 생성(우선순위: 전달된 payload > attacker.BuildImpactPayload())
        var finalPayload = payload;
        if (finalPayload.BaseDamage <= 0 && attacker != null)
            finalPayload = attacker.BuildImpactPayload();

        // 2) 유닛 타입별 분기
        if (attacker != null && Has(attacker.data.types, UnitType.Area))
        {
            DoAreaHit(finalPayload);
        }
        else if (attacker != null && Has(attacker.data.types, UnitType.Chain))
        {
            DoChainHit(finalPayload);
        }
        else
        {
            // 단일 명중
            ApplyDamageTo(target, finalPayload);
            // 옵션: 독
            if (attacker != null && Has(attacker.data.types, UnitType.Poison))
                StartCoroutine(CoPoison(target, attacker.data.poisonTickCount, attacker.data.poisonDamagePerTick, attacker.data.poisonTickInterval));
        }

        Destroy(gameObject);
    }

    private void DoAreaHit(DamagePayload basePayload)
    {
        float r = attacker.data.areaRadius;
        var hits = Physics2D.OverlapCircleAll(target.position, r)
                            .Select(c => c.GetComponent<IDamageable>())
                            .Where(d => d != null);

        foreach (var h in hits)
            h.TakeDamage(basePayload);
    }

    private void DoChainHit(DamagePayload basePayload)
    {
        // 첫 타격
        ApplyDamageTo(target, basePayload);

        // 이후 바운스
        int maxBounce = Mathf.Max(1, attacker.data.chainCount);
        var visited = new HashSet<Transform> { target };
        var current = target;

        for (int i = 0; i < maxBounce; i++)
        {
            var next = FindNearestMonster(current.position, attacker.data.chainRange, visited);
            if (next == null) break;

            ApplyDamageTo(next, basePayload);
            visited.Add(next);
            current = next;
        }
    }

    private Transform FindNearestMonster(Vector3 from, float range, HashSet<Transform> except)
    {
        // 사거리 내 몬스터 중 가장 가까운 것
        Collider2D[] cols = Physics2D.OverlapCircleAll(from, range);
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var c in cols)
        {
            if (!c.TryGetComponent<IDamageable>(out _)) continue;
            if (except.Contains(c.transform)) continue;

            float d = Vector2.Distance(from, c.transform.position);
            if (d <= range && d < bestDist)
            {
                bestDist = d;
                best = c.transform;
            }
        }
        return best;
    }

    private void ApplyDamageTo(Transform t, DamagePayload p)
    {
        if (t != null && t.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(p);

            // 옵션: Poison
            if (attacker != null && Has(attacker.data.types, UnitType.Poison))
                StartCoroutine(CoPoison(t, attacker.data.poisonTickCount, attacker.data.poisonDamagePerTick, attacker.data.poisonTickInterval));
        }
    }

    private IEnumerator CoPoison(Transform t, int ticks, int damagePerTick, float interval)
    {
        for (int i = 0; i < Mathf.Max(1, ticks); i++)
        {
            if (t == null) yield break;
            if (t.TryGetComponent<IDamageable>(out var dmg))
            {
                var dot = new DamagePayload
                {
                    BaseDamage = Mathf.Max(1, damagePerTick),
                    Type = DamageType.True,
                    Source = attacker ? attacker.gameObject : gameObject
                };
                dmg.TakeDamage(dot);
            }
            yield return new WaitForSeconds(Mathf.Max(0.05f, interval));
        }
    }

    private static bool Has(UnitType mask, UnitType flag) => (mask & flag) == flag;
}
