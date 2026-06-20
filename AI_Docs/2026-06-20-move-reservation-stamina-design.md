# Move Reservation Stamina Design

## Requested Behavior

- One move reservation occupies one timeline command slot.
- The reserved stamina cost is based on the planned movement distance.
- `S_Move_1` moves one cardinal grid per one stamina.
- `S_Move_2` moves two cardinal grids per one stamina.
- Runtime move value `>= 50` upgrades the character move skill to `S_Move_2`.
- Move skills use the full grid range ID (`Range_All`). The typo form `Rnage_All` is accepted defensively.
- If execution is blocked and the character moves fewer paid stamina chunks than planned, refund half of the blocked stamina chunks at turn end, rounded down.

## Cost Rule

- Movement is still cardinal-path based.
- Cost is calculated as:
  - `ceil(abs(x) / gridsPerStamina) + ceil(abs(y) / gridsPerStamina)`
- Example:
  - `S_Move_2`, planned 7 straight grids: `ceil(7 / 2) = 4`.
  - Actual 4 grids: `ceil(4 / 2) = 2`.
  - Blocked stamina chunks: `4 - 2 = 2`.
  - Turn-end refund: `floor(2 / 2) = 1`.

## Implementation Notes

- Store planned move distance, grids per stamina, and executed move distance on `PlayerReservedCommand`.
- Movement command base costs must be updated to the distance-based value so reservation cost modifiers can safely reset and reapply.
- Movement execution consumes the command's stamina cost and records actual movement distance.
- Timeline turn cleanup applies blocked movement refunds once before reservations are cleared.
