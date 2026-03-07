# Project Rules: TFT Defense (Unity/C#)

## AI Interaction Guidelines
- **Ask First:** If any logic is ambiguous, ask for clarification before generating code.
- **Lead by Human:** The human developer leads the architecture; the AI assists in implementation and optimization.
- **Reference:** Always check `CLAUDE.md` for project progress before answering.

## Coding Standards
- **Naming:** PascalCase for Classes/Methods, _camelCase for private fields.
- **Namespaces:** Must include `TFTDefense.[Category]` for all new scripts.
- **Safety:** Always include null checks for `GetComponent` or external references.

## Performance & Unity Best Practices
- **No GC in Update:** Avoid string concatenation or new allocations in `Update()`.
- **Object Pooling:** Mandatory for bullets, effects, and mob spawns.
- **Data-Driven:** Use `ScriptableObject` for all game balance data.

## Task-Specific Commands
- `/review`: Analyze the selected code for memory leaks and convention violations.
- `/refactor`: Propose a more clean and optimized version of the code.