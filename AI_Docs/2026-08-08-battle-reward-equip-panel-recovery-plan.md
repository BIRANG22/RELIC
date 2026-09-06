# Battle Reward Equip Panel Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make character cards reliably select a valid reward destination and make reward deletion reusable with the required skill/relic outcomes.

**Architecture:** Keep presentation in `BattleRewardEquipPanelUI`, but isolate deterministic skill-slot selection in a small policy so edge cases can be tested without scene state. Revalidate selections against current runtime data immediately before applying them.

**Tech Stack:** Unity 6, C#, uGUI, NUnit EditMode tests

## Global Constraints

- Documents remain under `AI_Docs`.
- Tests remain under `Assets/Tests/EditMode~/`.
- Do not run Unity in batchmode because the editor is assumed open.
- Do not create branches, commits, pushes, PRs, or worktrees.
- Preserve the ID-based multiplayer boundary and do not add networking dependencies.

---

### Task 1: Deterministic Skill Destination Policy

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardEquipSelectionPolicy.cs`
- Create: `Assets/Tests/EditMode~/BattleRewardEquipSelectionPolicyTests.cs`

**Interfaces:**
- Produces: `TryFindSkillViewIndex(CharacterRuntimeData character, Func<string, SkillMasterData> resolveSkill, out int skillViewIndex)`.

- [ ] Write failing tests for empty-slot priority, replaceable-slot fallback, and no-valid-slot failure.
- [ ] Compile the test project and confirm failure because the policy does not exist.
- [ ] Implement the minimum deterministic policy over runtime slots 2 and 3.
- [ ] Compile again and confirm the policy and tests compile.

### Task 2: Recoverable Card Selection and Delete Reuse

**Files:**
- Modify: `Assets/Project/Scripts/BattleRewardEquipPanelUI.cs`
- Modify: `Assets/Tests/EditMode~/BattleRewardEquipPanelUIRegressionTests.cs`

**Interfaces:**
- Consumes: `BattleRewardEquipSelectionPolicy.TryFindSkillViewIndex(...)`.
- Produces: character-card-wide selection, current-state validation, and reusable Confirm/Delete interaction.

- [ ] Write failing prefab/UI regression tests for card buttons, supported skill slot interaction, and reopened delete state.
- [ ] Compile the tests and confirm the expected failures.
- [ ] Make card selection choose a relic or skill destination by current reward type.
- [ ] Revalidate the chosen destination immediately before applying the reward.
- [ ] Reset Delete interaction in `Open` and replace broad listener removal with owned-listener removal.
- [ ] Preserve skill no-reward deletion and relic 30% remnant extraction.
- [ ] Compile runtime and editor test projects.

### Task 3: Final Verification

**Files:**
- Verify all files above.

- [ ] Inspect the final diff for unrelated changes.
- [ ] Build `Assembly-CSharp.csproj` without package restore.
- [ ] Build `Assembly-CSharp-Editor.csproj` without package restore.
- [ ] Report any Unity Editor-only behavior that could not be executed without batchmode.
