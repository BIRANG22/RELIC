# Event Choice Result And Reward Delay Plan

## Problem

When an event choice is selected, the result text can include the choice label
or description itself. Some choices also open the reward panel immediately, so
the reward UI can appear before the player has a moment to read the event
result.

## Investigation

- `EventChoiceExecutionService.Execute` builds `ResultMessage` from a message
  list and currently adds `choice.ChoiceDesc` before applying costs, dice/chance
  rolls, failures, and success results.
- `EventRoomController.ExecuteEventChoice` writes `result.ResultMessage` to
  `eventResultText`, then immediately calls `TryOpenPendingEventRewardPanel`
  when pending event rewards should be shown.
- `EventRoomController.ResolveChoice` is an older local result resolver that
  also adds `choice.ChoiceDesc`. Even though the current choice execution path
  uses `EventChoiceExecutionService`, keeping the duplicate behavior there
  leaves a regression path.
- Event rewards are already queued separately from text output, and
  `EventRoomController.CreateExecutionContext` uses
  `SuppressRewardResultMessages = true`. This means reward acquisition text can
  stay in the reward panel while result text only shows actual resolution
  messages.

## Design

1. Keep choice label/description out of event choice result message
   construction in `EventChoiceExecutionService`.
2. Remove the duplicate `ChoiceDesc` append from
   `EventRoomController.ResolveChoice`.
3. Add a serialized `eventRewardPanelOpenDelay` on `EventRoomController` with a
   default of `0.6` seconds.
4. When `ExecuteEventChoice` resolves a terminal choice with pending rewards,
   hide the next button, lock choices, wait the configured unscaled delay, then
   open `BattleRewardPanelUI`.
5. Keep the existing immediate path available for non-choice flows that call the
   reward panel from the next button.
6. Add EditMode tests covering choice text filtering and delayed reward open
   state.

## Scope

- Update `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventChoiceExecutionService.cs`.
- Update `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs`.
- Update existing EventRoom EditMode tests.
