# Manual Map And Event Room Runtime Fix Plan

## Scope

- Fix stale procedural map reuse when a manual template is assigned.
- Add fixed event choice slots and bind Excel Event rows into them.
- Apply supported Event row result data through a non-UI execution service.
- Update battle scenes so the fixed template and event slots are serialized.

## Implementation Steps

- Add tests for manual template runtime key mismatch regeneration.
- Add tests for event choice availability and supported result execution.
- Add `ManualBattleMapTemplate.GetRuntimeKey`.
- Add `ManualMapTemplateKey` to `MapRuntimeData`.
- Update `BattleMapPanel` to regenerate when the assigned manual template differs from the stored key.
- Add `EventChoiceSlotUI`.
- Add `EventChoiceExecutionService` and related result/context structs.
- Refactor `EventRoomController` to use fixed slots and remove runtime button creation.
- Update `Battle`, `DebugBattle`, and `Battletest` scene serialized references where applicable.
- Run project compile and diff checks. Unity batchmode tests are not run by rule.
