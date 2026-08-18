# Battle Reward Equip Blur Design

## Problem

BattleRewardEquipPanelUI opens on top of BattleRewardPanelUI when a skill or relic reward is selected.
The equip panel already uses UIBlurBackground, but the Battle scene does not assign BattleRewardPanelUI or BattleHUDCanvas to blurredUiRoots.
As a result, the world/background is blurred while the reward panel and HUD remain visually sharp behind the equip panel.

Lobby solves a similar case by assigning CharacterSetting to UIBlurBackground.blurredUiRoots in the scene.
Battle should reuse that same blur pipeline instead of adding a separate visual system.

## Design

- Keep UIBlurBackground as the single blur/capture component.
- Add runtime blur roots to UIBlurBackground and merge them with inspector-assigned roots.
- Before BattleRewardEquipPanelUI activates, collect active BattleRewardPanelUI objects and active Canvas objects named BattleHUDCanvas.
- Assign those objects as runtime blur roots on every UIBlurBackground under the equip panel.
- Let the existing UIBlurBackground enable/disable lifecycle capture and restore the blurred sources.

## Scope

- UI presentation only.
- No reward, inventory, skill, relic, or battle state behavior changes.
- No scene rewiring is required for BattleRewardPanelUI or BattleHUDCanvas references.

## Tests

- Verify UIBlurBackground merges inspector roots and runtime roots while removing duplicates.
- Verify BattleRewardEquipPanelUI assigns active reward panel and BattleHUDCanvas roots before opening.
