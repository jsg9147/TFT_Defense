# Synergy System

## Overview

The Synergy system calculates and applies bonuses based on unit Jobs (classes) and Origins (races) on the battlefield. Reaching tier thresholds activates increasingly powerful effects.

## Key Classes

| Class | File | Role |
|-------|------|------|
| `SynergyManager` | `Systems/Synergy/SynergyManager.cs` | Synergy calculation and application |

## Synergy Types

### Jobs (8 types)
| Job | Thresholds | Bonus Type |
|-----|------------|------------|
| Warrior | 2/4/6 | Defense |
| Mage | 2/4/6 | Spell Power |
| Ranger | 2/4/6 | Attack Speed |
| Assassin | 2/4/6 | Crit Chance |
| Guardian | 2/4/6 | Team Armor |
| Support | 2/4/6 | Healing |
| Engineer | 2/4/6 | Turret Damage |
| Summoner | 2/4/6 | Summon Stats |

### Origins (8 types)
| Origin | Thresholds | Bonus Type |
|--------|------------|------------|
| Kingdom | 1/3/5 | Gold Generation |
| Undead | 1/3/5 | Lifesteal |
| Beast | 1/3/5 | Attack Damage |
| Mech | 1/3/5 | Shield |
| Spirit | 1/3/5 | Mana Regen |
| Void | 1/3/5 | True Damage |
| Goblin | 1/3/5 | Cost Reduction |
| Slime | 1/3/5 | HP Regen |

## Threshold Pattern

- **Jobs**: 2/4/6 units for Tier 1/2/3
- **Origins**: 1/3/5 units for Tier 1/2/3

## Flags Enums

```csharp
[Flags]
public enum Job
{
    None = 0,
    Warrior = 1 << 0,
    Mage = 1 << 1,
    Ranger = 1 << 2,
    // ... etc
}

[Flags]
public enum Origin
{
    None = 0,
    Kingdom = 1 << 0,
    Undead = 1 << 1,
    Beast = 1 << 2,
    // ... etc
}
```

## Calculation Flow

```
Unit Placed/Removed
    │
    ▼
SynergyManager.RegisterUnit() / UnregisterUnit()
    │
    ▼
SynergyManager.Recalculate()
    │
    ├── Count units per Job
    ├── Count units per Origin
    ├── Determine active tiers
    └── Apply bonuses to units
```

## Integration Points

- **UnitPlacementManager**: Triggers recalculation on placement
- **EvolutionManager**: Triggers recalculation on evolution
- **Combat**: Synergy bonuses affect damage/defense
