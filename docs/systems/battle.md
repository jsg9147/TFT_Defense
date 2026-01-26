# Battle System

## Overview

The Battle system manages the 2D grid board where units are placed and combat occurs. It handles grid positioning, slot management, and unit-to-grid interactions.

## Key Classes

| Class | File | Role |
|-------|------|------|
| `GridManager` | `Systems/Battle/GridManager.cs` | Grid board management (8x5) |
| `BoardSlot` | `Systems/Battle/BoardSlot.cs` | Individual grid cell |

## Grid Configuration

```
┌───┬───┬───┬───┬───┬───┬───┬───┐
│0,4│1,4│2,4│3,4│4,4│5,4│6,4│7,4│  ← Row 4 (Back)
├───┼───┼───┼───┼───┼───┼───┼───┤
│0,3│1,3│2,3│3,3│4,3│5,3│6,3│7,3│
├───┼───┼───┼───┼───┼───┼───┼───┤
│0,2│1,2│2,2│3,2│4,2│5,2│6,2│7,2│
├───┼───┼───┼───┼───┼───┼───┼───┤
│0,1│1,1│2,1│3,1│4,1│5,1│6,1│7,1│
├───┼───┼───┼───┼───┼───┼───┼───┤
│0,0│1,0│2,0│3,0│4,0│5,0│6,0│7,0│  ← Row 0 (Front)
└───┴───┴───┴───┴───┴───┴───┴───┘
  8 columns × 5 rows = 40 slots
```

## BoardSlot States

Each `BoardSlot` can be:
- **Empty**: Available for unit placement
- **Occupied**: Contains a player unit
- **Locked**: Temporarily unavailable

## Key Methods

### GridManager

| Method | Purpose |
|--------|---------|
| `TryPlaceUnit(unit, x, y)` | Attempt to place unit at coordinates |
| `RemoveUnit(x, y)` | Remove unit from slot |
| `GetSlot(x, y)` | Get BoardSlot at coordinates |
| `GetOccupiedSlots()` | List all slots with units |

## Placement Flow

```
TryPlaceUnit(unit, x, y)
    │
    ├── Validate coordinates in bounds
    │
    ├── Check slot is empty
    │
    ├── Place unit in slot
    │
    └── Trigger placement events
```

## Integration Points

- **UnitPlacementManager**: Requests placement via GridManager
- **SynergyManager**: Notified when units added/removed
- **EvolutionManager**: Checks grid for evolution candidates
