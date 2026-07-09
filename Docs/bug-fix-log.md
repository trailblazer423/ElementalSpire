# Bug Fix Log

## 2026-07-09 Cannot continue after clearing floor 1

- Symptom: After winning the first map node/floor and returning to `MapScene`, the next node could fail to enter the following battle.
- Cause: Map unlock/clear state only lived on `MapNode` scene instances and was not persisted across scene reloads. `MapManager.OnNodeClicked` also accepted only `Normal`/`Elite`/`Boss`, so an empty or unfinished node type looked like a dead click.
- Fix: Added `GameManager` node progress sets, synchronized `MapNode` view state from `GameManager`, persisted unlock/clear on win/floor reset/start, reset full run state from main menu, and recorded challenge progress before entering battle.
- Verification: Static flow check completed. Unity batchmode compile was not run because this project is currently open in Unity Editor; verify through the editor auto-compile or close it and run batchmode compile.