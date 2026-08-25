# Event Dice Roll Presentation

## Goal

이벤트룸의 `ChoiceType=Dice` 선택지에서 기존처럼 3d6 합계로 결과를 계산하되, 결과 텍스트 표시 전에 주사위 3개 연출을 먼저 보여준다.

## Design

- 전투 결과 계산은 `EventChoiceExecutionService`에 유지한다.
- `EventChoiceExecutionResult`는 합계(`DiceRoll`)와 개별 면(`DiceFaces`)을 함께 반환한다.
- `EventRoomController`는 Dice 선택지만 `EventDiceRollPresenter`를 통해 연출한 뒤, 동일한 실행 흐름으로 결과 적용, 비주얼 액션, 보상 패널, 다음 이벤트 전환을 처리한다.
- 프리젠터가 씬/프리팹에 없으면 기존 즉시 실행 흐름으로 폴백한다.
- 주사위 연출 프리팹은 `Assets/Project/Data/MapVisual/EventDiceRollPresenter.prefab`에 두고, Animator와 1~6 스프라이트 참조는 비워 둔다.

## Multiplayer Boundary

- 주사위 값은 `BattleRandom` 기반으로 생성하며 UI 프리젠터는 결과 계산을 하지 않는다.
- UI는 전달받은 결과 면을 표시만 하므로 전투 핵심 결과는 `Command -> State Change -> Result/Event` 흐름을 유지한다.
