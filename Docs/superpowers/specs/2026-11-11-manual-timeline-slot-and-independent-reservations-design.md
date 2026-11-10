# Manual Timeline Slot and Independent Reservations Design

## Intent

Timeline slot selection remains under direct player control. Changing characters or filling a slot must not move the selection. Removing or inserting one reservation must not delete later reservations for the same character.

## Behavior

- Keep the active slot when the selected character changes, even if that character cannot reserve there.
- Keep the active slot after it reaches capacity; the next reservation attempt may show the existing capacity warning.
- Do not auto-select a slot when tutorial input becomes ready.
- Do not delete later move or attack reservations when a move is added, merged, undone, or removed.
- Re-run reservation simulation after removal so retained actions use the remaining sequence. A right-facing character whose left move is removed attacks from the original position facing right.

## Approach

Remove automatic slot-selection calls and follow-up reservation cancellation calls at their controller entry points. Retain the existing `RefreshReservationSimulation()` pipeline as the single source for recalculating position, direction, and range.

## Non-goals

- Initial default-slot selection when battle input first becomes available remains unchanged.
- Explicit player slot clicks and locked-slot safety behavior remain unchanged.

