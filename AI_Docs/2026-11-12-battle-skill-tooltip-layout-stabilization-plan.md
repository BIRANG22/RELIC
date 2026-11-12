# Battle Skill Tooltip Layout Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 스킬 정보 툴팁의 레이아웃이 확정된 뒤 효과 정보 툴팁을 표시해 등장 직후 위치가 움직이지 않게 한다.

**Architecture:** `BattleCharacterPanelUI`가 효과 정보 표시 가능 프레임을 추적한다. `LateUpdate`에서 스킬 툴팁 레이아웃과 위치를 먼저 갱신한 뒤, 안정화 프레임 조건을 만족할 때만 효과 정보 툴팁을 표시한다.

**Tech Stack:** Unity 6, C#, Unity UI, TextMesh Pro, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-12-battle-skill-tooltip-layout-stabilization-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트는 수행하지 않는다.
- 기존 미커밋 변경을 보존한다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 호버 시작 프레임에 효과 정보가 먼저 노출되지 않아야 한다.
- 다음 프레임에는 효과 정보가 정상적으로 표시되어야 한다.
- 다른 스킬로 전환하면 안정화 대기가 다시 적용되어야 한다.
- 툴팁을 닫으면 기존 효과 정보와 준비 상태가 함께 초기화되어야 한다.
- 예약 행동 후 툴팁 재시작 흐름에서도 동일한 순서를 유지해야 한다.

---

### Task 1: 효과 정보 표시 프레임 제어

**Files:**
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- Modify: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

**Interfaces:**
- Produces: `IsSkillEffectTooltipLayoutReady(int currentFrame, int readyFrame)`

- [ ] 안정화 프레임 조건에 대한 실패 테스트를 작성한다.
- [ ] 테스트가 신규 API 부재로 실패하는지 확인한다.
- [ ] 스킬 툴팁 표시 시 다음 프레임을 준비 프레임으로 기록한다.
- [ ] 숨김·재시작 시 준비 상태를 초기화한다.

### Task 2: 레이아웃 갱신 순서 수정 및 검증

**Files:**
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`

**Interfaces:**
- Consumes: Task 1의 안정화 프레임 상태
- Produces: `스킬 정보 위치 → 효과 정보 위치` LateUpdate 순서

- [ ] `LateUpdate` 호출 순서를 변경한다.
- [ ] 효과 정보 갱신 진입 조건에 안정화 프레임 검사를 추가한다.
- [ ] 런타임 및 에디터 어셈블리를 컴파일한다.
- [ ] diff와 요청 범위를 검토한다.

