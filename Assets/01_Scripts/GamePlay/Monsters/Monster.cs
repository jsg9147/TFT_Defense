using TMPro;
using UnityEngine;

public class Monster : MonoBehaviour, IDamageable
{
    public delegate void MonsterDieHandler(Monster monster);
    public event MonsterDieHandler OnMonsterDie;

    [Header("설정")]
    public SpriteRenderer unitSprite;
    public MonsterData data;
    public TextMeshPro hpText; // 나중에 게이지 형식으로 교체 할꺼임

    [Header("상태")]
    private int currentHP;
    private Transform target; // 목표 위치 (경로 끝 또는 목표 타일)
    private int currentWaypointIndex = 0; // 현재 웨이포인트 인덱스

    private bool _unregistered;

    public bool IsAlive => currentHP > 0;

    public Transform Transform => transform;

    private void Start()
    {
        currentHP = data.maxHP;
        target = MonsterPathManager.Instance.GetWaypoint(currentWaypointIndex); // 예시: 경로 끝 반환
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
            return; // 목표가 없으면 몬스터 제거
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * data.moveSpeed * Time.deltaTime;

        // 목표 지점에 도달했는지 확인
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            GetNextTarget();
        }
    }

    private void GetNextTarget()
    {
        currentWaypointIndex++;

        // 마지막 웨이포인트를 지나면 다시 0으로 되돌림
        if (currentWaypointIndex >= MonsterPathManager.Instance.GetWaypointCount())
        {
            currentWaypointIndex = 0;
        }

        target = MonsterPathManager.Instance.GetWaypoint(currentWaypointIndex);
    }

    private void UpdateHpUI()
    {
        if (hpText) hpText.text = currentHP.ToString();
    }

    // 인터페이스 적용: 데미지 페이로드로 받기
    public void TakeDamage(in DamagePayload payload)
    {
        int finalDamage = DamageFormula.ComputeFinal(
            payload,
            data.defense,
            data.magicResistance
        );

        currentHP -= finalDamage;
        UpdateHpUI();

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        OnMonsterDie?.Invoke(this); // 스포너가 Unregister + Return 담당
        _unregistered = true;
    }

    private void OnDisable()
    {
        // 예기치 않은 비활성화 시 누수 방지(씬 종료 시 인위적 생성 방지)
        if (!_unregistered && Application.isPlaying)
        {
            var svc = FindAnyObjectByType<MonsterFieldManager>();
            if (svc != null) svc.Unregister(this);
            _unregistered = true;
        }
    }
}
