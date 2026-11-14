# Monster Info Status Icon Color Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 몬스터 정보 패널의 상태효과 아이콘에 전투 HUD와 동일한 효과별 스프라이트와 분류 색상을 적용한다.

**Architecture:** `MonsterInfoStatusItemUI`가 기존 `StatusEffectIconDatabase`에서 스프라이트와 색상을 함께 조회한다. 색상 정책은 데이터베이스에 유지하고 UI는 결과를 표시만 한다.

**Tech Stack:** Unity 6, C#, UGUI Image, NUnit EditMode

**Spec:** `AI_Docs/2026-11-14-monster-info-status-icon-color-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/` 아래에 작성한다.
- Unity batchmode와 에디터 직접 제어를 사용하지 않는다.
- 전투 핵심 상태와 네트워크 모델을 변경하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 버프·디버프·고유 특성 색상이 기존 HUD 정책과 동일해야 한다.
- 중립·미등록 효과는 흰색이어야 한다.
- 아이콘의 기존 알파값은 색상 적용 후에도 유지되어야 한다.
- 데이터가 제거되면 이전 효과 색상이 남지 않아야 한다.
- 아이콘이 없는 효과는 Image가 비활성화되어야 한다.

---

### Task 1: 몬스터 정보 상태 아이콘 표시

**Files:**
- Modify: `Assets/Project/Scripts/MonsterInfoStatusItemUI.cs`
- Test: `Assets/Tests/EditMode~/MonsterInfoStatusItemUITests.cs`

**Interfaces:**
- Consumes: `StatusEffectIconDatabase.TryGetIcon`, `StatusEffectIconDatabase.GetIconColor`
- Produces: `MonsterInfoStatusItemUI.Bind(StatusEffectRuntimeData)`의 색상 적용 동작

- [x] **Step 1: 실패하는 EditMode 테스트 작성**

버프·디버프·고유 특성·중립 색상과 알파 보존, Clear 시 흰색 복원을 검증한다.

- [x] **Step 2: 테스트 포함 Editor 컴파일로 API 경계 확인**

Run: `MSBuild Assembly-CSharp-Editor.csproj` with `Temp/CodexRequestedEditModeTests.targets`

- [x] **Step 3: 최소 구현**

`MonsterInfoStatusItemUI.Bind`에서 효과 아이콘과 색상을 같은 데이터베이스에서 조회해 Image에 적용한다. RGB는 조회 색상, 알파는 현재 Image 알파를 사용한다.

- [x] **Step 4: 전체 런타임·Editor 컴파일 및 diff 검사**

Expected: 컴파일 오류 0, `git diff --check` 통과.
