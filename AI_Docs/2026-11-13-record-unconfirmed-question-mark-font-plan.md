# Record Unconfirmed Question Mark Font Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기록서 미발견 슬롯이 `ㅂ` 모양이 아닌 정상 `?`를 표시하게 한다.

**Architecture:** 공용 손상 폰트는 유지하고 `RecordIconSlot.prefab`의 `Unconfirmed` TMP 컴포넌트만 정상 `DungGeunMo SDF`로 전환한다. 실제 프리팹 참조를 검사하는 EditMode 테스트로 회귀를 방지한다.

**Tech Stack:** Unity 6, C#, TextMeshPro, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-13-record-unconfirmed-question-mark-font-design.md`

## Global Constraints

- 문서와 테스트는 각각 `AI_Docs`, `Assets/Tests/EditMode~` 아래에만 작성한다.
- Unity 에디터를 직접 제어하거나 batchmode 테스트를 실행하지 않는다.
- 기록서 외 UI와 공용 폰트 에셋은 변경하지 않는다.

## Review Focus

- 프리팹에서 `Unconfirmed` 오브젝트를 찾지 못하면 테스트가 실패해야 한다.
- 문자 값이 `?`가 아니면 테스트가 실패해야 한다.
- 손상된 Legacy 폰트로 되돌아가면 테스트가 실패해야 한다.
- 안전한 폰트가 `?` 문자를 제공하지 않으면 테스트가 실패해야 한다.
- 기존 미발견 표시 활성화 조건은 변경하지 않아야 한다.

---

### Task 1: 기록서 미발견 글리프 수정

**Files:**
- Create: `Assets/Tests/EditMode~/RecordIconSlotUnconfirmedFontTests.cs`
- Modify: `Assets/Project/PrefabsR/RecordIconSlot.prefab`

**Interfaces:**
- Consumes: `RecordIconSlot.prefab`의 `Unconfirmed` TMP 컴포넌트
- Produces: 정상 `?` 글리프를 가진 프리팹 참조

- [ ] **Step 1: 실패하는 프리팹 회귀 테스트 작성**
- [ ] **Step 2: 현재 Legacy 폰트 참조로 검사가 실패하는지 확인**
- [ ] **Step 3: `Unconfirmed`의 폰트와 머티리얼을 `DungGeunMo SDF`로 교체**
- [ ] **Step 4: 테스트 포함 에디터 어셈블리 컴파일 및 프리팹 검사 통과 확인**
- [ ] **Step 5: `git diff --check`와 변경 범위 확인**
