// IDamageable.cs
using UnityEngine;
public interface IDamageable
{
    void TakeDamage(DamagePayload payload);
    bool IsAlive { get; }
    Transform Transform { get; }
}

public struct DamagePayload
{
    public int BaseDamage;
    public DamageType Type;   // Physical/Magic/True/Area...
    public GameObject Source; // 누가 때렸는지
}
