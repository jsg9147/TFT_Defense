using UnityEngine;
using System;
using System.Linq;

/// SPUM �⺻ �Ķ����(1_Move, 2_Attack, ...)�� �ڵ����� ã�� �ؽ÷� �����ϰ�,
/// ��Ȳ�� �°� �����ϰ� ȣ���ϴ� ����
[DisallowMultipleComponent]
public class UnitAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    // �ؽ� ĳ��
    private int _pMove = -1;      // "1_Move" (float/Ȥ�� bool�� ���� ��쵵 ������ SPUM�� ���� float speed)
    private int _pAttack = -1;    // "2_Attack" (Trigger)
    private int _pDamaged = -1;   // "3_Damaged" (Trigger)
    private int _pDeath = -1;     // "4_Death" (Trigger)
    private int _pDebuff = -1;    // "5_Debuff" (Trigger)
    private int _pOther = -1;     // "6_Other" (Trigger)
    private int _pIsDeath = -1;   // "isDeath" (Bool)

    // �̸� �ĺ�(��/������ ��¦ �ٲ� �ߵ� �� �ְ� �κ���ġ Ž��)
    private static readonly string[] MoveNames = { "1_Move", "Move", "Speed" };
    private static readonly string[] AttackNames = { "2_Attack", "Attack" };
    private static readonly string[] DamageNames = { "3_Damaged", "Damaged", "Hit" };
    private static readonly string[] DeathNames = { "4_Death", "Death" };
    private static readonly string[] DebuffNames = { "5_Debuff", "Debuff" };
    private static readonly string[] OtherNames = { "6_Other", "Other" };
    private static readonly string[] IsDeathNames = { "isDeath", "IsDeath" };

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        RebindParameters();
    }

    private void OnTransformChildrenChanged()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        RebindParameters();
    }

    public void RebindParameters()
    {
        if (!animator) return;

        // Animator�� ��ϵ� ��� �Ķ���͸� �Ⱦ �̸� �κ���ġ�� ����
        var ps = animator.parameters;

        _pMove = FindHash(ps, MoveNames);
        _pAttack = FindHash(ps, AttackNames);
        _pDamaged = FindHash(ps, DamageNames);
        _pDeath = FindHash(ps, DeathNames);
        _pDebuff = FindHash(ps, DebuffNames);
        _pOther = FindHash(ps, OtherNames);
        _pIsDeath = FindHash(ps, IsDeathNames);
    }

    private static int FindHash(AnimatorControllerParameter[] ps, string[] candidates)
    {
        var p = ps.FirstOrDefault(x => candidates.Any(c =>
            string.Equals(x.name, c, StringComparison.OrdinalIgnoreCase) ||
            x.name.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0));
        return p != null ? Animator.StringToHash(p.name) : -1;
    }

    // ===== �ܺο��� ȣ���� ���� ���� =====
    public void SetMoveSpeed(float speed)
    {
        if (!animator) return;
        if (_pMove != -1) animator.SetFloat(_pMove, speed);
        animator.speed = Mathf.Max(0.1f, speed); // Ŭ�� �ӵ��� ���� ���(���ϸ� �ּ�)
    }

    public void PlayIdle()
    {
        if (!animator) return;
        // SPUM �⺻�� IDLE�� ���̽��̹Ƿ� ���� �Ķ���� ���̵� ������.
        // �ʿ� �� animator.Play("IDLE"); �ص� ��(������Ʈ �̸� ���� ��)
    }

    public void TriggerAttack() { SetTriggerIfValid(_pAttack); }
    public void TriggerDamaged() { SetTriggerIfValid(_pDamaged); }
    public void TriggerDebuff() { SetTriggerIfValid(_pDebuff); }
    public void TriggerOther() { SetTriggerIfValid(_pOther); }

    public void TriggerDeath()
    {
        SetTriggerIfValid(_pDeath);
        if (_pIsDeath != -1) animator.SetBool(_pIsDeath, true);
    }

    private void SetTriggerIfValid(int hash)
    {
        if (!animator) return;
        if (hash != -1) animator.SetTrigger(hash);
    }
}
