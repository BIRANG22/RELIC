# Grid Effect Preview Tooltip Implementation Plan

> **For agentic workers:** Execute inline with `superpowers:executing-plans`; do not create a branch, worktree, commit, push, or PR without separate user approval.

**Goal:** 배치 미리보기 콜라이더와 전투 실행 중 UI 차단을 함께 수정해 그리드 효과 툴팁 호버를 복구한다.

**Architecture:** `GridEffectHoverTarget`이 기존 비활성 콜라이더를 trigger 호버 영역으로 재활성화하고, 콜라이더가 전혀 없을 때만 fallback을 생성한다. `GridEffectTooltipUI`는 전투 종료만 표시 차단 조건으로 유지한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-22-grid-effect-preview-tooltip-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode는 실행하지 않는다.
- 전투 상태 및 멀티플레이 동기화 경계는 변경하지 않는다.
- 기존 사용자 변경 파일은 수정하지 않는다.

## Review Focus

- 비활성 원본 콜라이더가 trigger로 재활성화되어야 한다.
- 활성 기존 콜라이더가 있으면 중복 콜라이더를 만들지 않아야 한다.
- `fallbackSize`가 없으면 기존 경고 동작을 유지해야 한다.
- 재초기화 시 콜라이더가 중복 생성되지 않아야 한다.
- 전투 실행 여부가 툴팁 표시를 막지 않아야 한다.

---

### Task 1: 미리보기 호버 콜라이더 회귀 수정

**Files:**
- Modify: `Assets/Project/Scripts/GridEffectHoverTarget.cs`
- Modify: `Assets/Project/Scripts/GridEffectTooltipUI.cs`
- Create: `Assets/Tests/EditMode~/GridEffectHoverTargetTests.cs`

**Interfaces:**
- Consumes: `GridEffectHoverTarget.Attach(GameObject, string, Vector2?, BattleGridEffectController, int)`
- Produces: 기존 콜라이더 재활성화와 전투 실행 중 툴팁 표시 동작

- [ ] 비활성 원본 콜라이더가 동일 크기의 trigger로 재활성화되는 실패 테스트를 작성한다.
- [ ] 구현 전 테스트를 실행해 기대 원인으로 실패하는지 확인한다.
- [ ] 활성 콜라이더 검색과 fallback 생성 로직을 최소 범위로 구현한다.
- [ ] 신규 회귀 테스트와 관련 테스트를 실행한다.
- [ ] `Assembly-CSharp-Editor.csproj` 컴파일 및 정적 검사를 수행한다.

### Task 2: 겹친 그리드 효과 툴팁 소유권

**Files:**
- Modify: `Assets/Project/Scripts/GridEffectHoverTarget.cs`
- Modify: `Assets/Project/Scripts/GridEffectTooltipUI.cs`
- Modify: `Assets/Tests/EditMode~/GridEffectHoverTargetTests.cs`

- [ ] 마지막 진입 대상 이탈 시 이전 호버 대상으로 복귀하는 실패 테스트를 작성한다.
- [ ] 호버 진입 순서를 기반으로 우선 대상을 선택한다.
- [ ] 현재 우선 대상만 툴팁 표시와 위치를 갱신하도록 제한한다.
- [ ] 프로젝트 및 테스트 어셈블리를 컴파일하고 diff를 검사한다.

