# 배틀 맵 노드 호버 정보 설계

- 노드 프리팹 부모 Image는 투명한 버튼 입력 영역으로만 사용하고 자식 Icon Image가 실제 노드 아이콘을 표시한다.
- MapNodeView의 Pointer Enter/Exit를 MapViewSpawner와 BattleMapPanel을 거쳐 BattleMapNodeInfoPresenter로 전달한다.
- 이동 불가능하거나 완료된 노드도 호버 정보를 표시한다.
- Node_Info는 호버 중 이름·아이콘·한 줄 설명을 표시하고 포인터가 빠지면 숨긴다.
- 타입별 이름은 시작, 휴식, 사건, 전투, 정예, 보스로 고정한다.
