# Monsters System

## Overview

The Monsters system handles wave-based enemy spawning, monster pooling for performance, and tracking monsters currently on the battlefield.

## Key Classes

| Class | File | Role |
|-------|------|------|
| `MonsterSpawner` | `Systems/Monsters/MonsterSpawner.cs` | Wave-based monster spawning |
| `MonsterPool` | `Systems/Monsters/MonsterPool.cs` | Object pooling for monsters |
| `MonsterFieldManager` | `Systems/Monsters/MonsterFieldManager.cs` | Tracks active monsters on field |

## Wave Spawning Flow

```
Battle Phase Start
    │
    ▼
MonsterSpawner.StartWave()
    │
    ▼
Load WaveData for current wave
    │
    ▼
Spawn monsters from MonsterPool
    │
    ▼
MonsterFieldManager tracks active count
    │
    ▼
Wave complete when all monsters defeated
```

## Field Limit System

`MonsterFieldManager` enforces limits on simultaneous monsters:
- Prevents performance issues from too many enemies
- Queues spawns when limit is reached
- Spawns queued monsters as others are defeated

## Network Behavior

- Monster spawning is **server-authoritative** in multiplayer
- Server determines spawn timing and positions
- Clients receive spawned monster data via NetworkObjects

## Data Files

| Path | Purpose |
|------|---------|
| `Assets/_Project/04_Data/MonsterData/` | Monster type definitions |
| `Assets/_Project/04_Data/WaveData/` | Wave composition and timing |

## Monster Data Structure

Each monster definition includes:
- **Base Stats**: HP, Attack, Defense, Speed
- **Behavior**: Movement pattern, attack pattern
- **Rewards**: Gold/XP on defeat

## Object Pooling

`MonsterPool` optimizes performance:
- Pre-instantiates monster prefabs
- Reuses deactivated monsters instead of destroying
- Reduces garbage collection during gameplay
