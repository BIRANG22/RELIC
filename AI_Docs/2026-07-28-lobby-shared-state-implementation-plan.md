# Lobby Shared State Implementation Plan

> For agentic workers: implement in small TDD steps. Unity batchmode tests are not run because the editor is assumed open by project rule.

**Goal:** Steam lobby clients can freely open lobby panels while seeing host-owned shop, erosion, culture tank, currency, inventory, bag, and loadout data.

**Architecture:** Keep party selection in `SteamLobbyPartySynchronizer`. Add a separate host-authoritative lobby shared state synchronizer that publishes host snapshots through Steam LobbyData plus immediate lobby chat broadcast. Clients do not mutate host-only lobby systems locally; equipment changes are sent as commands and accepted only for the requester's own party slot character.

**Tech Stack:** Unity C#, Steamworks.NET behind `STEAMWORKS_NET`, `JsonUtility`, existing `LobbyRuntimeData`, `CharacterRuntimeStore`, and EditMode source/serialization tests.

## Tasks

- [ ] Add shared state snapshot and command models.
- [ ] Add shared state serializer with round-trip coverage.
- [ ] Add `SteamLobbySharedStateSynchronizer` and bind it from `SteamLobbyInviteController`.
- [ ] Gate client writes for relic shop, erosion select, culture tank, and battle start.
- [ ] Route lobby skill/relic equip and unequip through host-authoritative commands.
- [ ] Refresh lobby inventory, bag, shop, erosion, and equipment UI when snapshots arrive.
- [ ] Verify with source boundary tests, compiler/build checks, and `git diff --check`.
