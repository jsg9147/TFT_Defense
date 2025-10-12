// IDamageCalculator.cs
public interface IDamageCalculator
{
    int Compute(IDamageable target, DamagePayload payload);
}
