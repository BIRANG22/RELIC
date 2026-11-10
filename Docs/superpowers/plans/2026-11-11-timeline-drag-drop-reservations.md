# Timeline Drag-and-Drop Reservations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add skill-to-timeline drag reservation and atomic reordering/moving of existing player reservations, including relative movement-path preservation and insertion between player actions.

**Architecture:** A shared drag context carries either character/skill data or an exact reserved-command identity. `BattleTimelineController` owns dry-run validation and atomic insertion/move APIs; UI components only translate pointer positions into player insertion indices. New movement skills store a pending insertion context while the existing grid path selector collects the route. Existing timeline simulation remains authoritative and recomputes absolute positions/ranges from preserved relative paths.

**Tech Stack:** Unity 6, C#, Unity EventSystems, NUnit EditMode tests, existing Steam battle command/snapshot layer.

**Spec:** `Docs/superpowers/specs/2026-11-11-timeline-drag-drop-reservations-design.md`

## Global Constraints

- Click-based reservation and cancellation behavior must remain available.
- Drag/drop must reuse the same slot, character, capacity, cost, resource, equipment, turn-lock, and authority rules as click reservation.
- Failed drops must not mutate source commands, costs, history, paths, selection, or previews.
- Existing moves preserve ordered relative steps; absolute destination and visual path are recomputed after reordering.
- Execution/display order remains Swift player actions, monster actions, then non-Swift player actions.
- Monster reservations are never draggable.

## Review Focus

- Same-slot forward/backward reorder must adjust the destination index after removing the source without off-by-one errors; Task 2 tests both directions.
- A stale drag payload must not move the command now occupying the old index; Task 2 tests exact command identity and a repeated request.
- Moving between slots must evaluate destination capacity as a proposed layout and leave source untouched on failure; Task 3 tests both outcomes.
- A pending new-move drop canceled during grid selection must clear only pending drag state; Task 4 tests cancellation.
- Dropping over the monster visual region must resolve to a legal boundary in the command's Swift/non-Swift player phase; Task 5 tests mapping.

---

### Task 1: Drag Payload and Proposed Layout Model

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/TimelineReservationDragContext.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/TimelineReservationLayoutUtility.cs`
- Test: `Assets/Tests/EditMode~/TimelineReservationLayoutUtilityTests.cs`

**Interfaces:**
- Produces: `TimelineReservationDragContext.ForNewSkill(CharacterRuntimeData, SkillMasterData)` and `.ForExisting(int, PlayerReservedCommand)`.
- Produces: `TimelineReservationLayoutUtility.BuildProposedPlayerCommands(...)` returning source/destination command lists without mutating live slots.

- [ ] **Step 1: Write failing tests** for new-skill/existing-command payload identity, cross-slot insertion, and same-slot reorder in both directions.
- [ ] **Step 2: Compile the EditMode assembly and verify missing-type failures.**
- [ ] **Step 3: Implement the immutable drag context and pure proposed-layout utility.**
- [ ] **Step 4: Compile and run the focused EditMode tests; expect all Task 1 tests to pass.**
- [ ] **Step 5: Commit the model and tests.**

### Task 2: Atomic Existing-Reservation Reorder API

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerReservedCommand.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineDragReservationTests.cs`

**Interfaces:**
- Consumes: `TimelineReservationLayoutUtility.BuildProposedPlayerCommands(...)`.
- Produces: `bool TryMovePlayerReservation(PlayerReservedCommand command, int destinationSlotIndex, int destinationPlayerIndex, bool showWarning = true)`.
- Produces: `bool CanApplyPlayerReservationLayout(...)` as the shared dry-run validation entry point.

- [ ] **Step 1: Write failing tests** proving exact-identity moves, same-slot reorder both directions, cross-slot move, stale repeated request no-op, and preservation of relative move offsets/direction.
- [ ] **Step 2: Run/compile tests and verify they fail because the APIs do not exist.**
- [ ] **Step 3: Extract reservation validation so a proposed layout can be evaluated without mutating live lists or runtime reserved-cost totals.**
- [ ] **Step 4: Implement atomic move/reorder, update reservation history, then recalculate costs and replay simulation once after success.**
- [ ] **Step 5: Run focused tests and existing manual-control regression tests; expect pass.**
- [ ] **Step 6: Commit controller changes and tests.**

### Task 3: Destination Validation and Failed-Drop Rollback

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/ReserveTurnSlotUI.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineDragReservationTests.cs`

**Interfaces:**
- Produces: `TimelineReservationDropValidation ValidateReservationDrop(TimelineReservationDragContext, int destinationSlotIndex, int destinationPlayerIndex)`.
- Produces: validation result fields `IsValid`, `WarningMessage`, and normalized insertion index.

- [ ] **Step 1: Add failing tests** for full destination, character mismatch, locked slot, insufficient cost/resource, dead character, and a valid source-slot-removal capacity calculation.
- [ ] **Step 2: Verify the focused tests fail.**
- [ ] **Step 3: Implement validation by delegating to existing acceptance/cost rules against the proposed layout.**
- [ ] **Step 4: Ensure every failure returns without changing either slot, runtime reserved values, or history.**
- [ ] **Step 5: Run focused and existing reservation tests; expect pass.**
- [ ] **Step 6: Commit validation changes and tests.**

### Task 4: New Skill Drop and Deferred Movement Path Selection

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Skill/SkillListSlotUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Skill/SkillListPanel.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerSkillReservationController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineDragReservationTests.cs`

**Interfaces:**
- Produces: `BeginDraggedSkillReservation(CharacterRuntimeData, SkillMasterData, int slotIndex, int playerIndex)`.
- Produces: pending insertion context methods on `PlayerSkillReservationController`: `SetPendingTimelineInsertion(int, int)`, `ClearPendingTimelineInsertion()`, and consumption during final confirmation.

- [ ] **Step 1: Add failing tests** for direction-skill insertion, new-move deferred confirmation, final insertion at the remembered index, and canceled grid selection leaving the timeline unchanged.
- [ ] **Step 2: Verify focused tests fail.**
- [ ] **Step 3: Add EventSystems drag source behavior to skill-list slots while retaining existing click behavior.**
- [ ] **Step 4: Route direction skills through the existing selection/confirmation flow with explicit slot and insertion index.**
- [ ] **Step 5: Store pending insertion for movement skills, invoke existing grid path selection, and consume it only after final validation.**
- [ ] **Step 6: Run focused tests and compile both runtime/editor assemblies; expect pass.**
- [ ] **Step 7: Commit skill drag and deferred movement flow.**

### Task 5: Timeline Drop Targets, Between-Action Mapping, and Feedback

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/TimelineReservationDropTarget.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/TimelineReservationDragVisual.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineGroupUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineBarUI.cs`
- Modify: `Assets/Project/PrefabsR/TimelineSlot.prefab` only if runtime-created targets cannot match the established layout.
- Test: `Assets/Tests/EditMode~/TimelineReservationDropTargetTests.cs`

**Interfaces:**
- Consumes: controller validation/move/new-skill APIs from Tasks 2–4.
- Produces: pointer-to-player-insertion mapping and legal/invalid highlight state.

- [ ] **Step 1: Write failing tests** for before/between/after mapping, nearest-boundary mapping over monster orders, and Swift/non-Swift phase constraints.
- [ ] **Step 2: Verify focused tests fail.**
- [ ] **Step 3: Implement runtime drop targets and a non-raycast-blocking drag ghost using Unity EventSystems.**
- [ ] **Step 4: Bind existing player entries as drag sources by exact command identity; keep monster entries non-draggable.**
- [ ] **Step 5: Add legal/invalid insertion highlights and pending-move insertion highlighting.**
- [ ] **Step 6: Run focused tests and perform a prefab/scene serialization diff check.**
- [ ] **Step 7: Commit UI drag/drop behavior.**

### Task 6: Network Authority and End-to-End Verification

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/BattleNetworkModels.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/BattleNetworkSerialization.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/SteamBattleStateSynchronizer.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineDragNetworkTests.cs`

**Interfaces:**
- Produces: network command for stable reservation identity, source slot, destination slot, and destination player insertion index.
- Consumes: authoritative validation and atomic move APIs from Tasks 2–3.

- [ ] **Step 1: Add failing serialization/authority tests** for valid reorder, rejected invalid destination, stale command identity, and host/client identical final order.
- [ ] **Step 2: Verify focused tests fail.**
- [ ] **Step 3: Extend network DTOs and serialization with backward-safe defaults for drag/reorder fields.**
- [ ] **Step 4: Have the host resolve stable identity, rerun validation, and atomically apply or reject the move.**
- [ ] **Step 5: Run all focused tests, existing EditMode suite, runtime/editor compilation, and `git diff --check`.**
- [ ] **Step 6: Manually verify the wall example: right-edge `right 3` moved before `left 3` simulates zero movement, then moving it back restores the preserved `right 3` relative route.**
- [ ] **Step 7: Commit network support and verification updates.**
