# Timeline Flip Line And Help Key Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 병합 플립은 `FipLine` 오브젝트 자체의 `Image` 컴포넌트로 소유자 색상에 맞춰 표시하고 도움말 토글 키를 `\`로 변경한다.

**Architecture:** 기존 `BattleTimelinePreviewEntry.HasMergedFacingFlip`을 표시 조건으로 재사용한다. `BattleTimelineGroupUI`의 활성 크기 루트 안에서 프리팹 배치 `FipLine/image`를 제어하며 전투 상태 계산은 변경하지 않는다.

**Tech Stack:** Unity 6, C#, uGUI, NUnit EditMode

**Spec:** `AI_Docs/2026-11-14-timeline-flip-line-help-key-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 플립이 없는 행동의 `FipLine`이 이전 표시 상태를 유지하지 않는지 확인한다.
- 크기 1/2/3 행동 루트 전환 시 비활성 루트의 `FipLine`이 남지 않는지 확인한다.
- 플레이어와 몬스터의 색상이 뒤바뀌지 않는지 확인한다.
- 사망 예약 색상이 플립 라인 소유자 색상 요구를 덮지 않는지 확인한다.
- 코드 기본 키와 Bootstrap 직렬화 키가 불일치하지 않는지 확인한다.

---

### Task 1: 병합 플립 전용 라인 표시

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineGroupUI.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

**Interfaces:**
- Consumes: `BattleTimelinePreviewEntry.HasMergedFacingFlip`, `IsMonster`
- Produces: 행동 테두리 기본색 유지와 `FipLine/image` 활성·색상 표시

- [ ] 병합 여부와 소유자별 표시 정책의 실패 테스트를 작성한다.
- [ ] 테스트 컴파일이 새 정책 API 부재로 실패하는지 확인한다.
- [ ] 테두리 색상 분기 제거와 플립 라인 활성·초기화 코드를 구현한다.
- [ ] 런타임 및 테스트 어셈블리 컴파일을 확인한다.

### Task 2: 도움말 토글 키 이동

**Files:**
- Modify: `Assets/Project/Scripts/Debug/DebugShortcutHelpPanel.cs`
- Modify: `Assets/Project/Scenes/YDM/Bootstrap.unity`
- Test: `Assets/Tests/EditMode~/DebugShortcutHelpPanelTests.cs`

**Interfaces:**
- Consumes: Unity `KeyCode.Backslash`
- Produces: 코드와 씬에서 동일한 `\` 도움말 토글 키

- [ ] 패널의 기본 토글 키를 동작으로 검증하는 실패 테스트를 작성한다.
- [ ] 기본 키와 Bootstrap 직렬화 값을 `Backslash`로 변경한다.
- [ ] 에디터 어셈블리 컴파일과 YAML 참조를 검증한다.
