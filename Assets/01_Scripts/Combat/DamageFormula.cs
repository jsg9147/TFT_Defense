using UnityEngine;

public static class DamageFormula
{
    // 점감형 방어 공식: effArmor / (100 + effArmor)
    static float ArmorReduction(float armor, float pen01)
    {
        float eff = Mathf.Max(0f, armor * (1f - Mathf.Clamp01(pen01)));
        return eff / (100f + eff);
    }

    public static int ComputeFinal(in DamagePayload p, int defense, int magicResist)
    {
        // 1) 치명타
        float raw = p.BaseDamage;
        if (p.CritChance > 0f && Random.value < Mathf.Clamp01(p.CritChance))
            raw *= Mathf.Max(1f, p.CritMultiplier);

        // 2) 타입별 경감
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
                    // 예: 물리의 50%만 방어 적용 (원하면 조절)
                    float red = ArmorReduction(Mathf.RoundToInt(defense * 0.5f), p.ArmorPen);
                    after = raw * (1f - red);
                    break;
                }
            case DamageType.True:
            default:
                after = raw;
                break;
        }

        // 3) 최소 피해 보장
        return Mathf.Max(1, Mathf.RoundToInt(after));
    }
}
