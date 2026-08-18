# Event_04 장착 유물 삭제 패널 씬 배치 전환 구현 계획

1. `EventEquippedRelicSelectionPanelUITests`를 씬 배치형 계층 기준으로 갱신한다.
2. 현재 런타임 생성 구현에서 실패하는 테스트를 확인한다.
3. `EventEquippedRelicSelectionPanelUI`를 별도 스크립트와 `.meta`로 분리한다.
4. 패널 컴포넌트에서 fallback hierarchy 생성을 제거하고, 씬 참조와 항목 템플릿만 사용하게 변경한다.
5. `EventRoomController`의 패널 보장 로직에서 런타임 생성 부분을 제거한다.
6. `Battle.unity`의 `DataEventRoot` 아래에 비활성 패널과 목록 항목 템플릿을 배치한다.
7. MSBuild와 테스트 소스 컴파일로 검증한다.
