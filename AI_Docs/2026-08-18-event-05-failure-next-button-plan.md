# Event_05 실패 진행 버튼 처리 구현 계획

1. `Assets/Tests/EditMode~/Event05FailureFlowTests.cs`에 실패 회귀 테스트를 추가한다.
   - 주사위 실패 결과가 `Succeeded=false`를 반환하는지 확인한다.
   - Event_05 1번 실패 후 `NextButton`이 표시되고 선택 버튼들이 잠기는지 확인한다.
2. 테스트 컴파일로 현재 구현이 실패하는지 확인한다.
3. `EventChoiceExecutionResult`에 `Succeeded` 플래그를 추가한다.
4. 주사위/확률 실패 반환에서 `Succeeded=false`를 세팅한다.
5. `EventRoomRewardFlowUtility`에 Event_05 1번/2번 실패 종료 정책을 추가한다.
6. `EventRoomController.ExecuteEventChoice`에서 해당 정책이면 `NextEventId`를 따라가지 않고 이벤트 해결 상태로 종료한다.
7. MSBuild와 테스트 소스 컴파일로 검증한다.
