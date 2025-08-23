using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;

    private Transform target;
    private DamagePayload payload;   // ������/Ÿ��/�ҽ� �� ���� ����
    private Unit attacker;           // �ɼ�: �ó���/Ư�� ��� �� ���
    private float maxLife = 5f;      // ������ġ
    private float life;

    /// <summary>
    /// �߻�ü �ʱ�ȭ: Ÿ��, ���̷ε�, (�ɼ�)������
    /// </summary>
    public void Initialize(Transform target, DamagePayload payload, Unit attacker = null)
    {
        this.target = target;
        this.payload = payload;
        this.attacker = attacker;

        // Source�� ����ִٸ� �ּ��� �߻�ü �ڽ��� �־�д�(�α� ������)
        if (this.payload.Source == null)
            this.payload.Source = attacker ? attacker.gameObject : gameObject;

        life = 0f;
    }

    private void Update()
    {
        // ���� ����
        life += Time.deltaTime;
        if (life > maxLife || target == null)
        {
            Destroy(gameObject);
            return;
        }

        // ���� �̵�
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // ������ ���� ����
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        // IDamageable�� ����
        if (target != null && target.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(payload);
        }

        Destroy(gameObject);
    }
}
