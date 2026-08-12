# Manual Map And Event Room Runtime Fix Design

## Goal

Fix the battle map so a connected manual map template replaces stale procedural runtime nodes, and make EventRoom render Excel-driven event choices through pre-placed scene slots.

## Findings

- `BattleMapPanel` returns early when `MapRuntimeData.IsRunInitialized` is true and `GeneratedNodes` already has entries. That means an older procedural run can remain even after a manual template is connected.
- `MapRuntimeData` only records whether a manual template was used, not which template produced the current nodes.
- `EventRoomController` currently has a fallback path that creates data event UI and choice buttons at runtime. This conflicts with the desired scene-authored slot approach.
- Some event results need a separate target picker, such as choosing a relic or choosing a memory. Until that UI exists, those choices should be shown as unavailable instead of auto-consuming an arbitrary target.

## Design

- Store a manual map template key in `MapRuntimeData`.
- Generate a stable runtime key from the assigned `ManualBattleMapTemplate`.
- Regenerate the map when the current runtime nodes were not produced by the assigned manual template.
- Keep room internals randomized by resolving map data from the template node type through the existing map pool.
- Add a fixed `EventChoiceSlotUI` component for scene-authored choice buttons.
- Change `EventRoomController` to bind choices into existing slots and hide unused slots.
- Move data-event execution into a focused service so UI only displays state and requests execution.
- Use `BattleRandom` for event dice, chance, and reward selection.

## Target Selection Boundary

Choices that require a target selection UI are intentionally disabled for now:

- Losing a selected relic before gaining a new relic.
- Awakening a selected memory.
- Offering two memories and selecting one.

These will become selectable once a dedicated selection panel is added.

## Multiplayer Impact

Map generation and event result application remain store-based and ID-based. UI does not become the source of combat calculation.
