# Equip Panel Fade Out Delay Removal Design

## Problem

Equip_panel now fades out when closing, but applied rewards still wait for BattleRewardEquipPanelUI.closeDelayAfterApply before the fade begins.
The serialized value is 0.75 seconds in the Battle scene and prefab, so the panel appears to pause before fading out.

## Design

- Keep UIFadeInOnEnable fade-out behavior.
- Remove the pre-fade wait from the applied reward close path.
- Start FinishResolvedReward immediately after the equip result is applied and the button inputs are disabled.
- Leave the fade duration itself unchanged so the panel still disappears smoothly.
- Keep reward state changes and callback order unchanged: state is applied first, fade-out closes the panel, then the reward callback runs.

## Scope

- Presentation timing only.
- No reward, skill, relic, inventory, or battle state changes.
- Scene and prefab serialized closeDelayAfterApply values do not need to be edited because the applied close path will no longer read that delay.
