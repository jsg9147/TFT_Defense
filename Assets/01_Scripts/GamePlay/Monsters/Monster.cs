using TMPro;
using UnityEngine;

public class Monster : MonoBehaviour
{
    public delegate void MonsterDieHandler(Monster monster);
    public event MonsterDieHandler OnMonsterDie;

    [Header("����")]
    public SpriteRenderer unitSprite;
    public MonsterData data;
    public TextMeshPro hpText; // ���߿� ������ �������� ��ü �Ҳ���

    [Header("����")]
    private int currentHP;
    private Transform target; // ��ǥ ��ġ (��� �� �Ǵ� ��ǥ Ÿ��)
    private int currentWaypointIndex = 0; // ���� ��������Ʈ �ε���

    private bool _unregistered;

    private void Start()
    {
        currentHP = data.maxHP;
        target = MonsterPathManager.Instance.GetWaypoint(currentWaypointIndex); // ����: ��� �� ��ȯ
    }

    private void Update()
    {
        MoveTowardsTarget();
    }
    public void Init()
    {
        currentHP = data.maxHP;
        currentWaypointIndex = 0;
        target = MonsterPathManager.Instance.GetWaypoint(currentWaypointIndex);
        _unregistered = false;
        gameObject.SetActive(true);
    }



    private void MoveTowardsTarget()
    {
        if (target == null) 
        {
            Destroy(gameObject);
            return; // ��ǥ�� ������ ���� ����
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * data.moveSpeed * Time.deltaTime;

        // ��ǥ ������ �����ߴ��� Ȯ��
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            GetNextTarget();
        }
    }

    private void GetNextTarget()
    {
        currentWaypointIndex++;

        // ������ ��������Ʈ�� ������ �ٽ� 0���� �ǵ���
        if (currentWaypointIndex >= MonsterPathManager.Instance.GetWaypointCount())
        {
            currentWaypointIndex = 0;
        }

        target = MonsterPathManager.Instance.GetWaypoint(currentWaypointIndex);
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        hpText.text = currentHP.ToString(); // ���� HP ǥ�� ������Ʈ
        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnMonsterDie?.Invoke(this); // �����ʰ� Unregister + Return ���
        _unregistered = true;
    }

    private void OnDisable()
    {
        // ����ġ ���� ��Ȱ��ȭ �� ���� ����(�� ���� �� ������ ���� ����)
        if (!_unregistered && Application.isPlaying)
        {
            var svc = FindAnyObjectByType<MonsterFieldManager>();
            if (svc != null) svc.Unregister(this);
            _unregistered = true;
        }
    }
}
