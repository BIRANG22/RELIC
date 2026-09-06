# RestRoom Map_26 표시 흐름 보정 설계

`SharedRoomRoot`가 RestRoom 진입 중 활성화될 때 `MapRoomController`가 레이어 전용 배경을 다시 표시하여 Map_26 전용 배경을 덮어쓰는 문제를 보정한다.

- `StageBackgroundController`는 Map ID가 있는 Background Range를 레이어 범위보다 우선한다.
- `MapRoomController`는 현재 노드의 Map ID를 배경 표시 요청에 전달한다.
- `MapRoomController`는 `AllyRoot를 사용하지 않을 Map ID` 목록을 제공한다. 목록에 있는 Map ID에서는 AllyRoot를 비활성화하고 맵용 아군을 생성하지 않는다.
- 기존 `StageBackgroundController`의 SpawnRoot 우회 목록과 관련 동작은 제거한다. 배경은 기존 SpawnRoot 생성 규칙을 사용한다.
- Battle 씬에는 `Map_26`을 AllyRoot 비사용 목록으로 설정한다.
