# Combat System

## Overview

The Combat system handles damage calculation, including physical and magical damage types, defense/resistance reduction, and critical hits.

## Key Classes

| Class | File | Role |
|-------|------|------|
| `DamageFormula` | `Systems/Combat/DamageFormula.cs` | Damage calculation formulas |

## Damage Types

| Type | Reduced By | Description |
|------|------------|-------------|
| Physical | Defense | Melee/ranged attacks |
| Magical | Resistance | Spell damage |
| True | Nothing | Ignores all reduction |

## Damage Formula

### Physical Damage
```
FinalDamage = BaseDamage × (100 / (100 + Defense))
```

### Magical Damage
```
FinalDamage = BaseDamage × (100 / (100 + Resistance))
```

### True Damage
```
FinalDamage = BaseDamage
```

## Defense Scaling

| Defense | Damage Reduction |
|---------|-----------------|
| 0 | 0% |
| 50 | 33% |
| 100 | 50% |
| 200 | 67% |
| 300 | 75% |

The formula provides diminishing returns - each point of defense is less effective than the last.

## Critical Hits

```
CritDamage = BaseDamage × CritMultiplier
```

Default crit multiplier: 1.5x (150% damage)

## IDamageable Interface

Units implement `IDamageable` for combat:

```csharp
public interface IDamageable
{
    void TakeDamage(float amount, DamageType type);
    float CurrentHealth { get; }
    bool IsDead { get; }
}
```

## Integration Points

- **Units**: Apply damage via `IDamageable`
- **Synergy**: Modifies attack/defense stats
- **Upgrade**: Enhances unit combat stats
