using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;

    private Transform target;
    private DamagePayload payload;   // 데미지/타입/소스 한 번에 전달
    private Unit attacker;           // 옵션: 시너지/특성 계산 시 사용
    private float maxLife = 5f;      // 안전장치
    private float life;

    /// <summary>
    /// 발사체 초기화: 타겟, 페이로드, (옵션)공격자
    /// </summary>
    public void Initialize(Transform target, DamagePayload payload, Unit attacker = null)
    {
        this.target = target;
        this.payload = payload;
        this.attacker = attacker;

        // Source가 비어있다면 최소한 발사체 자신을 넣어둔다(로그 추적용)
        if (this.payload.Source == null)
            this.payload.Source = attacker ? attacker.gameObject : gameObject;

        life = 0f;
    }

    private void Update()
    {
        // 안전 종료
        life += Time.deltaTime;
        if (life > maxLife || target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 추적 이동
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // 간단한 도달 판정
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        // IDamageable로 통일
        if (target != null && target.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(payload);
        }

        Destroy(gameObject);
    }
}
