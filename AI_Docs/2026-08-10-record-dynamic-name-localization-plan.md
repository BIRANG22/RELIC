# Record Dynamic Name Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `Record.prefab`의 선택 항목 이름 표시가 정적 `common.name` 대신 스킬, 룬, 유물, 완성 아이템의 Locale 표시명을 사용하게 한다.

**Architecture:** `RecordDisplayNameResolver`가 데이터 타입별 표시명 조회를 담당하고 `RecordPanelUI`는 resolver 결과만 슬롯에 전달한다. `Record/Info/Name`은 코드가 쓰는 동적 출력 영역이므로 `LocalizeStringEvent`를 제거한다.

**Tech Stack:** Unity 6, Unity Localization, TextMesh Pro, C# EditMode tests, prefab YAML

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR, 브랜치와 worktree 작업은 수행하지 않는다.
- 전투 결과와 멀티플레이 동기화 데이터는 변경하지 않는다.

---

### Task 1: Record 이름 표시 resolver

**Files:**
- Modify: `Assets/Project/Scripts/RecordPanelUI.cs`
- Test: `Assets/Tests/EditMode~/RecordDisplayNameResolverTests.cs`

**Interfaces:**
- Produces: `RecordDisplayNameResolver.ResolveDisplayName<T>(T data, string fallbackId, string sourceName, Func<T, string> localizer)`
- Produces: `RecordDisplayNameResolver.SkillName(SkillMasterData data)`
- Produces: `RecordDisplayNameResolver.RuneName(RuneData data)`
- Produces: `RecordDisplayNameResolver.RelicName(RelicData data)`
- Produces: `RecordDisplayNameResolver.ItemName(ItemData data)`

- [x] Write failing tests that localized values win over source names and stable IDs are used when names are blank.
- [x] Run the targeted tests and confirm they fail because `RecordDisplayNameResolver` does not exist.
- [x] Add the resolver with minimal fallback logic.
- [x] Run the targeted tests and confirm they pass.

### Task 2: RecordPanelUI 연결

**Files:**
- Modify: `Assets/Project/Scripts/RecordPanelUI.cs`
- Test: `Assets/Tests/EditMode~/RecordDisplayNameResolverTests.cs`

**Interfaces:**
- Consumes: `RecordDisplayNameResolver.SkillName`
- Consumes: `RecordDisplayNameResolver.RuneName`
- Consumes: `RecordDisplayNameResolver.RelicName`
- Consumes: `RecordDisplayNameResolver.ItemName`

- [x] Replace direct `Name` values in slot creation and sorting with resolver values.
- [x] Preserve existing icon lookup and tab behavior.
- [x] Run resolver tests and compile check.

### Task 3: Record prefab dynamic name cleanup

**Files:**
- Modify: `Assets/Project/PrefabsR/Record.prefab`
- Test: `Assets/Tests/EditMode~/RecordPrefabLocalizationTests.cs`

**Interfaces:**
- Verifies: `RecordPanelUI.nameText` target has no `LocalizeStringEvent`.

- [x] Write a failing prefab test that loads `Record.prefab`, resolves `RecordPanelUI.nameText`, and asserts no `LocalizeStringEvent` is attached to that TMP object.
- [x] Run the targeted test and confirm it fails while the prefab still has `Text/common.name`.
- [x] Remove only the `LocalizeStringEvent` component from `Record/Info/Name`.
- [x] Run the targeted test and confirm it passes.

### Task 4: Final verification

**Files:**
- Check: changed source, prefab, tests, docs

- [x] Run MSBuild compile.
- [x] Review `git diff`.
- [x] Report changed files, implementation, verification, unverified items, multiplayer impact, and commit/Push/PR status.
