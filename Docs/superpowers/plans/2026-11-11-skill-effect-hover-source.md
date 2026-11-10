# Skill Effect Hover Source Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make reward-card effect explanations match the effects linked in the battle skill description.

**Architecture:** `SkillDescriptionFormatter` owns extraction of `effecticon:` links. Battle and reward panels consume that shared output instead of independently deriving display effects.

**Tech Stack:** Unity, C#, NUnit, TextMeshPro rich-text links

**Spec:** `docs/superpowers/specs/2026-11-11-skill-effect-hover-source-design.md`

## Global Constraints

- Preserve display order and deduplicate IDs case-insensitively.
- Display authored description effects, not internal execution effects.

## Review Focus

- Duplicate links retain their first occurrence.
- Empty descriptions return no effects.
- Internal-only execution effects do not leak into the UI.
- Localized descriptions remain the source for reward cards.
- Battle tooltip positioning behavior remains unchanged.

---

### Task 1: Shared linked-effect extraction

**Files:**
- Modify: `Assets/Project/Scripts/SkillDescriptionFormatter.cs`
- Modify: `Assets/Tests/EditMode~/BattleRewardSkillEffectHoverTests.cs`

**Interfaces:**
- Produces: `SkillDescriptionFormatter.ExtractLinkedEffectIds(string): List<string>`

- [ ] Add failing tests for ordered deduplication and `독성발현` resolving to `E_Poison`.
- [ ] Run the focused EditMode tests and confirm the missing API/old behavior fails.
- [ ] Implement the minimal link parser.
- [ ] Run the focused tests and confirm they pass.

### Task 2: Unify consumers

**Files:**
- Modify: `Assets/Project/Scripts/BattleRewardSkillPanelUI.cs`
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`

**Interfaces:**
- Consumes: `SkillDescriptionFormatter.ExtractLinkedEffectIds(string)`

- [ ] Build reward preview effects from the formatted localized description.
- [ ] Replace battle TMP mesh link enumeration with the shared parser.
- [ ] Run focused tests, compile runtime/editor assemblies, and run relevant EditMode tests.

