# Map FixedPosition Removal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 고정맵 템플릿 기준 맵 생성에서 `FixedPosition` 런타임 의존을 제거한다.

**Architecture:** 맵 데이터 선택은 `Chapter`, `Stage`, `Type`만 사용한다. 수동 템플릿과 절차형 fallback 모두 동일한 후보 선택 기준을 공유하되, Start는 가상 맵 fallback을 유지한다.

**Tech Stack:** Unity C#, NUnit EditMode tests, MSBuild verification.

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR, 브랜치 변경은 사용자 승인 없이는 하지 않는다.
- 전투 결과에 영향을 주는 랜덤은 기존 `BattleRandom` 흐름을 유지한다.

---

### Task 1: Tests Describe Type-Only Map Selection

**Files:**
- Modify: `Assets/Tests/EditMode~/ManualBattleMapTemplateTests.cs`

**Interfaces:**
- Consumes: `ManualBattleMapTemplate.TryBuildNodes(List<MapData>, string, string, out List<GeneratedMapNodeData>)`
- Produces: 테스트가 기대하는 계약: 빈 `MapIdOverride`는 `Type` 기준 후보를 선택하고 Start는 맵 데이터 없이도 가상 Start로 fallback한다.

- [x] **Step 1: Write failing tests**

Add tests that build a template with blank Start, Common, and Boss nodes. Use a map pool where `MapData` has no `FixedPosition` field assignments. Assert Start resolves to `"Start"` when no Start row exists, Common resolves to `"battle_a"`, and Boss resolves to `"boss_a"` by type.

- [x] **Step 2: Verify RED**

Run the focused EditMode test in the Unity Test Runner when available. Expected before production changes: `TryBuildNodes_BlankStartMapIdSelectsStartMapByType` fails because the existing resolver falls back to the virtual `"Start"` map when the matching Start row does not have `FixedPosition.Front`. In this workspace, do not run Unity batchmode tests because the project rule forbids it.

### Task 2: Remove FixedPosition From Runtime Map Data

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Map/MapData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/MapDatabase.cs`

**Interfaces:**
- Consumes: `MapData`
- Produces: `MapData` with `MapId`, `Name`, `Type`, `BattleMapId`, `EventId`, `Chapter`, `Stage`, `SpawnWeight` only.

- [x] **Step 1: Remove the enum and field**

Delete `FixedPosition` enum and `MapData.FixedPosition`.

- [x] **Step 2: Remove obsolete database helpers**

Delete `GetStartMap`, `GetFinalMap`, `GetPenultimateMap`, and their private comparer if no longer used.

### Task 3: Use Type-Only Selection In Generators

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Map/ManualBattleMapTemplate.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/ProceduralMapGenerator.cs`

**Interfaces:**
- Consumes: `MapData.Type`, `MapData.Chapter`, `MapData.Stage`, `MapData.SpawnWeight`
- Produces: generated nodes whose `MapId` and `Type` are resolved without `FixedPosition`.

- [x] **Step 1: Simplify manual template resolver**

Change `TryResolveMap` so blank overrides call a `TryPickCandidate` that filters only `Chapter`, `Stage`, and `Type`. Keep virtual Start fallback.

- [x] **Step 2: Simplify procedural fallback**

Change layer 0 to pick `Type == "Start"` first, then virtual Start. Change final layer to pick `Type == "Boss"`. Remove fixed-position candidate filters.

- [x] **Step 3: Keep normal fallback playable**

When a requested non-fixed type has no candidates, pick another same chapter/stage map that is not `Start` or `Boss`.

### Task 4: Verification

**Files:**
- Read: `git diff --check`
- Build: `RELIC.sln`

**Interfaces:**
- Produces: clean compile and whitespace check result.

- [x] **Step 1: Build**

Run MSBuild for `RELIC.sln` in Debug configuration.

- [x] **Step 2: Check diff**

Run `git diff --check`.

- [x] **Step 3: Report**

Report changed files, implementation summary, verification, unverified items, multiplayer impact, and commit/Push/PR status.
