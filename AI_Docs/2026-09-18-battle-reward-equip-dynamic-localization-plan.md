# Battle Reward Equip Panel Dynamic Localization Plan

1. 실패하는 EditMode 회귀 테스트로 Equip_panel의 세 동적 텍스트 보호를 정의한다.
2. `BattleRewardEquipPanelUI`가 바인딩 직후 세 텍스트를 보호하도록 구현한다.
3. 배틀씬 `Equip_panel`의 정적 `LocalizedTMPText` 세 개를 제거하고 `LocalizationIgnore`를 추가한다.
4. EditMode 테스트와 C# 컴파일을 검증한다.
