# StartRoom Event_09 기억 보상 설계

## 목표
- 시작룸은 EventData를 읽지 않는다.
- 시작룸의 기존 선택 흐름에 Event_09와 같은 동작을 직접 제공한다.
- 선택지는 공격 관련 기억, 버프 관련 기억, 디버프 관련 기억 3개다.
- 선택 시 해당 `SkillType`의 보유하지 않은 코어 기본 기억 2개를 전리품 패널에 띄운다.

## 설계
- `RelicChoiceAreaUI`는 시작룸 선택 UI를 유지하되, 시작룸에서는 유물 ID 대신 고정된 `StartRoomSkillRewardChoice`를 슬롯에 표시한다.
- 후보 수집은 `StartRoomSkillRewardSelectionUtility`로 분리한다.
- 후보 조건은 `Category.Core`, 선택한 `SkillType`, 기본형 기억, 미보유 기억이다.
- 미보유 판정에는 전투 기억 인벤토리, 캐릭터 장착 기억, 캐릭터 기본 기억 참조, 그리고 기본/강화 짝 기억을 포함한다.
- 보상 선택 랜덤은 `BattleRandom.Range`를 사용한다.
- 전리품 표시와 장착 처리는 기존 `BattleRewardPanelUI`와 `BattleRewardEquipPanelUI` 흐름을 재사용한다.

## 범위
- 시작룸이 EventData를 읽도록 바꾸지 않는다.
- 시작룸 씬의 기존 선택 슬롯 배치는 유지한다.
- 유물 선택용 기존 유틸리티는 과거 테스트와 참조 안정성을 위해 남긴다.
