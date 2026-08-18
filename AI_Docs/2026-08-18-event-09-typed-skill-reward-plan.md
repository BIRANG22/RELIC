# Event_09 타입별 기억 보상 구현 계획

1. `OfferChoice` 타입별 기억 보상이 선택 가능하고 지정 개수만큼 콜백을 호출하는 테스트를 추가한다.
2. `EventChoiceExecutionService`에 `OfferChoice` 기억 보상 판정과 타입/개수 파싱, 실행 컨텍스트 콜백을 추가한다.
3. `EventRoomController`에 타입별 랜덤 기억 후보 수집과 보상 큐잉을 구현한다.
4. Event_09가 기존 `BattleRewardPanelUI` 흐름으로 전리품 패널을 열도록 연결한다.
5. MSBuild로 런타임/에디터 어셈블리 컴파일을 검증한다.
