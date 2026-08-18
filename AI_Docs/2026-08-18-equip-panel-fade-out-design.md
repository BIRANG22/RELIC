# Equip Panel Fade Out Design

## Problem

Equip_panel fades in because the scene object has UIFadeInOnEnable.
When the reward is resolved, BattleRewardEquipPanelUI calls SetActive(false) immediately in FinishResolvedReward.
That bypasses any fade out and makes the panel disappear abruptly.

## Design

- Reuse UIFadeInOnEnable as the visual transition owner for the panel.
- Add a public method that fades all tracked Graphics from their current alpha to zero, then deactivates the GameObject.
- During fade out, disable interaction through CanvasGroup so the closing panel cannot receive input.
- Keep the existing OnDisable restore behavior so the next OnEnable can start from a clean fade-in state.
- BattleRewardEquipPanelUI should request fade out in FinishResolvedReward and invoke the reward callback only after deactivation.
- If UIFadeInOnEnable is missing or cannot run, BattleRewardEquipPanelUI falls back to the existing immediate SetActive(false) path.

## Scope

- Presentation-only change.
- Reward resolution, skill equip, relic equip, and remnant extraction results are unchanged.
- No battle state or multiplayer synchronization data is added.
