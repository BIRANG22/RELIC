# RestRoom Map_26 표시 흐름 보정 계획

1. Map ID별 AllyRoot 표시 제어와 MapRoom 배경 갱신 동작을 검증하는 EditMode 테스트를 추가한다.
2. `StageBackgroundController`에서 SpawnRoot Map ID 우회 목록을 제거하고 Map ID 우선 배경 선택은 유지한다.
3. `MapRoomController`가 현재 Map ID로 배경을 갱신하고, 목록 기반으로 AllyRoot를 활성/비활성화하도록 수정한다.
4. Battle 씬의 `Map_26` 설정을 MapRoomController로 이동한다.
5. 컴파일과 씬 직렬화 연결을 검증한다.
