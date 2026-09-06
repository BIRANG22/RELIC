# Event Room Static Reward Panel Plan

## Goal

이벤트룸 선택지에서 획득하는 유물, 기억, 레드 더스티움 보상을 전투방 전용 `BattleRewardPanelUI`가 아니라 이벤트룸에 씬 배치된 전용 보상 패널로 지급한다.

## Root Cause

- `BattleRewardPanelUI`는 `BattleRewardCanvas` 아래의 전투방 전용 UI이다.
- 이벤트룸 진행 중에는 전투방 UI 계층이 꺼지거나 스케일 0 상태일 수 있으므로 `BattleRewardPanelUI.Open()`을 호출해도 화면에 보이지 않는다.
- `BattleRewardPanelUI`에는 `BattleRoomCleaner`, `BattleRewardCollector`, 전투 노드 클리어 등 배틀룸 종료 흐름이 포함되어 이벤트룸 흐름과 책임이 섞인다.

## Design

- `EventRoomRewardPanelUI`를 새로 추가한다.
- 기존 보상 슬롯 프리팹, 레드 더스티움 아이콘, 획득 사운드, 유물/기억 장착 패널 호출 방식은 유지한다.
- 완료 처리는 배틀룸 정리 없이 콜백만 호출한다.
- `EventRoomController`는 `EventRoomRewardPanelUI`를 참조하고, 씬에 미리 배치된 패널만 찾아서 연다.
- 런타임에 보상 패널 GameObject를 생성하지 않는다.
- `Assets/Project/Scenes/YDM/Battle.unity`의 이벤트룸 Canvas 아래에 기존 보상 패널 이미지를 복제한 `EventRoomRewardPanelUI` 오브젝트를 정적으로 배치한다.

## Test Plan

- 새 테스트에서 `EventRoomController`의 보상 패널 필드가 `EventRoomRewardPanelUI` 타입인지 검증한다.
- 새 테스트에서 `EventRoomRewardPanelUI` 타입이 존재하고 `Open(List<BattleRewardData>, Action)` 인터페이스를 제공하는지 검증한다.
- MSBuild로 전체 C# 컴파일을 확인한다.
- `git diff --check`로 공백 오류를 확인한다.

## Multiplayer Boundary

보상 계산과 선택 결과는 `EventChoiceExecutionService`가 만들고, 이벤트룸 보상 패널은 이미 생성된 `BattleRewardData`를 표시하고 획득 처리만 수행한다. UI가 선택 결과를 계산하지 않는다.
