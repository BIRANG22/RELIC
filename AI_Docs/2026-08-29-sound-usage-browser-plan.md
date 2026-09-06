# Sound Usage Browser Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** SoundDatabase와 사운드 참조 위치를 역추적하고 SoundDatabase clip을 에디터에서 교체할 수 있는 관리 도구를 만든다.

**Architecture:** `SoundUsageScanner`가 DB 항목, `[SoundId]` 직렬화 참조, 스킬 VFX 참조, 프리팹 내장 AudioSource를 수집한다. `SoundUsageBrowserWindow`는 스캐너 결과를 보여주고 SoundDatabase 항목을 `SerializedObject`로 편집한다. Markdown 리포트는 같은 스캔 결과에서 생성한다.

**Tech Stack:** Unity Editor, ScriptableObject, SerializedObject, AssetDatabase, NUnit EditMode tests.

**Spec:** `AI_Docs/2026-08-29-sound-usage-browser-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 내부에만 작성한다.
- Unity batchmode 테스트는 시도하지 않는다.
- 런타임 전투 결과 로직은 변경하지 않는다.
- 커밋, Push, PR, 브랜치 생성은 하지 않는다.

---

### Task 1: Scanner Result Model And Database Scan

**Files:**
- Create: `Assets/Project/Editor/SoundUsageScanner.cs`
- Test: `Assets/Tests/EditMode~/Editor/SoundUsageScannerTests.cs`

**Interfaces:**
- Produces: `SoundUsageReport SoundUsageScanner.Scan(SoundUsageScanOptions options)`
- Produces: `string SoundUsageScanner.BuildMarkdown(SoundUsageReport report)`
- Produces: `SoundUsageScanOptions` with database path and prefab roots.

- [ ] Write failing tests for SoundDatabase entry collection and unused entry detection.
- [ ] Run the focused EditMode tests and verify RED.
- [ ] Implement result model and SoundDatabase scanning.
- [ ] Run the focused EditMode tests and verify GREEN.

### Task 2: Reference And Embedded Audio Scan

**Files:**
- Modify: `Assets/Project/Editor/SoundUsageScanner.cs`
- Modify: `Assets/Tests/EditMode~/Editor/SoundUsageScannerTests.cs`

**Interfaces:**
- Consumes: `SoundUsageReport`, `SoundUsageReference`, `EmbeddedAudioSourceUsage`.
- Produces: prefab component scanning for `[SoundId]` string fields and embedded AudioSource.

- [ ] Write failing tests for `[SoundId]` references, Skill SFX references, missing DB references, and embedded AudioSource entries.
- [ ] Run the focused EditMode tests and verify RED.
- [ ] Implement prefab/component scanning and missing reference classification.
- [ ] Run the focused EditMode tests and verify GREEN.

### Task 3: Editor Window

**Files:**
- Create: `Assets/Project/Editor/SoundUsageBrowserWindow.cs`
- Modify: `Assets/Project/Editor/SoundUsageScanner.cs`

**Interfaces:**
- Consumes: scanner report and database entry metadata.
- Produces: menu items `Relic/Audio/Open Sound Usage Browser` and `Relic/Audio/Generate Sound Usage Report`.

- [ ] Add EditorWindow UI with refresh, report generation, list, detail, and SoundData editing.
- [ ] Reuse scanner Markdown generation for report output.
- [ ] Verify scripts compile through MSBuild.

### Task 4: Final Verification

**Files:**
- Test: `Assets/Tests/EditMode~/Editor/SoundUsageScannerTests.cs`
- Existing: `Assets/Project/Editor/SkillVfxAudioAudit.cs`

- [ ] Run focused EditMode tests through the Unity Test Runner when available in editor, or compile with approved MSBuild commands.
- [ ] Confirm no runtime battle logic changed.
- [ ] Report changed files, implementation, verification, unverified items, multiplayer impact, and commit/push/PR status.
