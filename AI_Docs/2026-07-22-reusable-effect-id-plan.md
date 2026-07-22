# Reusable Effect ID Implementation Plan

Goal: Replace placeholder `E_Value` usage with reusable effect IDs and route relic/rune/item/grid effects through shared battle hooks.

Architecture: Keep existing skill and monster effect IDs intact. Add reusable equipment effect IDs for stat deltas, slot reservation modifiers, turn-start effects, and active relic behaviors. Route equip/relic/rune effects through `BattleEquipmentEffectService` using parsed `EffectEntries` so future relic data can compose effects with `;`.

Global Constraints:
- Documents stay in `AI_Docs`.
- Tests stay under `Assets/Tests/EditMode~/`.
- No commit or PR without user approval.
- Core battle logic stays separated from UI/VFX and uses runtime data IDs where possible.

Tasks:
- Add EditMode tests for reusable max HP, max cost, cost recovery, move value, slot cost/value/count modifiers, turn-start armor/status, and active relic ID resolution.
- Implement common helpers in `BattleEquipmentEffectService` that read equipped rune/relic `EffectEntries` instead of hard-coding only relic IDs.
- Keep existing rune/relic behavior compatible while changing CSV IDs from `E_Value` to meaningful IDs.
- Update `ActiveRelicEffectResolver` to resolve explicit active relic effect IDs first.
- Add new Effect master rows and clean `Rune`, `Relic`, and `GridEffect` effect IDs in `Assets/Resources/Data/GameDataRuntime.csv`.
- Verify with targeted tests/build where possible; do not run Unity batchmode tests because the editor is assumed open.
