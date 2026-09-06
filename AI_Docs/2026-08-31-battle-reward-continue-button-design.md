# Battle Reward Continue Button Design

## Context

Battle and event reward panels previously completed the room flow as soon as every reward was claimed. This made the game return to map selection immediately after reward collection.

## Decision

- Keep `BattleRewardPanelUI` as a generic reward collection panel.
- Let room controllers decide what happens after reward collection.
- Event room rewards now close the reward panel, clear pending reward state, and show the shared `NextButton`.
- Battle room rewards now close the reward panel and show the same shared `NextButton`.
- In the main Battle scene, the shared continue button is parented under `BattleRewardCanvas` so it remains in an active Canvas while avoiding inactive room roots.
- The old event-room-only `NextButton` object was removed, and event room, rest room, and battle room controllers reference the shared button.
- The battle room node clear, room cleanup, and map or boss result transition happen only when the player presses the continue button.
- If the battle room continue button is missing, the flow logs a warning and falls back to immediate completion to avoid a soft lock.

## Multiplayer Boundary

Reward collection remains separate from map progression. The continue button only triggers the existing node completion and transition flow; it does not calculate rewards or battle results.
