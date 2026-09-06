# Equip Panel Fade Out Plan

1. Add EditMode coverage for UIFadeInOnEnable exposing a fade-out close API.
2. Add EditMode coverage that BattleRewardEquipPanelUI keeps the panel active until fade-out is requested.
3. Update UIFadeInOnEnable to support fade-out deactivation with optional completion callback.
4. Update BattleRewardEquipPanelUI.FinishResolvedReward to use the fade-out API when present.
5. Build Assembly-CSharp and Assembly-CSharp-Editor.
6. Report verification limits caused by the no-batchmode Unity rule.
