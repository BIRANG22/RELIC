# 전투 기억 보상 현지화 구현 계획

1. `BattlePresentationLocalizationRegressionTests`에 신규 키, 하드코딩 금지, 스킬 영어 설명 회귀 검사를 추가한다.
2. 실패를 확인한 뒤 `BattleRewardSkillPanelUI`, `BattleRewardSlotUI`, `EventRoomController`를 키 기반 출력으로 변경한다.
3. `Localization.xlsx`의 신규 키와 두 스킬 설명을 수정하고 Unity String Table에 동기화한다.
4. Battle 씬에서 기억 보상 패널 제목 TMP를 명시적으로 연결한다.
5. 회귀 검사와 C# 컴파일로 검증한다.
