# 이벤트 공통 선택지와 선택지별 연출 슬롯 설계

## 목표

- 단계형 이벤트에서 동일한 종료 선택지를 행마다 복제하지 않는다.
- 이벤트 선택지 최대 5개를 기준으로 성공/실패 연출 액션 10개를 명시적으로 제공한다.

## 공통 선택지

`EventData.PersistAcrossNextEvent`가 `true`인 선택지는 현재 이벤트 세션의 후속 `NextEventId` 단계에도 계속 표시한다. 새 이벤트룸 진입 시 목록을 초기화하므로 다른 이벤트로 누출되지 않는다.

Event_08은 다음 네 행만 사용한다.

1. Event_08 / 1차 공명
2. Event_08_A / 2차 공명
3. Event_08_B / 3차 공명
4. Event_08 / 돌아선다 / `PersistAcrossNextEvent = true`

## 연출 액션 ID

각 이벤트 프리팹은 아래 10개 액션 슬롯을 제공한다.

- `event_choice_01_success`, `event_choice_01_failure`
- `event_choice_02_success`, `event_choice_02_failure`
- `event_choice_03_success`, `event_choice_03_failure`
- `event_choice_04_success`, `event_choice_04_failure`
- `event_choice_05_success`, `event_choice_05_failure`

Excel에는 10개의 별도 열을 만들지 않는다. 선택지 한 행이 이미 하나의 선택지를 의미하므로 각 행의 `SuccessVisualActionId`와 `FailureVisualActionId`에 해당 ID를 기록한다. 사용하지 않는 결과는 빈 셀로 둔다.

## 멀티플레이 경계

공통 선택지 합성은 UI 표시 데이터이며 전투 상태를 바꾸지 않는다. 선택 결과는 기존 `EventChoiceExecutionService`가 계산하고 안정적인 비주얼 ID와 액션 ID만 프레젠테이션으로 전달한다.
