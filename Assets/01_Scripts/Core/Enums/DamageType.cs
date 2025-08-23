// DamageType.cs
public enum DamageType
{
    Physical,  // 방어력 적용
    Magic,     // 마법 저항 적용
    True,      // 고정 피해
    Area       // AOE: 방어의 일부만 적용(아래 계산식 참조)
}
