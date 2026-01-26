# Economy System

## Overview

The Economy system manages the shop interface, unit summoning, currency spending, and probability-based unit rolling.

## Key Classes

| Class | File | Role |
|-------|------|------|
| `SummonManager` | `Systems/Economy/SummonManager.cs` | Unit summoning/rolling |
| `ShopManager` | `Systems/Economy/ShopManager.cs` | Shop UI and transactions |

## Shop Mechanics

### Rolling Units
- Players spend gold to roll for random units
- Unit pool determined by probability tables
- Higher player level = better unit odds

### Probability Tables

Player level affects unit cost probabilities:

| Level | 1-Cost | 2-Cost | 3-Cost | 4-Cost | 5-Cost |
|-------|--------|--------|--------|--------|--------|
| 1 | 100% | 0% | 0% | 0% | 0% |
| 3 | 75% | 25% | 0% | 0% | 0% |
| 5 | 55% | 30% | 15% | 0% | 0% |
| 7 | 35% | 35% | 25% | 5% | 0% |
| 9 | 15% | 20% | 35% | 25% | 5% |

## Data Files

| Path | Purpose |
|------|---------|
| `Assets/_Project/04_Data/ProbabilityTable/` | Roll probabilities by level |
| `Assets/_Project/04_Data/PlayerUnitData/` | Available units in pool |

## Currency Flow

```
Player Gold
    │
    ├── Roll Shop (-gold) → Random Unit
    │
    ├── Buy XP (-gold) → Level Up
    │
    └── Sell Unit (+gold) → Remove Unit
```

## Known Issues

> ⚠️ **Security Risk**: `SummonManager` currently uses client-side random for unit rolling. In multiplayer, this should be server-authoritative to prevent manipulation.

> ⚠️ **Incomplete**: Shop phase is currently commented out. Game runs Prepare→Battle loop only.

## Integration Points

- **CurrencyManager**: Gold transactions (single-player)
- **NetworkPlayer**: Gold sync (multiplayer)
- **PlayerLevelManager**: Level affects probabilities
