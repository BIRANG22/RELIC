# SDD ledger — plan: Docs/superpowers/plans/2026-11-11-time-aware-player-move-path.md

Pre-flight: Task 1 produces `TimedMoveTransition` and `TimeAwareMovePathUtility.FindShortestPath`; Task 2 consumes the same types and return value. Interface matches the plan and spec.

Ruling: Continue in the shared dirty Unity workspace instead of creating a worktree — the requested feature depends on the user's existing uncommitted reservation changes — cost if wrong: this task cannot provide an isolated commit range and verification must distinguish unrelated changes.

