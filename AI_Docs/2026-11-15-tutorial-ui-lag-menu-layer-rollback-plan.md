# Tutorial UI Lag and Menu Layer Rollback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 튜토리얼 렌더 직전 전역 UI 검색을 제거하고 메뉴에서 연 모달이 메뉴 위에 표시되도록 동일 레이어 정렬 구조를 복구한다.

**Architecture:** 튜토리얼 툴팁 Canvas를 발견 단계에서 등록하고 렌더 직전에는 등록 목록만 재적용한다. 메뉴 전용 Sorting Layer 가드는 제거하고 하나의 Sorting Layer 안에서 정렬 구간을 분리한다.

**Tech Stack:** Unity 6, C#, uGUI Canvas, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-15-tutorial-ui-lag-menu-layer-rollback-design.md`

## Global Constraints

- 현재 작업 공간과 브랜치에서 작업한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 등록 이후 툴팁 Canvas 정렬이 다른 UI 코드에 의해 변경되어도 렌더 직전에 복구되어야 한다.
- 렌더 직전 정렬 복구는 새 툴팁을 발견하거나 씬 전체를 검색하지 않아야 한다.
- 일반 및 보상 튜토리얼 효과 툴팁은 암막보다 앞에 있으면서 메뉴보다 낮아야 한다.
- 메뉴는 튜토리얼 UI보다 앞에 있어야 한다.
- 설정·기록·확인 모달은 메뉴보다 앞에 있어야 한다.

---

### Task 1: 툴팁 Canvas 캐시 적용

**Files:**
- Modify: `Assets/Project/Scripts/TutorialFocusGuideRunner.cs`
- Modify: `Assets/Project/Scripts/UI/TutorialForegroundTooltipCanvas.cs`
- Test: `Assets/Tests/EditMode~/UiLayerAndMonsterEffectRegressionTests.cs`

**Interfaces:**
- Produces: `RefreshForegroundTooltipRegistry()`, `EnforceRegisteredForegroundTooltipLayers()`, 정렬 순서 정책 API

- [x] 렌더 직전 등록 Canvas만 복구하고 새 툴팁은 발견하지 않는 실패 테스트를 작성한다.
- [x] 회귀 테스트 포함 에디터 컴파일이 신규 API 부재로 실패하는지 확인한다.
- [x] 발견 단계와 재적용 단계를 분리하고 렌더 이벤트에서는 캐시만 순회한다.
- [x] 회귀 테스트 포함 에디터 컴파일을 다시 실행한다.

### Task 2: 메뉴 Sorting Layer 가드 제거

**Files:**
- Modify: `Assets/Project/Scripts/UI/UIPanelButton.cs`
- Modify: `Assets/Project/Scripts/BattleMenuEscapeInputController.cs`
- Modify: `Assets/Project/Scripts/UI/UnitStatusEffectTooltipUI.cs`
- Test: `Assets/Tests/EditMode~/UiLayerAndMonsterEffectRegressionTests.cs`

**Interfaces:**
- Consumes: Task 1의 튜토리얼 정렬 순서 정책
- Produces: 동일 Sorting Layer의 `튜토리얼 UI < 메뉴 < 메뉴 모달` 순서

- [x] 메뉴와 모달의 상대 순서를 검증하는 실패 테스트를 작성한다.
- [x] 메뉴 전용 최상위 Sorting Layer 가드와 관련 참조를 제거한다.
- [x] 일반 튜토리얼 포커스 기준 order를 29800으로 조정하고 보상 튜토리얼 최대 32630보다 높은 메뉴 32700/32701과 모달 32702 이상을 적용한다.
- [x] 런타임 및 회귀 테스트 포함 에디터 컴파일을 실행한다.
- [x] 변경 파일 정적 검사와 최종 자체 리뷰를 수행한다.
