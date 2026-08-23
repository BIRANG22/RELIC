# DebugBattle GridEffectTooltipUI Warning Plan

1. Confirm DebugBattle scene does not contain `GridEffectTooltipUI`.
2. Add a scene asset regression test requiring `GridEffectTooltipUI` in DebugBattle.
3. Add the Battle scene tooltip UI group to DebugBattle's main `BattleHUDCanvas`.
4. Verify the scene contains the tooltip object, script GUID, parent child reference, and text references.
5. Run compile/diff checks without Unity batchmode tests.
