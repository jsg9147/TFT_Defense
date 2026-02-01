using TMPro;
using UnityEngine;
using TFT_Defense.Managers;

public class Monster : MonoBehaviour, IDamageable
{
    public delegate void MonsterDieHandler(Monster monster);
    public event MonsterDieHandler OnMonsterDie;

    [Header("")]
    public SpriteRenderer unitSprite;
    public MonsterData data;
    public TextMeshPro hpText; 

    [Header("")]
    private int currentHP;
    private Transform target; 
    private int currentWaypointIndex = 0; 

    private bool _unregistered;

    public bool IsAlive => currentHP > 0;

    public Transform Transform => transform;

    private void Start()
    {
        currentHP = data.maxHP;
        target = MonsterPathManager.Instance.GetWaypoint(currentWaypointIndex); 
    }

    private void Update()
    {
        MoveTowardsTarget();
    }
    public void Init()
    {
        currentHP = data.maxHP;
        UpdateHpUI(); // Add this line to update UI immediately
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
            return;  
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * data.moveSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            GetNextTarget();
        }
    }

    private void GetNextTarget()
    {
        currentWaypointIndex++;

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


    public void TakeDamage(in DamagePayload payload)
    {
        int finalDamage = DamageFormula.ComputeFinal(
            payload,
            data.defense,
            data.magicResistance
        );

        if (finalDamage > 0)
        {
            DamageTextManager.Instance.ShowDamage(finalDamage, transform.position);
        }

        currentHP -= finalDamage;
        UpdateHpUI();

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        OnMonsterDie?.Invoke(this);     
        _unregistered = true;
    }

    private void OnDisable()
    {
        if (!_unregistered && Application.isPlaying)
        {
            var svc = FindAnyObjectByType<MonsterFieldManager>();
            if (svc != null) svc.Unregister(this);
            _unregistered = true;
        }
    }
}