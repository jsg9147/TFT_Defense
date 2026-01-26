# Units System

## Overview

The Units system handles unit placement on the game board and the 3-star evolution mechanic where combining identical units creates stronger versions.

## Key Classes

| Class | File | Role |
|-------|------|------|
| `UnitPlacementManager` | `Systems/Units/UnitPlacementManager.cs` | Handles unit placement/removal on board |
| `EvolutionManager` | `Systems/Units/EvolutionManager.cs` | 3-star evolution system |

## 3-Star Evolution System

Units evolve when 3 identical units are combined:

```
★☆☆ + ★☆☆ + ★☆☆  →  ★★☆ (2-star)
★★☆ + ★★☆ + ★★☆  →  ★★★ (3-star)
```

### Evolution Rules
- Requires 3 units of the same type and star level
- Evolution is automatic when conditions are met
- Higher star units have increased stats

## Unit Placement Flow

```
Unit Placement Request
    │
    ▼
GridManager.TryPlaceUnit()
    │
    ▼
EvolutionManager.RegisterOnField()
    │
    ▼
SynergyManager.RegisterUnit()
    │
    ▼
SynergyManager.Recalculate()
```

## Data Files

| Path | Purpose |
|------|---------|
| `Assets/_Project/04_Data/PlayerUnitData/Cost01/` | 1-cost unit definitions |
| `Assets/_Project/04_Data/PlayerUnitData/Cost02/` | 2-cost unit definitions |
| `Assets/_Project/04_Data/PlayerUnitData/Cost03/` | 3-cost unit definitions |
| `Assets/_Project/04_Data/PlayerUnitData/Cost04/` | 4-cost unit definitions |
| `Assets/_Project/04_Data/PlayerUnitData/Cost05/` | 5-cost unit definitions |

## Unit Data Structure

Each unit has:
- **Cost**: Gold cost (1-5)
- **Job**: Class type (Warrior, Mage, etc.)
- **Origin**: Race type (Kingdom, Undead, etc.)
- **Base Stats**: HP, Attack, Defense, etc.
- **Star Multipliers**: Stat scaling per star level

## Integration Points

- **GridManager**: Validates placement positions
- **SynergyManager**: Updates synergy bonuses on placement
- **EvolutionManager**: Checks evolution conditions
