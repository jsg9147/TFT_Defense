# GEMINI.md: TFT_Defense Project Guide

This document provides guidance for developing the TFT_Defense project, ensuring adherence to architectural patterns and coding standards.

## 1. Project Overview

**TFT Defense** is a Tower Defense and Auto Chess hybrid game built in Unity with C#. It's inspired by Teamfight Tactics (TFT) and features multiplayer support using Unity Netcode for GameObjects. The core gameplay loop involves preparing for monster waves, battling them, and managing units and economy.

**Core Technologies:**
*   **Game Engine:** Unity 2022.2.14f1
*   **Programming Language:** C#
*   **Rendering:** Universal Render Pipeline (URP)
*   **Networking:** Unity Netcode for GameObjects
*   **Input:** Unity Input System

## 2. Build & Development

This is a Unity project, so all development and execution happens within the Unity Editor.

**To Run the Game:**
1.  Open the project in the Unity Editor.
2.  Open one of the key scenes:
    *   `Assets/_Project/00_Scenes/GameScene.unity` (Main gameplay)
    *   `Assets/_Project/00_Scenes/NetworkLobby.unity` (Multiplayer lobby)
    *   `Assets/_Project/00_Scenes/StartScene.unity` (Game startup)
3.  Press the "Play" button.

**To Build the Game:**
1.  In the Unity Editor, go to `File > Build Settings...`.
2.  Select the target platform (e.g., Windows, macOS).
3.  Click "Build" and choose an output location.

## 3. Architecture

The project follows a data-driven architecture with clear separation of concerns between different game systems.

### 3.1. Core Patterns
*   **Singleton Pattern**: `MonoSingleton<T>` for globally accessible managers and `SceneSingleton<T>` for scene-specific managers.
*   **Event-Based Communication**: C# `Action` and `event` delegates are used for loose coupling between systems (e.g., `GameManager.OnWaveChanged`).
*   **Manager Pattern**: Each core system (e.g., Economy, Units, Battle) has a dedicated Manager class that serves as its public API.
*   **Data-Driven Design**: `ScriptableObject` assets in `Assets/_Project/04_Data/` are used to hold all configurable data, such as unit stats, wave compositions, and synergy rules.

### 3.2. Major Systems
All system scripts are located in `Assets/_Project/01_Scripts/Systems/`.

| System     | Manager(s)                               | Purpose                                     |
|------------|------------------------------------------|---------------------------------------------|
| **GameLoop** | `GameManager`                            | Controls the main game state and wave loop. |
| **Units**    | `UnitPlacementManager`, `EvolutionManager` | Handles unit placement and 3-star evolution. |
| **Monsters** | `MonsterSpawner`, `MonsterFieldManager`  | Manages wave-based monster spawning.        |
| **Battle**   | `GridManager`                            | Manages the 8x5 grid and unit positioning.  |
| **Combat**   | `DamageFormula`                          | Calculates damage based on stats.           |
| **Synergy**  | `SynergyManager`                         | Manages and applies job/origin bonuses.     |
| **Economy**  | `SummonManager`, `ShopManager`           | Governs the shop, unit summoning, and currency. |
| **Upgrade**  | `UpgradeManager`                         | Handles the unit enhancement system.        |
| **Network**  | `NetworkGameManager`, `NetworkPlayer`    | Wraps Netcode logic and player state sync.  |

### 3.3. Quick Reference
*   **Network Mode Check**: `NetworkGameManager.IsNetworkMode()`
*   **Synergy Thresholds**: Jobs (2/4/6), Origins (1/3/5)
*   **Grid Size**: 8 columns × 5 rows
*   **Data Location**: `Assets/_Project/04_Data/`

## 4. SOLID Principles & Clean Code

To maintain code quality, we adhere to SOLID principles and clean code practices.

*   **Single Responsibility Principle (SRP)**: Every class and method should have one, and only one, reason to change. Managers should delegate tasks to specialized components.
*   **Open/Closed Principle (OCP)**: Systems should be open for extension but closed for modification. Use `ScriptableObject` data and C# events to add new content and functionality without altering existing, stable code.
*   **Interface Segregation Principle (ISP)**: Prefer small, specific interfaces (e.g., `IDamageable`) over large, monolithic ones. This allows classes to only implement the behavior they truly need.
*   **Dependency Inversion Principle (DIP)**: High-level modules should not depend on low-level modules; both should depend on abstractions (interfaces or events). This is key to our event-based architecture.
*   **Clean Code Practices**:
    *   **Descriptive Naming**: Variable and method names should clearly communicate their purpose.
    *   **Keep Methods Small**: A method should do one thing well.
    *   **No Magic Numbers**: Use named constants or `ScriptableObject` fields instead of hard-coded numbers.
    *   **Readability**: Write code for humans first. Keep nesting shallow and add comments only to explain the *why*, not the *what*.

## 5. Coding Conventions

*   **Naming**:
    *   Classes, methods, and events use `PascalCase`.
    *   Private fields and properties use `camelCase`.
    *   Events should be prefixed with `On` (e.g., `OnWaveChanged`).
    *   Interfaces should be prefixed with `I` (e.g., `IDamageable`).
*   **Unity Patterns**:
    *   Subscribe to events in `OnEnable()` and unsubscribe in `OnDisable()` to prevent memory leaks.
    *   Always stop existing coroutines before starting a new one to prevent duplicates.
    *   All singleton managers must implement a `Reset()` method to handle scene reloads gracefully.
    *   Always null-check singletons before accessing their properties or methods.
*   **Comments**: 가급적 코드에 주석을 추가하여 코드의 의도, 복잡한 로직, 중요한 결정 사항 등을 설명해주세요. 주석은 한글로 작성하는 것을 권장합니다.

## 6. External Dependencies

*   Unity Netcode for GameObjects
*   Unity Input System
*   TextMeshPro
*   SPUM (Sprite Animation Tool)
*   DamageNumbersPro
*   Odin Inspector (used with `#if ODIN_INSPECTOR` directive for safe, optional use)

## 7. Known Incomplete Areas

*   The **Shop phase** is currently commented out in the main game loop.
*   `SummonManager` uses a **client-side random number generator**, which is a security risk for multiplayer and must be moved server-side.
*   Network synchronization for unit combat and state is **incomplete**.
*   VFX and sound systems contain **TODO placeholders** and need full implementation.