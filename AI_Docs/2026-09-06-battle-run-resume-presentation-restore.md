# Battle Run Resume Presentation Restore

## 조사 기준

`ResumeData`는 마지막 체크포인트의 게임 런타임 스냅샷을 보완하는 최소 복원 메타데이터다. UI GameObject의 active 상태는 저장하지 않는다. Continue는 런타임을 먼저 복원한 뒤, 각 Room Controller가 아래 표의 presentation recipe를 적용한다.

## 현재 분기와 복원 정책

| Phase | Presentation | 저장 데이터 | Restore UI ON | 반드시 OFF |
|---|---|---|---|---|
| BattleReward | RewardPanel | MapId, NodeIndex, PendingRewards, 보상 수령 전 Runtime snapshot | BattleRewardPanel (저장 보상 bind) | MapPanel, MapSelectionPresenter, Event/Rest room, battle command/timeline transient, reward equip 초기 패널 |
| EventChoice | ChoiceList | 다음 EventId, 선택 실행 결과의 Runtime snapshot, 선택지 상태 | Event title, 다음 Event의 choice scroll | 이전 choice/result/dice/reward/selection/shop transient |
| EventChoice | ResultOnly | EventId, ResultMessage, ChanceSucceeded, NextButtonVisible | Event title/result, Next | choice scroll, dice, reward, relic/skill selection, shop |
| EventChoice | RewardPanel | EventId, SelectedChoiceId, PendingRewards | Shared BattleRewardPanel (저장 보상 bind) | choice scroll, result next, dice, relic/skill selection, shop |
| EventChoice | Shop | EventId, NextEventId, 저장 상점 재고 | Shop panel (저장 재고 bind) | choice scroll, dice, reward, relic/skill selection, Next |
| EventDice | DiceResolved | EventId, SelectedChoiceId, DiceFaces | Dice presenter resolved 값 + 확인 버튼 | choice scroll, result/next, reward, relic/skill selection, shop |

장착 유물 선택과 스킬 각성 선택은 현재 `ExecuteEventChoice` 전에 사용자가 입력할 비용/대상을 고르는 presentation이다. 해당 입력 전에는 EventChoice checkpoint를 만들지 않으며, 선택 실행 뒤의 확정 결과만 EventChoice checkpoint로 저장한다. 스킬 각성의 결과 애니메이션은 확정 결과를 표시하는 ResultOnly/RewardPanel로 정규화한다.

## 실제 정상 진행 흐름

- Battle 보상: `BattleResultChecker.OpenRewardPanel`에서 보상을 resolve한 직후 checkpoint를 만들고 패널을 연다. 이후 보상 slot 클릭은 다음 checkpoint가 아니므로 checkpoint를 갱신하면 안 된다.
- Event 보상: `EventChoiceExecutionService.Execute`가 Runtime/확률/보상을 확정한다. 이후 `ContinueAfterExecutedChoiceCore`가 다음 Event choice, terminal result, reward panel, Event_06 shop으로 분기한다.
- Dice: `TryBeginDiceRollChoice`가 roll 클릭 직후 dice faces를 확정하고 EventDice checkpoint를 저장한다. 결과 적용은 확인 클릭 뒤 `ExecuteEventChoice(... forcedDiceFaces)`에서 한 번만 실행한다.
- Event_06 shop: 기존 `RestRoomShopPanel.Open`은 매번 `CreateStock`을 호출한다. Continue 재추첨을 막기 위해 EventChoice Shop payload에 재고 ID/종류/가격을 저장하고, restore에서는 저장 재고를 bind해야 한다.

## 공통 Restore Recipe

1. Runtime State Restore는 기존 `SaveSystem.TryLoadBattleContinueProgress` 흐름을 유지한다.
2. `BattleSceneController.Start`의 `InitializeRuntime`, `CloseAllRooms`, `battleMapPanel.Prepare` 뒤에 pending Resume phase를 라우팅한다.
3. Room별 restore 시작 시 해당 Room의 transient UI만 canonical baseline으로 닫는다. MenuRoot, Setting, transition, tooltip, blur 기반 공용 UI는 건드리지 않는다.
4. Restore 전용 경로는 `ExecuteEventChoice`, `ContinueAfterExecutedChoice`, reward resolve, dice random, shop stock random을 호출하지 않는다.
5. recipe 적용이 끝난 뒤에만 autosave suppression을 해제하며, restore 도중 checkpoint를 다시 쓰지 않는다.
