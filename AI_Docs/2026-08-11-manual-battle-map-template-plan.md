# Manual Battle Map Template Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow designers to define battle map node types, positions, and connections from the Inspector while preserving current random room content selection when a node has no fixed map id.

**Architecture:** Add a `ScriptableObject` template that stores serializable node definitions. A builder converts the template into existing `GeneratedMapNodeData`, so downstream map UI and room opening continue using the same runtime contract. `BattleMapPanel` chooses the manual template when it is assigned and valid, otherwise it keeps the current `ProceduralMapGenerator` fallback.

**Tech Stack:** Unity C#, NUnit EditMode tests, existing `MapData`, `GeneratedMapNodeData`, `BattleRandom`, and `BattleMapLayoutUtility`.

## Global Constraints

- Documents are written only under `AI_Docs`.
- Tests are written only under `Assets/Tests/EditMode~/` or `Assets/Tests/PlayMode~/`.
- Do not run Unity batchmode tests; the editor is assumed to be open.
- Do not change battle result logic from UI/VFX/scene code.
- Random selection that can affect gameplay uses `BattleRandom`.
- No commit, push, PR, branch, or worktree operation without explicit approval.

---

### Task 1: Manual Template Runtime Builder

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Data/Map/ManualBattleMapTemplate.cs`
- Test: `Assets/Tests/EditMode~/ManualBattleMapTemplateTests.cs`

**Interfaces:**
- Produces: `ManualBattleMapTemplate.TryBuildNodes(List<MapData> mapPool, string chapter, string stage, out List<GeneratedMapNodeData> nodes)`
- Produces: `ManualBattleMapNodeDefinition` with node index, layer index, room type, optional map id override, optional custom position, and next node indices.

- [ ] Write failing tests that verify a manual template preserves declared node type, connection order, and calculated positions.
- [ ] Write failing tests that verify an empty map id override randomly selects a matching `MapData` by type/chapter/stage.
- [ ] Implement `ManualBattleMapTemplate` and validation.
- [ ] Run targeted compile/test verification.

### Task 2: BattleMapPanel Integration

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleMapPanel.cs`
- Test: `Assets/Tests/EditMode~/ManualBattleMapTemplateTests.cs`

**Interfaces:**
- Consumes: `ManualBattleMapTemplate.TryBuildNodes(...)`
- Produces: manual-template-first generation path in `BattleMapPanel.EnsureMapGenerated()`.

- [ ] Add a serialized `ManualBattleMapTemplate` field under a map generation header.
- [ ] Route new runs through the template before `ProceduralMapGenerator`.
- [ ] Keep existing runtime reuse behavior when `IsRunInitialized` already has nodes.
- [ ] Fall back to procedural generation when no template is assigned or the template cannot build valid nodes.

### Task 3: Verification

**Files:**
- Verify compile affected C# files.
- Verify no unintended scene or prefab churn.

- [ ] Run available non-batch compile checks.
- [ ] Run targeted EditMode tests from the open Unity editor if available, otherwise report that Unity execution was not performed.
- [ ] Check git diff and summarize only touched files.
