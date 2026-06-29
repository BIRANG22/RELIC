# Execution Range Grid Design

## Goal

During battle execution, show only the currently executing action's affected grid cells by turning the base grid renderers on and tinting them red. When that action ends, restore the grid to the normal hidden execution state.

## Current Behavior

`BattleTurnExecutor.RefreshBattlePresentationState()` calls `GridManager.SetGridVisible(false)` while turns are executing, so all base grid renderers and colliders are disabled. Existing reservation and hover previews use `RangePreview`, which turns on each `GridCell`'s separate `Highlight` object. The execution-time display should not reuse that highlight object.

## Design

`GridCell` will expose a temporary base-renderer tint API. It captures the cell's normal renderer enabled state and material property block before applying a red execution tint. Clearing the tint restores the captured state.

`GridManager` will own execution range display state. It will offer `ShowExecutionRange(IReadOnlyCollection<int>, Color)` and `ClearExecutionRange()`. Showing a range first clears the prior execution range, then applies the temporary tint only to valid cells in the supplied grid indices.

`BattleActionRunner` will call the grid manager immediately before running a player move, player skill, monster move, or monster skill. Player and monster skills use their recalculated `RangeGridIndices`. Player moves use the reserved move range if present, falling back to the destination cell. Monster moves show the occupied cells after applying the effective move offset. Each action clears the execution range before leaving the action routine.

Reservation and hover range previews remain unchanged.

## Testing

Add EditMode coverage that proves `GridManager.ShowExecutionRange` enables and tints only the requested base grid cell while leaving non-range cells hidden. Add runner-level tests for the helper that resolves execution range indices for player moves and monster moves, so the action runner can show the correct range without depending on animation timing.
