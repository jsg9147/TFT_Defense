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

### Major Systems

All systems located in `Assets/_Project/01_Scripts/Systems/`. For detailed documentation, see [docs/systems/](docs/systems/README.md).

| System | Manager | Purpose | Docs |
|--------|---------|---------|------|
| GameLoop | `GameManager` | Game state machine, wave loop, timer | [gameloop.md](docs/systems/gameloop.md) |
| Units | `UnitPlacementManager`, `EvolutionManager` | Unit placement, 3-star evolution | [units.md](docs/systems/units.md) |
| Monsters | `MonsterSpawner`, `MonsterFieldManager` | Wave-based spawning, field limit | [monsters.md](docs/systems/monsters.md) |
| Battle | `GridManager` | 2D grid board (8x5), unit positioning | [battle.md](docs/systems/battle.md) |
| Combat | `DamageFormula` | Damage calculation with defense/resistance | [combat.md](docs/systems/combat.md) |
| Synergy | `SynergyManager` | Job/Origin synergy bonuses | [synergy.md](docs/systems/synergy.md) |
| Economy | `SummonManager`, `ShopManager` | Shop, unit summons, currency | [economy.md](docs/systems/economy.md) |
| Upgrade | `UpgradeManager` | Unit enhancement system | [upgrade.md](docs/systems/upgrade.md) |
| Network | `NetworkGameManager`, `NetworkPlayer` | Netcode wrapper, per-player state sync | [network.md](docs/systems/network.md) |

### Quick Reference

- **Network mode check**: `NetworkGameManager.IsNetworkMode()`
- **Synergy thresholds**: Jobs (2/4/6), Origins (1/3/5)
- **Grid size**: 8 columns × 5 rows
- **Data location**: `Assets/_Project/04_Data/`

## Coding Conventions

### Standards & Quality
- **Clean Code**: Adhere rigidly to SOLID principles. Keep methods small, focused, and testable (Single Responsibility).
- **Comments**: Mandatory XML documentation (`///`) for public APIs. Explain the *intent* ("why"), not just the mechanics, especially for complex logic.
- **Optimization**: Avoid frequent allocation in `Update` loops (GC pressure).

### Naming
- **Classes/Methods**: PascalCase
- **Fields/Properties**: camelCase
- **Events**: `On` prefix (e.g., `OnWaveChanged`)
- **Interfaces**: `I` prefix (e.g., `IDamageable`)

### Unity Patterns
- Use `FindAnyObjectByType` for managers (avoid `FindObjectOfType`).
- Subscribe events in `OnEnable`, unsubscribe in `OnDisable`.
- Stop coroutines before starting new ones to prevent duplicates.
- All managers must implement `Reset()` for scene reload handling.
- Null-check singletons before access.

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