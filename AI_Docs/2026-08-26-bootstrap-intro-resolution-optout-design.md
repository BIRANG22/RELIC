# Bootstrap Intro Resolution Opt-out Design

## Problem

Bootstrap intro UI uses its own overlay canvas and should keep a fixed visual size when the game window resolution changes. The global `ResolutionManager` currently fits every active root canvas into the selected resolution viewport, so the intro canvas is wrapped by `ResolutionCanvasViewportFitter` and scaled during resolution refresh.

## Recommended Design

- Add a small marker component for canvases that should not be fitted by `ResolutionManager`.
- Make `ResolutionManager.ShouldFitCanvasForResolution` return `false` when the root canvas or one of its parents has the marker.
- Add the marker to the bootstrap intro canvas at runtime from `IntroSequenceController`, so existing scene references remain stable.
- Add an EditMode test that proves marked canvases are excluded from fitting.

## Scope

- UI-only behavior.
- No changes to combat state, result calculation, networking, rewards, map generation, or seeded random logic.
