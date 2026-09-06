# Battle Map Node Hover Info Implementation Plan

1. 타입별 이름·설명과 호버 표시 테스트를 추가한다.
2. BattleMapNodeInfoPresenter를 구현한다.
3. MapNodeView와 MapViewSpawner에 호버 이벤트를 추가한다.
4. BattleMapPanel과 Battle.unity에 Presenter 참조를 연결한다.
5. NodePrefab의 부모 Image를 투명 입력 영역으로, 자식 Icon을 실제 아이콘으로 연결한다.
6. 컴파일과 프리팹·씬 참조를 검증한다.
