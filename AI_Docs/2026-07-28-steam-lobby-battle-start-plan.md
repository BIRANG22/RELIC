# Steam Lobby Battle Start Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When the Steam lobby host starts battle from the lobby, every joined client applies the same host-authored lobby snapshot and enters Battle state.

**Architecture:** Keep battle synchronization out of scope. The host publishes the latest shared lobby snapshot, then broadcasts a small battle-start command containing a session id, battle seed, map ids, and required shared-state revision. Each peer locally converts the applied lobby snapshot into `BattleRuntimeData` with the existing `LobbyBattleRuntimeTransferService`, then calls `GameStateType.Battle`.

**Tech Stack:** Unity C#, Steamworks.NET lobby chat/data, existing `GameManager` state machine, existing lobby shared-state snapshot code.

## Global Constraints

- Documents are written only in `AI_Docs`.
- Tests are written only under `Assets/Tests/EditMode~/` or `Assets/Tests/PlayMode~/`.
- Do not run Unity batchmode tests because the editor is assumed open.
- Do not commit without explicit user approval.
- Keep multiplayer-boundary logic outside battle core calculation.

---

### Task 1: Battle Start Command Model And Serialization

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyBattleStartModels.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyBattleStartSerialization.cs`
- Test: `Assets/Tests/EditMode~/LobbyBattleStartSerializationTests.cs`

**Interfaces:**
- Produces: `LobbyBattleStartCommand` with `RequestId`, `HostSteamId`, `RequiredSharedStateRevision`, `BattleSessionId`, `BattleSeed`, `ChapterId`, `StageId`.
- Produces: `LobbyBattleStartSerialization.SerializeCommand(command)` and `TryDeserializeCommand(payload, out command)`.

- [ ] Write a failing serialization round-trip test.
- [ ] Run the test compile and verify the model/serialization types are missing.
- [ ] Add the model and serialization code.
- [ ] Re-run test compile and verify success.

### Task 2: Steam Battle Start Synchronizer

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyBattleStartSynchronizer.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyInviteController.cs`
- Test: `Assets/Tests/EditMode~/SteamLobbyBattleStartBoundaryTests.cs`

**Interfaces:**
- Produces: `EnterLobby(lobbyId, localId, ownerSteamId)`, `LeaveLobby()`, `CanLocalPlayerStartBattle()`, `TryBroadcastBattleStart(requiredRevision, chapterId, stageId, battleSeed)`.
- Consumes: `SteamLobbySharedStateSynchronizer.CurrentSnapshot` and `AppliedRevision`.

- [ ] Write failing boundary tests for binding, command broadcast, client command handling, and shared-state revision wait.
- [ ] Implement the synchronizer with Steam chat command handling.
- [ ] Bind it from `SteamLobbyInviteController`.
- [ ] Re-run tests and build.

### Task 3: Shared Battle Entry Runner

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyBattleEntryService.cs`
- Modify: `Assets/Project/Scripts/UI/Lobby/BattlePlayButton.cs`
- Test: extend battle-start boundary tests.

**Interfaces:**
- Produces: `LobbyBattleEntryService.TryEnterBattle(...)`, wrapping the existing lobby-to-battle transfer and state transition.
- `BattlePlayButton` host click calls the same local entry path after broadcasting the start command.
- Client command receipt uses the same entry path.

- [ ] Extract the duplicated battle entry steps from `BattlePlayButton`.
- [ ] Connect host click to broadcast, then local entry.
- [ ] Connect client command receipt to shared-state wait, then local entry.
- [ ] Verify clients remain blocked from manually starting battle.

### Task 4: Verification

**Files:**
- No new production files.

- [ ] Run `MSBuild RELIC.sln /t:Build /p:Configuration=Debug /v:minimal`.
- [ ] Compile EditMode test sources with `csc`.
- [ ] Run `git diff --check`.
- [ ] Report that Unity batchmode tests were not run due project rule.
