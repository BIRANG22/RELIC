# StartRoom Event_09 기억 보상 구현 계획

1. 타입별 기억 보상 후보를 수집하는 EditMode 테스트를 추가한다.
2. `StartRoomSkillRewardSelectionUtility`를 추가해 기본 선택지와 후보 필터링을 제공한다.
3. `RelicChoiceSlotUI`에 기억 타입 선택지 표시와 클릭 전달을 추가한다.
4. `RelicChoiceAreaUI`의 시작룸 `Open` 흐름을 기억 타입 선택으로 전환한다.
5. 선택 타입 기준으로 보유하지 않은 기억 2개를 `BattleRandom`으로 고르고 `BattleRewardPanelUI`에 전달한다.
6. 전리품 패널 완료 후 기존 시작룸 완료 흐름으로 복귀한다.
7. 런타임 빌드와 diff 검사를 수행한다.
