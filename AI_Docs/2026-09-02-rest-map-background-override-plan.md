# RestRoom Map_26 배경 오버라이드 구현 계획

1. `StageBackgroundController`의 실제 프리팹 생성 결과를 검증하는 EditMode 테스트를 추가한다.
   - Map ID 범위가 일반 레이어 범위보다 우선한다.
   - Map_26은 `SpawnRoot`를 우회해 `Stage_01` 아래에 생성된다.
2. 컨트롤러에 Map ID 우선 선택 API와 SpawnRoot 우회 목록을 구현한다. 기존 `ShowForLayer` API의 동작은 유지한다.
3. `BattleSceneController`가 룸 배경을 표시할 때 노드 Map ID를 전달하도록 수정한다.
4. `Battle.unity`의 `Stage_01` 인스펙터 데이터에 `Map_26`과 `Share_Restroom.prefab`을 연결한다.
5. 게임 스크립트와 EditMode 테스트 컴파일을 확인하고, 씬 YAML 및 런타임 데이터 연결을 재검증한다.
