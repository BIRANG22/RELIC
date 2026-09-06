# Culture Tank Combination Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development while implementing this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace timed per-item culture research with an unordered three-item type combination and claimable completion result.

**Architecture:** `CultureTankResearchService` remains the command/state boundary, while a ScriptableObject recipe database resolves stable combination IDs and effect data. The lobby presenter binds existing scene controls and only reflects service state.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity YAML scene/assets, runtime CSV

## Global Constraints

- Documents live only under `AI_Docs`.
- Tests live only under `Assets/Tests/EditMode~/`.
- No Unity batchmode while the editor is open.
- Multiplayer state uses stable IDs and UI does not calculate effects.

---

### Task 1: Combination domain and service

**Files:**
- Modify: `Assets/Tests/EditMode~/CultureTankResearchServiceTests.cs`
- Create: `Assets/Project/Scripts/Gameplay/Data/Database/CultureTankCombinationDatabase.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/CultureTankResearchService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/CultureTankResearchRuntimeData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Runtime/LobbyRuntimeData.cs`

- [ ] Write failing tests for unordered resolution, three-slot combination, missing-recipe preservation, and claiming.
- [ ] Verify the new tests fail because the combination API does not exist.
- [ ] Implement the minimal database and service API.
- [ ] Compile and verify the tests pass.

### Task 2: Item schema and configured recipes

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Economy/ItemData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/ItemIconDatabase.cs`
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`
- Create: `Assets/DB/Culture Tank Combination Database.asset`
- Modify: `Assets/Project/Scripts/Gameplay/Data/DataManager.cs`
- Modify: `Assets/DB/Item Icon Database.asset`

- [ ] Add schema/database tests that fail on the old per-item result model.
- [ ] Replace item research columns with `CultureType` and assign A/B/C cyclically.
- [ ] Move ten existing result sprites and effects into the combination database with rates set to 1.
- [ ] Verify loader and database tests.

### Task 3: Lobby UI and shared state

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCultureTankPanelPresenter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCultureTankController.cs`
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Modify: `Assets/Tests/EditMode~/LobbyCultureTankPanelTests.cs`
- Modify: `Assets/Tests/EditMode~/LobbySharedStateSerializationTests.cs`

- [ ] Write failing binding/state tests for combine and completion controls.
- [ ] Bind `GameObject/Button`, `completion`, and its result image/button.
- [ ] Render filled slots, combine immediately, and claim through the service.
- [ ] Remove time-based world result calculation and preserve cosmetic-only behavior.
- [ ] Verify shared-state round trip and scene bindings.

### Task 4: Final verification

- [ ] Build `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` without restoring packages.
- [ ] Run available non-batch editor tests or report why they could not be run.
- [ ] Inspect the diff for unrelated changes and confirm no commit/push/PR was performed.
