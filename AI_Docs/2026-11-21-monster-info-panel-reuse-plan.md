# Monster Info Panel Reuse Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 몬스터 정보 패널 반복 개폐에서 동적 UI와 블러 복제 재생성 비용이 누적되지 않게 한다.

**Architecture:** 작은 재사용 컴포넌트 풀을 추가하고 몬스터 스킬·상태 항목이 이를 사용한다. 풀의 계층 확장 여부를 반환하여 실제 계층 변경이 있을 때만 블러 복제본을 무효화한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-21-monster-info-panel-reuse-design.md`

## Global Constraints

- 문서와 테스트는 각각 `AI_Docs`, `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 및 에디터 직접 제어를 사용하지 않는다.
- 전투 상태와 멀티플레이 동기화 경계를 변경하지 않는다.
- 현재 브랜치와 작업 공간을 유지하고 커밋·Push·PR을 수행하지 않는다.

## Review Focus

- 같은 수량으로 반복 갱신할 때 새 항목이 생성되지 않아야 한다.
- 표시 수량 감소 후 증가 시 비활성 항목이 재사용되어야 한다.
- null 또는 파괴된 풀 항목은 필요한 시점에 안전하게 보충되어야 한다.
- 패널 닫기에서는 항목을 파괴하지 않아야 한다.
- 블러 무효화는 풀의 계층이 증가했을 때만 실행되어야 한다.

---

### Task 1: 재사용 컴포넌트 풀

**Files:**
- Create: `Assets/Project/Scripts/UI/ReusableComponentPool.cs`
- Create: `Assets/Tests/EditMode~/ReusableComponentPoolTests.cs`

**Interfaces:**
- Produces: `ReusableComponentPool<T>.Prepare(int, Func<T>)`, `DeactivateUnused(int)`, `DeactivateAll()`

- [ ] 실제 GameObject 컴포넌트로 확장·축소·재확장 동작을 검증하는 실패 테스트를 작성한다.
- [ ] 테스트를 컴파일하여 타입 부재로 실패하는 것을 확인한다.
- [ ] 필요한 수량만 생성하고 나머지를 비활성화하는 최소 풀을 구현한다.
- [ ] 테스트 및 런타임 어셈블리 컴파일을 확인한다.

### Task 2: 몬스터 패널 풀 적용과 블러 갱신 제한

**Files:**
- Modify: `Assets/Project/Scripts/BattleMonsterInfoCanvasUI.cs`
- Modify: `Assets/Tests/EditMode~/ReusableComponentPoolTests.cs`

**Interfaces:**
- Consumes: Task 1의 `ReusableComponentPool<T>`
- Produces: 스킬·상태 항목 재사용 및 계층 확장 기반 블러 갱신

- [ ] 패널 사용 시 풀의 확장 결과가 블러 갱신 여부를 결정하는 회귀 테스트를 먼저 추가한다.
- [ ] 테스트 실패를 확인한다.
- [ ] 기존 생성·파괴 리스트를 풀 기반 재바인딩으로 교체한다.
- [ ] 전체 컴파일과 diff 검사를 수행한다.
