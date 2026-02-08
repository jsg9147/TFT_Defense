// UnitInspectable.cs
using System.Text;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class UnitInspectable : MonoBehaviour, IInspectable
{
    private Unit u;
    public string DisplayName => u?.data ? u.data.unitName : name;
    public Sprite Icon => u?.data ? u.data.icon : null;
    public Transform FollowTarget => transform;

    void Awake() => u = GetComponent<Unit>();

    public InspectData BuildData()
    { 
        var d = u.data;
        var r = new InspectData
        {
            name = d.unitName,
            icon = d.icon,
            star = u.starLevel,
            cost = d.cost,
            range = d.range
        };

        // 업그레이드 배율/Flat 합산
        float atkMul = 1f, aspdMul = 1f;
        int flat = 0;
        if (UpgradeManager.Instance)
        {
            (atkMul, aspdMul) = UpgradeManager.Instance.GetFinalMultipliers(d); // 배율
            flat = UpgradeManager.Instance.GetFinalAttackFlat(d);              // Flat
        }

        // 최종 공격력(유닛 내부 계산과 동일한 개략치)
        int baseDmg = d.baseAttack + (u.starLevel - 1) * 5;
        r.attack = Mathf.Max(1, Mathf.RoundToInt((baseDmg + flat) * atkMul));
        r.aps = Mathf.Min(d.attackSpeed * aspdMul, 5f);

        // 태그 문자열(Job / Origin / Type)
        r.tags = $"{d.jobs} | {d.origins} | {d.types}";

        // 시너지 개요 (간단 요약)
        if (SynergyManager.Instance)
        {
            var snap = SynergyManager.Instance.GetSnapshotFor(u);
            var sb = new StringBuilder();
            if (snap.physMul != 1f) sb.Append($"Phys x{snap.physMul:F2}  ");
            if (snap.magicMul != 1f) sb.Append($"Magic x{snap.magicMul:F2}  ");
            if (snap.areaMul != 1f) sb.Append($"Area x{snap.areaMul:F2}  ");
            if (snap.trueMul != 1f) sb.Append($"True x{snap.trueMul:F2}  ");
            if (snap.flatAdd != 0) sb.Append($"+{snap.flatAdd} ");
            if (snap.critAdd != 0) sb.Append($"Crit+{snap.critAdd:P0} ");
            if (snap.critMultAdd != 0) sb.Append($"CritDmg+{snap.critMultAdd:P0} ");
            r.synergySummary = sb.ToString().Trim();
        }

        // 업그레이드 상세 로그(선택 표시)
        if (UpgradeManager.Instance != null)
        {
            var bd = UpgradeManager.Instance.BuildBreakdown(d);
            if (bd.lines != null && bd.lines.Count > 0)
                r.upgradeBreakdown = string.Join("\n", bd.lines);
        }
        return r;
    }
}
