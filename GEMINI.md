# GEMINI.md: TFT_Defense Project

## Project Overview

This is a Unity game project named "TFT_Defense". Based on the codebase and file structure, it appears to be a multiplayer tower defense game, likely inspired by Teamfight Tactics (TFT). The game involves waves of monsters, player-controlled units, and an economy system.

**Core Technologies:**

*   **Game Engine:** Unity 2022.2.14f1
*   **Programming Language:** C#
*   **Rendering:** Universal Render Pipeline (URP)
*   **Networking:** Unity Netcode for GameObjects
*   **Input:** Unity Input System

**Architecture:**

*   The project follows a singleton pattern for core managers (`GameManager`, `CurrencyManager`, etc.).
*   The code is structured into systems-based directories (`Battle`, `Combat`, `Economy`, `GameLoop`, `Units`, etc.).
*   The `GameManager` class controls the main game loop, which consists of `Prepare`, `Battle`, and `Shop` phases.
*   The project uses `ScriptableObjects` for data management (inferred from common Unity practices, although not directly seen in the limited file view).
*   The game is designed to be multiplayer, as indicated by the use of `Unity.Netcode.GameObjects`.

## Building and Running

There are no explicit build or run commands in the repository. As this is a Unity project, the standard Unity Editor workflow should be used.

**To Run the Game:**

1.  Open the project in the Unity Editor (version 6000.2.14f1 or compatible).
2.  Open the main game scene. Based on the scripts, this is likely located in `Assets/_Project/00_Scenes/`.
3.  Press the "Play" button in the Unity Editor.

**To Build the Game:**

1.  Open the project in the Unity Editor.
2.  Go to `File > Build Settings...`.
3.  Select the target platform (e.g., Windows, macOS, Linux).
4.  Click "Build" and choose a location for the build output.

## Development Conventions

*   **Code Style:** The C# code uses standard conventions, with classes in PascalCase and methods in PascalCase.
*   **Design Patterns:** The project makes use of the Singleton pattern for manager classes.
*   **Scene Management:** The `GameManager` handles scene loading and initialization.
*   **Event-Driven Architecture:** The `GameManager` uses C# events (`Action`) to communicate with other systems (e.g., `OnWaveChanged`, `OnPhaseChanged`).
*   **Testing:** The project includes the `com.unity.test-framework` package, suggesting that unit tests can be written and run from the Unity Test Runner.
