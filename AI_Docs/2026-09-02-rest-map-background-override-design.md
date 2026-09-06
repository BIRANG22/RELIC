# RestRoom Map_26 배경 오버라이드 설계

## 목적

RestRoom을 일반 SharedRoom 배경과 구분한다. `Map_26`이 열릴 때는 `Stage_01`에 설정한 전용 배경 프리팹을 항상 사용하고, 해당 프리팹은 기존 `SpawnRoot`가 아닌 `Stage_01` 아래에 생성한다.

## 데이터 전제

- `GameData.xlsx`의 `Map` 시트 및 `GameDataRuntime.csv`에는 이미 `Map_26, Rest, Rest, 0, 0, Stage1, 1`이 있다.
- 전투 맵 ID와 이벤트 ID는 모두 `0`이며, 맵 타입은 `Rest`다.
- 이 작업에서는 위 데이터와 `Share_Restroom.prefab`의 내용은 수정하지 않는다.

## 동작 설계

1. `StageBackgroundController.BackgroundRange`에 선택적 `MapId`를 추가한다.
2. 배경 표시 요청이 Map ID를 전달하면, 동일한 `MapId`를 가진 범위를 레이어 범위보다 먼저 선택한다.
3. Map ID가 일치하지 않으면 Map ID가 비어 있는 기존 레이어 범위 규칙을 그대로 사용한다.
4. `bypassSpawnRootMapIds` 목록에 있는 Map ID는 배경 인스턴스를 `SpawnRoot` 대신 컨트롤러 자신(`Stage_01`) 아래에 생성한다.
5. `BattleSceneController`는 Room을 열 때 노드의 `MapId`와 `LayerIndex`를 함께 전달한다. 맵 화면의 기존 레이어 전용 호출은 유지해 일반 배경 표시와 호환한다.

## Inspector 설정

`Battle.unity`의 `SharedRoomRoot/Background/Stage_01`에 다음을 설정한다.

- `Background Ranges`: `MapId = Map_26`, `Prefab = Share_Restroom`
- `Bypass Spawn Root Map Ids`: `Map_26`

이 구조는 이후 다른 전용 방도 enum이나 코드 분기 없이 Map ID와 프리팹 연결만으로 확장할 수 있다.
