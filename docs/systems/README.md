# Systems Documentation

This directory contains detailed documentation for each major system in TFT Defense.

## System Index

| System | Documentation | Key Classes |
|--------|---------------|-------------|
| [GameLoop](gameloop.md) | Game state machine, wave loop, timer | `GameManager`, `CurrencyManager`, `PlayerLevelManager` |
| [Units](units.md) | Unit placement, 3-star evolution | `UnitPlacementManager`, `EvolutionManager` |
| [Monsters](monsters.md) | Wave-based spawning, field limit | `MonsterSpawner`, `MonsterPool`, `MonsterFieldManager` |
| [Battle](battle.md) | 2D grid board, unit positioning | `GridManager`, `BoardSlot` |
| [Combat](combat.md) | Damage calculation, defense/resistance | `DamageFormula` |
| [Synergy](synergy.md) | Job/Origin synergy bonuses | `SynergyManager` |
| [Economy](economy.md) | Shop, unit summons, currency | `SummonManager`, `ShopManager` |
| [Upgrade](upgrade.md) | Unit enhancement system | `UpgradeManager` |
| [Network](network.md) | Multiplayer sync, per-player state | `NetworkGameManager`, `NetworkPlayer` |

## Code Location

All system scripts are located in:
```
Assets/_Project/01_Scripts/Systems/
```

## Architecture Overview

Each system follows these patterns:
- **Singleton Pattern**: Managers use `MonoSingleton<T>` or `SceneSingleton<T>`
- **Event-Based Communication**: C# Action/Event delegates for loose coupling
- **Data-Driven Design**: ScriptableObjects in `Assets/_Project/04_Data/`
