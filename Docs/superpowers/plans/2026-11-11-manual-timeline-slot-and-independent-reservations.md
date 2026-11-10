# Manual Timeline Slot and Independent Reservations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop automatic timeline-slot movement and preserve later reservations when an earlier action changes.

**Architecture:** `BattleTimelineController` retains the player's active slot and mutates only the explicitly targeted reservation. Existing simulation refreshes recalculate retained commands from the current sequence.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `Docs/superpowers/specs/2026-11-11-manual-timeline-slot-and-independent-reservations-design.md`

## Global Constraints

- Initial battle default-slot selection remains available.
- Explicit slot selection and locked-slot safety remain unchanged.
- No reservation other than the explicitly removed command is deleted.

## Review Focus

- Character switch while the active slot belongs to another character.
- Capacity reached in a non-final slot.
- Removing a move before another move in the same slot.
- Adding a move before an already reserved later-slot attack.
- Preview direction after removing the only left-facing move.

---

### Task 1: Manual slot selection

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

- [ ] Add tests proving character changes and full slots preserve `ActiveSlotIndex`.
- [ ] Run tests and confirm existing auto-selection behavior fails them.
- [ ] Remove character/full/tutorial auto-selection entry points.
- [ ] Run tests and confirm they pass.

### Task 2: Independent reservations

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

- [ ] Add tests proving later moves and attacks remain and direction returns to runtime direction.
- [ ] Run tests and confirm existing cascade cancellation fails them.
- [ ] Remove follow-up move/attack cancellation calls and dead helpers.
- [ ] Run focused tests and compile runtime/editor assemblies.

