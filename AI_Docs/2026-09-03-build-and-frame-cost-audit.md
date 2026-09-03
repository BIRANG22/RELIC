# Build And Frame Cost Audit

## Discord Partner SDK Build Error

Observed error:

`Cannot open file 'Packages/com.discord.partnersdk/Runtime/Plugins/x86_64/Debug/discord_partner_sdk.dll.meta' for write`

Checked state:

- `Packages/com.discord.partnersdk` is an embedded local package, not a symlink.
- `discord_partner_sdk.dll.meta` exists.
- The file is not read-only.
- Folder and file ACLs allow the current user and Codex sandbox users to modify the file.

Conclusion:

The current filesystem state does not show a persistent read-only permission problem. The failure is most likely a transient file lock during Player Build, commonly from Unity import/build state, IDE indexing, antivirus scanning, sync tooling, or another Unity process.

Manual recovery order:

1. Close extra Unity/IDE instances using this project.
2. Retry Player Build after waiting for package import to finish.
3. If it still fails, close Unity and delete only `Packages/com.discord.partnersdk/Runtime/Plugins/x86_64/Debug/discord_partner_sdk.dll.meta`, then reopen Unity so the meta is regenerated.
4. If the package keeps failing, reimport or replace only the embedded `Packages/com.discord.partnersdk` package.
5. Delete the whole `Library` folder only as a last resort.

## Safe Code Changes

- `YSortSprite`: keep LateUpdate, but avoid sortingOrder writes unless Y-sort inputs change.
- `InputManager`: cache `Camera.main` and reacquire only when the cached camera is gone.
- `MonsterHUDSlot`: cache the follow camera and reacquire only when missing.
- `LobbyCultureTankPanelPresenter`: remove per-frame full refresh; keep immediate refreshes and throttle passive refresh to 0.25 seconds.
- `BattleWorldVfxHandle`: skip LateUpdate work for fully static handles without follow or sorting target.

## Report-Only Candidates

- `BattleWorldVfxRenderer` creates a RenderTexture, Camera, Material, proxy GameObject, and VFX instance per IndividualWorldRenderTexture spawn.
- `BattleUnitAnimator` has repeated Instantiate/Destroy paths for skill VFX, projectile missiles, impacts, and target unit VFX.
- Status icon UI in battle panels destroys and recreates icons on refresh.
- Canvas/layout rebuild costs should be checked in Profiler before prefab structure changes.
