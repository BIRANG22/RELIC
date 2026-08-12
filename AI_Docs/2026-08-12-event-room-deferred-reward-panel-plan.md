# 이벤트룸 보상 패널 연동 구현 계획

## 목표

이벤트룸에서 재화, 기억, 유물 획득 결과를 배틀룸 보상 패널 수령 흐름으로 처리한다.

## 작업 범위

- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs`
- 테스트: `Assets/Tests/EditMode~/EventRoomRewardPanelFlowTests.cs`
- 문서: `AI_Docs/2026-08-12-event-room-deferred-reward-panel-design.md`

## 구현 단계

1. `EventRoomController`에 `pendingEventRewards`, `BattleRewardPanelUI` 참조, 보상 패널 진행 중 플래그를 추가한다.
2. 이벤트 보상 콜백을 직접 지급 대신 `BattleRewardData` 생성으로 변경한다.
3. `OnEventChoiceClicked`에서 다음 이벤트가 없고 보류 보상이 있으면 즉시 보상 패널을 열도록 한다.
4. 보상 패널 완료 콜백에서 이벤트 노드를 클리어하고 맵으로 복귀한다.
5. 보상 패널을 열 수 없으면 기존 `NextButton` 완료 흐름으로 폴백한다.
6. 보류 보상 생성과 최종 이벤트 즉시 패널 호출을 EditMode 테스트로 검증한다.

## 검증 계획

- `EventRoomRewardPanelFlowTests`로 다음 이벤트 존재 여부에 따른 보상 보류/패널 호출을 검증한다.
- 기존 `EventChoiceExecutionServiceTests`로 즉시 효과와 누적 재화 동작 회귀를 확인한다.
- Unity 에디터가 열려 있다고 가정하므로 batchmode 테스트는 시도하지 않는다.
- 가능하면 MSBuild로 스크립트 컴파일 오류를 확인한다.
