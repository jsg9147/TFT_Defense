using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    private Transform target;
    private int damage;
    private Unit attacker; // optional: 시너지, 특성 적용 시 사용

    public void Initialize(Transform target, int damage)
    {
        this.target = target;
        this.damage = damage;
    }

    private void Update()
    {
        if (target == null)
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
        Monster monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            monster.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
