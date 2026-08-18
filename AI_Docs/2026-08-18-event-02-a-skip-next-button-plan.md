# Event_02_A 진행 버튼 스킵 구현 계획

1. `EventRoomRewardFlowUtility`에 스킵 가능한 미해결 이벤트 판정 함수를 추가한다.
2. `EventRoomController.LoadEventDefinition`에서 스킵 가능한 이벤트일 때 진행 버튼을 표시한다.
3. `EventRoomController.OnNextButtonClicked`에서 스킵 가능한 미해결 이벤트는 기존 종료/보상 패널 흐름으로 진입시킨다.
4. `Assets/Tests/EditMode~/EventRoomRewardPanelFlowTests.cs`에 `Event_02_A` 스킵 가능/일반 이벤트 스킵 불가 테스트를 추가한다.
5. 빌드 및 정적 검증을 수행한다.
