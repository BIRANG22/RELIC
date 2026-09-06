# IntroSequence Resolution Fix

## Problem

`IntroSequenceController` shows the Bootstrap intro UI from a dedicated intro root. The intro art is authored in a 1920x1080 coordinate space, but the intro root was marked as `ResolutionCanvasFitOptOut`, causing it to bypass the fixed-resolution viewport applied by `ResolutionManager`.

When the game resolution changes, the intro canvas uses raw screen pixel sizing while its children still use 1920x1080-authored positions and scales. This makes the intro appear to change size.

## Design

- Keep the intro canvas eligible for `ResolutionManager` viewport fitting.
- Do not automatically attach `ResolutionCanvasFitOptOut` to the intro root.
- Set the intro root `CanvasScaler` to `Scale With Screen Size` with a 1920x1080 reference resolution.
- Preserve existing sorting behavior so the intro remains above ordinary UI.

## Multiplayer Impact

This is UI presentation only. It does not affect battle state, randomness, commands, results, or synchronization boundaries.
