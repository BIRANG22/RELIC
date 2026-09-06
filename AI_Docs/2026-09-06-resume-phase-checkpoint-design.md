# Resume Phase Checkpoint Design

## Goal

현재 Runtime 상태와 `ResumeData`를 함께 저장하여 Continue 시 저장 시점의 전투·이벤트·보상·휴식 단계를 재실행 없이 복원한다.

## Boundaries

- 일반 진행 저장은 `SaveSystem.SaveCheckpoint(ResumeData)`가 현재 Runtime snapshot을 저장한다.
- BattleEntry rollback snapshot은 메모리 전용의 전투 재시작 기준점이며, 일반 저장 API가 이를 생성·갱신하지 않는다.
- 전투 결과를 바꾸는 순서는 `결과 확정 -> Runtime 반영 -> ResumeData 갱신 -> Save -> UI/VFX`로 통일한다.
- UI는 `ResumeData`를 계산하거나 변경하지 않고 저장된 결과만 표시한다.

## Resume Payload

`ResumeData`는 `Phase`, 현재 `NodeIndex`/`MapId`, `EventId`를 공통 키로 보관한다.

- `BattleEntry`: 초기 GridEffect와 MonsterCommand. BattleRoom 초기화가 끝난 뒤 저장한다.
- `BattleReward`: 확정 보상과 수령 보상. 재개 시 Battle을 실행하지 않고 패널을 연다.
- `EventEntry`: 확정 EventId와 Event runtime session 상태. 재개 시 선택 전 UI를 연다.
- `EventChoice`: 선택 ID, 적용 완료 여부, 확정 확률 결과, 후속 EventId, 미수령 보상. 재개 시 Execute를 호출하지 않는다.
- `EventDice`: EventId, 선택 ID, 확정 DiceFaces. 재개 시 그 결과를 Presenter에 주입한다.
- `Rest`: 기존 Rest 처리 완료 상태를 보존한다.

보상은 안정적인 `BattleRewardSaveData` 값으로 직렬화하며, 지급 직후 수령 목록을 저장한다. 동일한 `RewardId`/Type/Amount 보상은 미수령 목록에서 한 번만 처리한다.

## Restore Router

`BattleSceneController`가 Continue Runtime 적용 뒤 pending `ResumeData`를 소비한다.

1. `BattleEntry`: 기존 BattleRoom loader를 통해 초기 상태를 적용한다.
2. `BattleReward`: BattleRoom 실행을 생략하고 저장된 보상 패널을 연다.
3. `EventEntry`: 저장된 EventId로 EventRoom을 열고 선택지 UI를 구성한다.
4. `EventChoice`: Event runtime을 복원하고 저장된 후속 UI 또는 보상 패널을 연다.
5. `EventDice`: 저장 DiceFaces로 Dice Presenter를 복원하고 확인 시에만 이미 확정된 선택 결과를 적용한다.
6. `Rest`: 기존 Rest room 상태를 다시 구성한다.

복원 과정에서 autosave는 억제하며, 복원이 완료된 뒤에만 해제한다.

## Tests

EditMode 테스트는 `ResumeData` 직렬화, 보상 변환/수령 상태, Dice 결과 보존, phase별 Continue 라우팅 선택을 검증한다. 씬/프리팹 의존 경로는 기존 테스트 패턴을 사용해 최소 범위로 검증한다.
