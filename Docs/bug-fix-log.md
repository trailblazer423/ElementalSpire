# Bug Fix Log

## 2026-07-09 Cannot continue after clearing floor 1

- Symptom: After winning the first map node/floor and returning to `MapScene`, the next node could fail to enter the following battle.
- Cause: Map unlock/clear state only lived on `MapNode` scene instances and was not persisted across scene reloads. `MapManager.OnNodeClicked` also accepted only `Normal`/`Elite`/`Boss`, so an empty or unfinished node type looked like a dead click.
- Fix: Added `GameManager` node progress sets, synchronized `MapNode` view state from `GameManager`, persisted unlock/clear on win/floor reset/start, reset full run state from main menu, and recorded challenge progress before entering battle.
- Verification: Static flow check completed. Unity batchmode compile was not run because this project is currently open in Unity Editor; verify through the editor auto-compile or close it and run batchmode compile.

## 2026-07-10 UI and five-scene flow integration

- Symptom: The UI project and the map/card-reward project edited the same Unity scenes independently. Copying either side wholesale removed required managers, card buttons, node bindings, or newer visual assets. The first map node also remained locked because `CardDraftManager` marked the map initialized before `MapManager` could initialize it.
- Cause: Scene files mixed presentation and gameplay ownership, the UI worktree had been reset after its UI commit, and the actual UI work survived mainly as uncommitted scene/assets state. The battle result still returned directly to `MapScene`, while the agreed flow requires a reward draft first.
- Fix: Kept the latest `main` scenes and functional components as the base, selectively synchronized matching visual objects and assets from the UI worktree, added explicit initial-draft state to `GameManager`, separated initial and battle-reward drafting, left map initialization to `MapManager`, and routed battle victory through `CardDraftScene` before returning to the map.
- Verification: Unity 2022.3.62f3 batchmode compilation succeeded. An automated play-mode smoke test completed `MainMenuScene -> ElementSelectScene -> CardDraftScene -> MapScene -> BattleScene -> CardDraftScene -> MapScene`, confirmed node 1 unlocked on first map entry, and confirmed node 1 cleared/node 2 unlocked after the reward draft.
