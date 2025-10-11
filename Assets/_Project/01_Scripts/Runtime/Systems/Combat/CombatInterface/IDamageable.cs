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
    public int BaseDamage;     // ��ų/��Ÿ�� ���� ��ġ
    public DamageType Type;    // ����/����/Ʈ��/����

    // Ȯ�� �ɼ� (���ϸ� 0���� ��)
    public float CritChance;   // 0~1
    public float CritMultiplier; // 1.5 ���� ����
    public float ArmorPen;     // 0~1 (���� ��� ����)
    public float MagicPen;     // 0~1 (���� ����)

    public Object Source;      // ������ ����(����, ��ų ��)

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