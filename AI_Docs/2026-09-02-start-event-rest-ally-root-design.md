# Start 이벤트 및 RestRoom AllyRoot 보정 설계

- `Map_25`는 CSV의 `Type = Start`, `EventId = Event_09` 데이터다. 시작 노드에서 이 타입을 `OpenSpecialEvent`로 라우팅해 이벤트 방을 연다.
- RestRoom을 열 때 BattleSceneController가 AllyRoot를 다시 켜는 호출 뒤에 `MapRoomController.RefreshForMap(Map_26)`를 실행한다. `skipAllyRootMapIds`가 최종 상태를 결정하므로 Map_26에서는 AllyRoot가 비활성화된다.
