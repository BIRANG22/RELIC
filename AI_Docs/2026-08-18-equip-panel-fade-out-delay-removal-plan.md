# Equip Panel Fade Out Delay Removal Plan

1. Add regression coverage that a large closeDelayAfterApply does not delay applied reward close.
2. Change BattleRewardEquipPanelUI.BeginAppliedPreviewAndClose to start closing immediately.
3. Remove the now-unused close delay coroutine and field from code if no longer referenced.
4. Build Assembly-CSharp and Assembly-CSharp-Editor.
5. Report Unity Test Runner limitations caused by the no-batchmode rule.
