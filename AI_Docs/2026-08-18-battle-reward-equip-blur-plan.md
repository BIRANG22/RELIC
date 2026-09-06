# Battle Reward Equip Blur Plan

1. Add an EditMode test for runtime blur roots on UIBlurBackground.
2. Add an EditMode test for BattleRewardEquipPanelUI collecting BattleRewardPanelUI and BattleHUDCanvas.
3. Run the targeted tests and confirm the new tests fail before production changes.
4. Add runtime blur root support to UIBlurBackground.
5. Add BattleRewardEquipPanelUI blur target assignment before SetActive(true).
6. Run targeted EditMode tests or the available compile/test alternative in the open Unity workflow.
7. Report changed files, verification, and multiplayer impact.
