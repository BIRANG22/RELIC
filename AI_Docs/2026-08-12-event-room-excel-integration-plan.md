# Event Room Excel Integration Plan

1. `Event` 데이터 모델, ID 정규화 유틸, 로더, DB를 추가한다.
2. `DataBootstrap`과 `DataManager`에서 `EventDatabase`를 초기화 및 노출한다.
3. `GeneratedMapNodeData`, 수동/절차 맵 생성, `BattleSceneController`에 `EventId` 전달 흐름을 연결한다.
4. `EventRoomController`에 데이터 이벤트 모드를 추가하고 기존 상자 모드는 유지한다.
5. `GameData.xlsx`의 `EventMap` 시트를 `Event`로 바꾸고 `EventMaster`, `EventChoice` 시트를 삭제한다.
6. `GameDataRuntime.csv`를 재생성하거나 동일 구조로 정리한다.
7. EditMode 테스트와 MSBuild로 컴파일 검증을 수행한다.
