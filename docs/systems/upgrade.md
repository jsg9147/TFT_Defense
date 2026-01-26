# Upgrade System

## Overview

The Upgrade system handles unit enhancement, allowing players to improve unit stats through an upgrade progression system.

## Key Classes

| Class | File | Role |
|-------|------|------|
| `UpgradeManager` | `Systems/Upgrade/UpgradeManager.cs` | Unit upgrade logic |

## Upgrade Stages

Units can be upgraded through multiple stages, each providing stat bonuses:

```
Base → Stage 1 → Stage 2 → Stage 3 → ... → Max
```

## Upgrade Costs

Each upgrade stage has associated costs:
- Gold cost increases per stage
- May require specific materials/items

## Stat Bonuses

Upgrades can enhance:
- Attack Damage
- Defense
- Magic Resistance
- Health
- Attack Speed

## Data Files

| Path | Purpose |
|------|---------|
| `Assets/_Project/04_Data/Upgrade/` | Upgrade stage configurations |

## Upgrade Flow

```
Select Unit for Upgrade
    │
    ▼
Check Requirements (gold, materials)
    │
    ▼
Apply Upgrade Stage
    │
    ▼
Update Unit Stats
    │
    ▼
Trigger Visual Feedback
```

## Integration Points

- **CurrencyManager**: Deduct upgrade costs
- **Combat**: Upgraded stats affect damage calculation
- **UI**: Display upgrade progress and options
