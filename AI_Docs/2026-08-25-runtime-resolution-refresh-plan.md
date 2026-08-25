# Runtime Resolution Refresh Plan

## Problem

Runtime resolution changes can leave scene-wide UI and cameras in a broken layout, while the initially applied resolution works. The existing `ResolutionManager` applies letterboxing and canvas viewport fitting for a fixed five-frame window after `Screen.SetResolution`.

## Root Cause Hypothesis

`Screen.SetResolution`, root Canvas rect updates, and CanvasScaler layout passes can settle on different frames. A fixed short refresh can apply viewport scale using stale or zero Canvas sizes, then stop before Unity finishes resizing the window and rebuilding UI.

## Recommended Design

1. Keep the current 16:9 target viewport and letterbox architecture.
2. Replace the fixed five-frame refresh with a stability-based refresh that keeps applying until screen size and root Canvas sizes are stable for several frames, with a max frame cap.
3. Run the same refresh path for explicit resolution changes, detected screen size changes, fullscreen mode changes, and scene load.
4. Add defensive viewport fitting so invalid Canvas sizes do not become final layout state.
5. Add EditMode tests under `Assets/Tests/EditMode~/` for the new refresh policy and fitter behavior.

## Scope

- Update `Assets/Project/Scripts/Core/Managers/ResolutionManager.cs`.
- Update `Assets/Tests/EditMode~/ResolutionManagerTests.cs`.
- No changes to combat result logic, networking logic, prefabs, scenes, branch, commit, push, or PR.
