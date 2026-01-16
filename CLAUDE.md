# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**TFT Defense** is a Tower Defense + Auto Chess hybrid game built in Unity with C#. It features multiplayer support via Unity Netcode for GameObjects.

## Build & Development

This is a Unity project - there are no CLI build commands. Open in Unity Editor (2D project, URP).

**Key Scenes:**
- `Assets/_Project/00_Scenes/GameScene.unity` - Main gameplay
- `Assets/_Project/00_Scenes/NetworkLobby.unity` - Multiplayer lobby
- `Assets/_Project/00_Scenes/StartScene.unity` - Game startup

## Architecture

### Core Patterns

1. **Singleton Pattern**: `MonoSingleton<T>` for scene-persistent managers, `SceneSingleton<T>` for scene-scoped
2. **Event-Based Communication**: C# Action/Event delegates for loose coupling (e.g., `OnWaveChanged`, `OnPhaseChanged`)
3. **Manager Pattern**: Each system has a dedicated Manager class
4. **Data-Driven Design**: ScriptableObjects in `04_Data/` for all configurable data

### Major Systems (in `Assets/_Project/01_Scripts/Systems/`)

| System | Manager | Purpose |
|--------|---------|---------|
| GameLoop | `GameManager` | Game state machine (Prepare→Battle→Win/Lose), wave loop, timer |
| Units | `UnitPlacementManager`, `EvolutionManager` | Unit placement, 3-star evolution system |
| Monsters | `MonsterSpawner`, `MonsterFieldManager` | Wave-based spawning, field limit tracking |
| Battle | `GridManager` | 2D grid board (8x5), unit positioning |
| Combat | `DamageFormula` | Damage calculation with defense/resistance |
| Synergy | `SynergyManager` | Job/Origin synergy bonuses (tier thresholds: 2/4/6, 1/3/5) |
| Economy | `SummonManager`, `ShopManager` | Shop, unit summons, currency |
| Network | `NetworkGameManager`, `NetworkPlayer` | Netcode wrapper, per-player state sync |

### Network Architecture

- **Single-player**: Uses `CurrencyManager` (local singleton)
- **Multiplayer**: Uses `NetworkPlayer` with NetworkVariables and ServerRPCs
- Check `NetworkGameManager.IsNetworkMode()` to distinguish modes
- Monster spawning is server-authoritative

### Key Event Flows

```
GameManager.InitializeGame() → ResetAllManagers() → WaveLoop()
    → Prepare Phase (timer) → Battle Phase (MonsterSpawner.StartWave())
    → Check win/lose → Next wave or end

Unit Placement → GridManager.TryPlaceUnit()
    → EvolutionManager.RegisterOnField()
    → SynergyManager.RegisterUnit()
    → SynergyManager.Recalculate()
```

### Data Locations (`Assets/_Project/04_Data/`)

- `MonsterData/` - Monster definitions
- `PlayerUnitData/` - Units organized by cost (Cost01-Cost05)
- `WaveData/` - Wave configurations
- `ProbabilityTable/` - Shop roll probabilities by level
- `Upgrade/` - Enhancement configurations

## Coding Conventions

### Naming
- Classes/Methods: PascalCase
- Fields/Properties: camelCase
- Events: `On` prefix (e.g., `OnWaveChanged`)
- Interfaces: `I` prefix (e.g., `IDamageable`)

### Unity Patterns
- Scene-dependent managers use `FindAnyObjectByType` for binding
- Subscribe events in `OnEnable`, unsubscribe in `OnDisable`
- Stop coroutines before starting new ones (prevent duplicates)
- All managers must implement `Reset()` for scene reload handling
- Null-check singletons before access

### Synergy Types (Flags Enums)
- **Jobs (8)**: Warrior, Mage, Ranger, Assassin, Guardian, Support, Engineer, Summoner
- **Origins (8)**: Kingdom, Undead, Beast, Mech, Spirit, Void, Goblin, Slime

## External Dependencies

- Unity Netcode for GameObjects (2.7.0)
- Unity Input System
- TextMeshPro
- SPUM (sprite animation)
- DamageNumbersPro
- Odin Inspector (conditional: `#if ODIN_INSPECTOR`)

## Known Incomplete Areas

- Shop phase is commented out (Prepare→Battle loop only)
- SummonManager uses client-side random (security risk for multiplayer)
- Network sync incomplete for units/combat
- VFX/sound systems have TODO placeholders
