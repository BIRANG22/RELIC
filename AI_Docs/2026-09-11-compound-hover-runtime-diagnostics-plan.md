# Compound Hover Runtime Diagnostics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make A/B/C Compound hover runtime logs prove the source ID and localized name reaching the TMP writer.

**Architecture:** Carry a stable display-source ID from slot binding to the hover component, then emit boundary logs at initialization, pointer entry, and tooltip assignment. The logs use the existing `LocalizationHoverDiagnostics` formatter and do not alter runtime localization ownership.

**Tech Stack:** Unity 6, C#, TextMeshPro, NUnit Edit Mode tests.

**Spec:** `AI_Docs/2026-09-11-compound-hover-runtime-diagnostics-design.md`

## Global Constraints

- Preserve existing uncommitted changes.
- Do not commit, push, create a PR, or alter unrelated localization paths.
- Keep tests under `Assets/Tests/EditMode~/`.
- Do not use Unity batchmode; compile with the established Assembly-CSharp project command.

---

### Task 1: Carry diagnostic source identity through Compound hover

**Files:**
- Modify: `Assets/Project/Scripts/CompoundRecipeSlotUI.cs`
- Modify: `Assets/Project/Scripts/CompoundReferenceNameTooltip.cs`
- Modify: `Assets/Project/Scripts/Core/Localization/RuntimeTMPTextAutoLocalizer.cs`

**Interfaces:**
- Consumes: `CompoundRecipeSlotUI.Bind(DataManager, CompoundData, CompoundReferenceNameTooltip)` and `GameDataLocalization.CompoundName(CompoundData)`.
- Produces: `CompoundReferenceIconHover.Initialize(CompoundReferenceNameTooltip, RectTransform, string sourceId, string displayName)` and diagnostics that include SourceId and IncomingText before/after `Show` assignment.

- [ ] **Step 1: Add an Edit Mode failing test for the intended Initialize signature and retained source ID.**
- [ ] **Step 2: Run the focused Edit Mode test and confirm it fails because the source-ID API does not exist.**
- [ ] **Step 3: Add the minimum identity forwarding and boundary-only diagnostic logging.**
- [ ] **Step 4: Run the focused test and confirm it passes.**
- [ ] **Step 5: Compile `Assembly-CSharp.csproj` without Unity batchmode.**

### Task 2: Perform Play Mode evidence collection

**Files:**
- No source changes.

- [ ] **Step 1: In Korean locale, reproduce Compound A → B → C and capture the shared tooltip TMP logs.**
- [ ] **Step 2: Determine whether each source ID and localized name reaches `Show`, and whether a later writer changes the same TMP.**
- [ ] **Step 3: Repeat in English after any root-cause fix, then remove temporary diagnostics only after the issue is verified.**
