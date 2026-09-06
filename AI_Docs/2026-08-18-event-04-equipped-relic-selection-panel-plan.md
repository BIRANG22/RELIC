# Event_04 장착 유물 선택 패널 구현 계획

1. `EventEquippedRelicSelectionPanelUITests`에 패널 열기, 선택, 취소 테스트를 추가한다.
2. `EventEquippedRelicSelectionPanelUI`와 표시 엔트리 모델을 추가한다.
3. 패널이 런타임 자동 생성 UI를 구성하고, 전체 장착 유물 목록을 스크롤 가능한 그리드로 표시하게 한다.
4. `EventRoomController`의 선택지 슬롯 재사용 흐름을 패널 오픈 흐름으로 교체한다.
5. 선택된 장착 유물 비용은 기존 `EventChoiceExecutionService` 실행 경로로 전달해 삭제와 랜덤 보상이 순서대로 처리되게 한다.
6. 빌드와 정적 검증을 수행한다.
