# Steam Lobby Invite Design

## Goal

Lobby scene Invite button creates or reuses a Steam lobby with AppID 480 test configuration, opens the Steam friend invite overlay, accepts lobby invites on the second PC, and displays a small shared lobby status panel.

## Scope

- Steamworks.NET is the Steam integration boundary.
- Steam lobby is used for invite, membership, and lightweight lobby metadata.
- Battle transport, battle state sync, server authority, and matchmaking browser are out of scope.
- The first test target is two PCs, two Steam accounts, both accounts already friends, both game builds already running.

## Architecture

`SteamLobbyInviteController` lives on the Lobby scene Invite button. It initializes Steam when Steamworks.NET is available, calls `SteamAPI.RunCallbacks()` each frame, creates a friends-only lobby when needed, opens `ActivateGameOverlayInviteDialog`, joins accepted invites through `GameLobbyJoinRequested_t`, and mirrors party slot character IDs into Steam lobby member data.

The controller auto-creates a compact runtime status panel if no text fields are assigned. The panel shows Steam initialization state, current lobby ID, local Steam persona, and current lobby members with slot and character metadata.

## Data Flow

1. User clicks `Invite`.
2. If no Steam lobby exists, create a friends-only lobby with max 3 members.
3. On `LobbyCreated_t`, store lobby ID and open Steam overlay invite dialog.
4. Invited friend accepts invite.
5. Friend receives `GameLobbyJoinRequested_t` and joins the lobby.
6. `LobbyEnter_t`, `LobbyChatUpdate_t`, and `LobbyDataUpdate_t` refresh visible lobby state.
7. Local party slot character IDs are periodically written to lobby member data.

## Constraints

- `steam_appid.txt` contains `480` for local Spacewar smoke testing only.
- Real Steam release must replace 480 with the assigned AppID and ship through Steam without relying on the local txt file.
- Code compiles without Steamworks.NET by using `STEAMWORKS_NET` guards, but real invite behavior requires the package to resolve.
- Existing battle logic is not changed.

## Verification

- Compile project after Unity resolves the package.
- Manual two-PC test:
  - Steam is running on both PCs.
  - Two different Steam accounts are logged in.
  - Accounts are Steam friends.
  - Both run the same build with `steam_appid.txt` set to `480`.
  - Host clicks Invite and sees the Steam friend invite overlay.
  - Client accepts and appears in the lobby member list.
