# GameLoop System

## Overview

The GameLoop system manages the core game state machine, controlling phase transitions (Prepare → Battle → Win/Lose), wave progression, and game timers.

## Key Classes

| Class | File | Role |
|-------|------|------|
| `GameManager` | `Systems/GameLoop/GameManager.cs` | Main state machine, wave loop control |
| `CurrencyManager` | `Systems/GameLoop/CurrencyManager.cs` | Gold management (single-player) |
| `PlayerLevelManager` | `Systems/GameLoop/PlayerLevelManager.cs` | Player level and XP tracking |

## Game Phases

```
┌─────────┐     ┌─────────┐     ┌─────────┐
│ Prepare │ ──▶ │ Battle  │ ──▶ │ Win/Lose│
└─────────┘     └─────────┘     └─────────┘
     │               │
     └───────────────┘
        (Next Wave)
```

### Phase Details
- **Prepare Phase**: Timer-based preparation period for unit placement
- **Battle Phase**: Monsters spawn, combat occurs
- **Win/Lose**: Game end conditions checked after each wave

## Event Flow

```
GameManager.InitializeGame()
    │
    ▼
ResetAllManagers()
    │
    ▼
WaveLoop()
    │
    ├── Prepare Phase (timer countdown)
    │
    ├── Battle Phase
    │       └── MonsterSpawner.StartWave()
    │
    ├── Check win/lose conditions
    │
    └── Next wave or end game
```

## Key Events

| Event | Trigger | Purpose |
|-------|---------|---------|
| `OnWaveChanged` | Wave number changes | Update UI, trigger wave-specific logic |
| `OnPhaseChanged` | Phase transition | Notify systems of phase change |

## Data Files

| Path | Purpose |
|------|---------|
| `Assets/_Project/04_Data/WaveData/` | Wave configurations |

## Usage Example

```csharp
// Subscribe to phase changes
GameManager.Instance.OnPhaseChanged += HandlePhaseChange;

// Check current phase
if (GameManager.Instance.CurrentPhase == GamePhase.Battle)
{
    // Battle logic
}
```

## Reset Behavior

`GameManager.Reset()` is called on scene reload:
- Stops all coroutines
- Resets wave counter
- Returns to initial phase
- Triggers `ResetAllManagers()` for dependent systems
