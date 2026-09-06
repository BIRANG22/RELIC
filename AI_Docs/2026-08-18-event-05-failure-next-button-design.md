# Event_05 실패 진행 버튼 처리 설계

## 배경

Event_05의 1번/2번 선택지는 주사위 판정에 성공하면 같은 Event_05로 돌아가 추가 채굴을 진행한다. 실패할 때도 데이터에는 `NextEventId=Event_05`가 들어 있어 현재 컨트롤러가 실패 결과를 후속 이벤트로 해석하고 선택지를 다시 활성화한다.

## 목표

- Event_05의 1번 또는 2번 선택지가 판정 실패하면 결과 문구를 보여주고 진행 버튼을 표시한다.
- 실패 후에는 다른 선택지를 다시 고를 수 없게 유지한다.
- 성공 시에는 기존처럼 Event_05를 다시 로드해 추가 채굴을 이어간다.

## 설계

`EventChoiceExecutionResult`에 선택 실행 수락 여부와 별개인 `Succeeded` 플래그를 추가한다. 선택 조건 부족처럼 실행되지 않은 경우는 `Accepted=false`, 주사위/확률 판정 실패는 `Accepted=true`와 `Succeeded=false`로 구분한다.

`EventRoomRewardFlowUtility`에 Event_05 1번/2번 실패를 종료 처리해야 하는지 판단하는 순수 함수를 둔다. `EventRoomController.ExecuteEventChoice`는 이 함수가 true인 경우 `NextEventId`를 따라가지 않고 현재 이벤트를 해결된 상태로 전환한다.

선택지 잠금은 기존 `ExecuteEventChoice` 시작부의 `SetChoiceSlotsInteractable(false)`를 그대로 사용한다. 실패 종료 경로에서 `BindChoiceSlots`를 다시 호출하지 않으므로 다른 선택지가 재활성화되지 않는다.

## 검증

- 실행 서비스 테스트로 주사위 실패가 `Accepted=true`, `Succeeded=false`임을 고정한다.
- 컨트롤러 테스트로 Event_05 1번 실패 후 진행 버튼이 표시되고 선택 버튼이 잠기는 흐름을 고정한다.
