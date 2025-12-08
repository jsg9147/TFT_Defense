// IDamageable.cs
using UnityEngine;
public interface IDamageable
{
    void TakeDamage(in DamagePayload payload);
    bool IsAlive { get; }
    Transform Transform { get; }
}

public struct DamagePayload
{
    public int BaseDamage;     // 스킬/평타의 기초 수치
    public DamageType Type;    // 물리/마법/트루/광역

    // 확장 옵션 (원하면 0으로 둠)
    public float CritChance;   // 0~1
    public float CritMultiplier; // 1.5 같은 배율
    public float ArmorPen;     // 0~1 (물리 방어 관통)
    public float MagicPen;     // 0~1 (마저 관통)

    public Object Source;      // 가해자 참조(유닛, 스킬 등)

    public static DamagePayload MakeBasic(int dmg, DamageType type, Object src = null)
    {
        return new DamagePayload
        {
            BaseDamage = dmg,
            Type = type,
            CritChance = 0f,
            CritMultiplier = 1.5f,
            ArmorPen = 0f,
            MagicPen = 0f,
            Source = src
        };
    }
}