using UnityEngine;

public static class DamageFormula
{
    // ������ ��� ����: effArmor / (100 + effArmor)
    static float ArmorReduction(float armor, float pen01)
    {
        float eff = Mathf.Max(0f, armor * (1f - Mathf.Clamp01(pen01)));
        return eff / (100f + eff);
    }

    public static int ComputeFinal(in DamagePayload p, int defense, int magicResist)
    {
        // 1) ġ��Ÿ
        float raw = p.BaseDamage;
        if (p.CritChance > 0f && Random.value < Mathf.Clamp01(p.CritChance))
            raw *= Mathf.Max(1f, p.CritMultiplier);

        // 2) Ÿ�Ժ� �氨
        float after = raw;
        switch (p.Type)
        {
            case DamageType.Physical:
                {
                    float red = ArmorReduction(defense, p.ArmorPen);
                    after = raw * (1f - red);
                    break;
                }
            case DamageType.Magic:
                {
                    float red = ArmorReduction(magicResist, p.MagicPen);
                    after = raw * (1f - red);
                    break;
                }
            case DamageType.Area:
                {
                    // ��: ������ 50%�� ��� ���� (���ϸ� ����)
                    float red = ArmorReduction(Mathf.RoundToInt(defense * 0.5f), p.ArmorPen);
                    after = raw * (1f - red);
                    break;
                }
            case DamageType.True:
            default:
                after = raw;
                break;
        }

        // 3) �ּ� ���� ����
        return Mathf.Max(1, Mathf.RoundToInt(after));
    }
}
