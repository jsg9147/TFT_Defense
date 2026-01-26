# Network System

## Overview

The Network system provides multiplayer functionality using Unity Netcode for GameObjects, handling player synchronization, server-authoritative game state, and mode detection.

## Key Classes

| Class | File | Role |
|-------|------|------|
| `NetworkGameManager` | `Systems/Network/NetworkGameManager.cs` | Netcode wrapper, game mode management |
| `NetworkPlayer` | `Systems/Network/NetworkPlayer.cs` | Per-player state synchronization |

## Network Architecture

### Single-player Mode
- Uses local singletons (`CurrencyManager`, etc.)
- No network overhead
- Direct method calls

### Multiplayer Mode
- Uses `NetworkPlayer` with NetworkVariables
- ServerRPCs for authoritative actions
- State synced via Netcode

## Mode Detection

```csharp
// Check current network mode
if (NetworkGameManager.IsNetworkMode())
{
    // Multiplayer logic
    NetworkPlayer.Instance.RequestAction_ServerRpc();
}
else
{
    // Single-player logic
    LocalManager.Instance.DoAction();
}
```

## NetworkPlayer State

`NetworkPlayer` synchronizes per-player data:
- Gold (NetworkVariable)
- Level (NetworkVariable)
- Board state (NetworkList)
- Health (NetworkVariable)

## Server-Authoritative Systems

These systems run on server only in multiplayer:
- Monster spawning
- Damage calculation (validation)
- Gold/XP rewards
- Win/lose conditions

## RPC Patterns

### Client → Server (ServerRpc)
```csharp
[ServerRpc]
void RequestPlaceUnit_ServerRpc(int unitId, int x, int y)
{
    // Server validates and executes
}
```

### Server → Client (ClientRpc)
```csharp
[ClientRpc]
void NotifyUnitPlaced_ClientRpc(int unitId, int x, int y)
{
    // All clients update visuals
}
```

## Known Issues

> ⚠️ **Incomplete**: Network sync for units and combat is not fully implemented. Current multiplayer support is limited to lobby and basic game state.

## Key Scenes

| Scene | Purpose |
|-------|---------|
| `NetworkLobby.unity` | Multiplayer lobby, host/join |
| `GameScene.unity` | Gameplay (supports both modes) |

## Dependencies

- Unity Netcode for GameObjects (2.7.0)
- Unity Transport

## Integration Points

- **GameManager**: Coordinates with NetworkGameManager for phase sync
- **CurrencyManager**: Bypassed in network mode (use NetworkPlayer)
- **MonsterSpawner**: Server-authoritative spawning
