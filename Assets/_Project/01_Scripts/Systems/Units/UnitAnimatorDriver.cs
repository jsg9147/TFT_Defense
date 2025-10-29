using UnityEngine;
using System;
using System.Linq;

/// SPUM 기본 파라미터(1_Move, 2_Attack, ...)를 자동으로 찾아 해시로 보관하고,
/// 상황에 맞게 안전하게 호출하는 래퍼
[DisallowMultipleComponent]
public class UnitAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    // 해시 캐시
    private int _pMove = -1;      // "1_Move" (float/혹은 bool로 쓰는 경우도 있으나 SPUM은 보통 float speed)
    private int _pAttack = -1;    // "2_Attack" (Trigger)
    private int _pDamaged = -1;   // "3_Damaged" (Trigger)
    private int _pDeath = -1;     // "4_Death" (Trigger)
    private int _pDebuff = -1;    // "5_Debuff" (Trigger)
    private int _pOther = -1;     // "6_Other" (Trigger)
    private int _pIsDeath = -1;   // "isDeath" (Bool)

    // 이름 후보(팀/툴에서 살짝 바뀌어도 견딜 수 있게 부분일치 탐색)
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

        // Animator에 등록된 모든 파라미터를 훑어서 이름 부분일치로 매핑
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

    // ===== 외부에서 호출할 안전 래퍼 =====
    public void SetMoveSpeed(float speed)
    {
        if (!animator) return;
        if (_pMove != -1) animator.SetFloat(_pMove, speed);
        animator.speed = Mathf.Max(0.1f, speed); // 클립 속도도 같이 비례(원하면 주석)
    }

    public void PlayIdle()
    {
        if (!animator) return;
        // SPUM 기본은 IDLE이 베이스이므로 별도 파라미터 없이도 유지됨.
        // 필요 시 animator.Play("IDLE"); 해도 됨(스테이트 이름 고정 시)
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
