# Grid Effect Preview Tooltip Implementation Plan

> **For agentic workers:** Execute inline with `superpowers:executing-plans`; do not create a branch, worktree, commit, push, or PR without separate user approval.

**Goal:** 비활성화된 배치 미리보기 콜라이더를 건드리지 않고 그리드 효과 툴팁 호버를 복구한다.

**Architecture:** `GridEffectHoverTarget`이 활성 콜라이더를 우선 사용하고, 없을 때만 전달된 `fallbackSize`로 전용 호버 콜라이더를 생성한다. 전투 상태와 VFX 표시 구조는 그대로 유지한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-22-grid-effect-preview-tooltip-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode는 실행하지 않는다.
- 전투 상태 및 멀티플레이 동기화 경계는 변경하지 않는다.
- 기존 사용자 변경 파일은 수정하지 않는다.

## Review Focus

- 비활성 원본 콜라이더가 실수로 재활성화되지 않아야 한다.
- 활성 기존 콜라이더가 있으면 중복 콜라이더를 만들지 않아야 한다.
- `fallbackSize`가 없으면 기존 경고 동작을 유지해야 한다.
- 재초기화 시 전용 콜라이더가 중복 생성되지 않아야 한다.
- 생성된 콜라이더 크기가 전달값과 일치해야 한다.

---

### Task 1: 미리보기 호버 콜라이더 회귀 수정

**Files:**
- Modify: `Assets/Project/Scripts/GridEffectHoverTarget.cs`
- Create: `Assets/Tests/EditMode~/GridEffectHoverTargetTests.cs`

**Interfaces:**
- Consumes: `GridEffectHoverTarget.Attach(GameObject, string, Vector2?, BattleGridEffectController, int)`
- Produces: 활성 기존 콜라이더 선택 및 필요 시 fallback 호버 콜라이더 생성 동작

- [ ] 비활성 원본 콜라이더를 보존하면서 활성 fallback 콜라이더가 생성되는 실패 테스트를 작성한다.
- [ ] 구현 전 테스트를 실행해 기대 원인으로 실패하는지 확인한다.
- [ ] 활성 콜라이더 검색과 fallback 생성 로직을 최소 범위로 구현한다.
- [ ] 신규 회귀 테스트와 관련 테스트를 실행한다.
- [ ] `Assembly-CSharp-Editor.csproj` 컴파일 및 정적 검사를 수행한다.

