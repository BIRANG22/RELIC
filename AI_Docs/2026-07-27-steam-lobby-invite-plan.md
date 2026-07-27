# Steam Lobby Invite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Steamworks.NET-based Lobby scene invite smoke test flow.

**Architecture:** One scene controller owns Steam initialization, lobby creation, invite overlay, accepted invite callback, member list refresh, and lightweight member data sync. A tiny pure parser is isolated for launch command parsing tests.

**Tech Stack:** Unity 6000, UGUI, TextMeshPro, Steamworks.NET package `com.rlabrecque.steamworks.net`.

## Global Constraints

- Documents stay in `AI_Docs`.
- Tests stay in `Assets/Tests/EditMode~/`.
- Do not run Unity batchmode tests while the editor is open.
- Do not touch battle transport or battle result logic.
- Do not commit without explicit user approval.

---

### Task 1: Package And Test AppID

**Files:**
- Modify: `Packages/manifest.json`
- Create: `steam_appid.txt`

**Interfaces:**
- Produces: Steamworks.NET package available after Unity package restore.
- Produces: local test AppID file with a single line `480`.

- [x] Add `com.rlabrecque.steamworks.net` version `2025.163.0` through the OpenUPM scoped registry.
- [x] Add `steam_appid.txt` with `480`.

### Task 2: Launch Command Parser Test

**Files:**
- Create: `Assets/Tests/EditMode~/SteamLobbyLaunchCommandParserTests.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyLaunchCommandParser.cs`

**Interfaces:**
- Produces: `SteamLobbyLaunchCommandParser.TryParseLobbyId(string commandLine, out ulong lobbyId)`.

- [x] Test quoted and unquoted `+connect_lobby <id>` command lines.
- [x] Test missing or invalid lobby IDs.
- [x] Implement the minimal parser.

### Task 3: Steam Lobby Invite Controller

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyInviteController.cs`

**Interfaces:**
- Consumes: `SteamLobbyLaunchCommandParser.TryParseLobbyId(...)`.
- Produces: `SteamLobbyInviteController.OpenInviteFlow()` for the Invite button.

- [x] Initialize Steam when `STEAMWORKS_NET` is defined.
- [x] Create a friends-only lobby when no current lobby exists.
- [x] Open `SteamFriends.ActivateGameOverlayInviteDialog`.
- [x] Join lobby from `GameLobbyJoinRequested_t`.
- [x] Poll current party slot IDs and set lobby member data.
- [x] Auto-create a status panel if text references are missing.

### Task 4: Scene Wiring

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`

**Interfaces:**
- Consumes: `SteamLobbyInviteController.OpenInviteFlow()`.

- [x] Add `SteamLobbyInviteController` to the existing `Invite` button object.
- [x] Replace the `Button.onClick` target from panel transition to Steam invite flow.

### Task 5: Verification

**Files:**
- Verify: `RELIC.sln`
- Verify: `Assets/Project/Scenes/YDM/Lobby.unity`

**Interfaces:**
- Produces: manual two-PC Steam smoke test checklist.

- [x] Run MSBuild after edits.
- [ ] If Unity has not resolved Steamworks.NET yet, open Unity Package Manager or let Unity reload packages, then rerun compile.
- [ ] Manual test with two Steam accounts and two PCs.
