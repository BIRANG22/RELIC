# Battle Camera Explicit Movement Focus Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 일반 이동과 돌진 넉백의 카메라 포커스를 명시적 행동 이벤트로 제어해 포커스 왕복 흔들림을 제거한다.

**Architecture:** `BattleCameraController`는 명시적으로 전달된 Transform만 추적한다. `BattleActionRunner`가 일반 Move 및 돌진 넉백의 연출 순서에 맞춰 추적 시작·전환·종료를 호출한다.

**Tech Stack:** Unity, C#, NUnit EditMode tests

**Spec:** `AI_Docs/BattleCamera_ExplicitMovementFocus_Design.md`

## Global Constraints

- 전투 판정과 상태 변경 로직은 수정하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트를 실행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 넉백 Transform 이동만으로 카메라 포커스가 자동 변경되지 않아야 한다.
- 같은 유닛의 반복 직접 이동 시작은 줌 코루틴을 불필요하게 재시작하지 않아야 한다.
- 직접 이동 종료는 해당 유닛이 현재 포커스일 때만 follow를 해제해야 한다.
- 돌진 충돌 시 공격자에서 피격자로 포커스가 한 번만 전환되어야 한다.
- 넉백이 실패하거나 대상이 사라지면 기존 공격 카메라 흐름이 안전하게 종료되어야 한다.

---

### Task 1: 명시적 카메라 포커스 계약 검증

**Files:**
- Create: `Assets/Tests/EditMode~/BattleCameraExplicitMovementFocusTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`

**Interfaces:**
- Consumes: `BeginDirectGridMovement(Transform)`, `EndDirectGridMovement(Transform)`
- Produces: Transform 자동 감지가 없는 명시적 이동 포커스 계약

- [ ] **Step 1:** Transform 변화만으로 포커스가 선택되지 않고 명시적 시작에서만 선택되는 실패 테스트를 작성한다.
- [ ] **Step 2:** 테스트용 C# 프로젝트 빌드로 새 테스트가 기존 자동 감지 구조를 검출하는지 확인한다.
- [ ] **Step 3:** `BattleCameraController`의 Transform 폴링 필드·Update 호출·감지 메서드를 제거한다.
- [ ] **Step 4:** 테스트용 C# 프로젝트 빌드와 런타임 어셈블리 컴파일을 확인한다.

### Task 2: 일반 Move 행동 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Modify: `Assets/Tests/EditMode~/BattleCameraExplicitMovementFocusTests.cs`

**Interfaces:**
- Consumes: `BeginDirectGridMovement(Transform)`, `EndDirectGridMovement(Transform)`
- Produces: 플레이어·몬스터 일반 Move의 명시적 추적 수명

- [ ] **Step 1:** 일반 Move 실행 경로가 시작/종료 API를 사용하는지 검증하는 실패 테스트를 작성한다.
- [ ] **Step 2:** 플레이어 단일/다단 이동과 몬스터 일반 이동의 실제 시각 이동 구간을 `try/finally` 추적으로 감싼다.
- [ ] **Step 3:** 테스트용 C# 프로젝트 빌드와 런타임 어셈블리 컴파일을 확인한다.

### Task 3: 돌진 후 넉백 대상 포커스 전환

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Effect/Effects/Move/KnockbackEffect.cs`
- Modify: `Assets/Tests/EditMode~/BattleCameraExplicitMovementFocusTests.cs`

**Interfaces:**
- Consumes: 명시적 카메라 follow API와 넉백 시각 이동 완료 신호
- Produces: 돌진자에서 넉백 대상까지 이어지는 단일 포커스 전환

- [ ] **Step 1:** 돌진 넉백이 대상 추적을 요청하고 넉백 완료 뒤 follow를 해제하는 실패 테스트를 작성한다.
- [ ] **Step 2:** 넉백 연출에 선택적 완료 콜백 또는 완료 대기 계약을 추가한다.
- [ ] **Step 3:** 돌진 공격에서 넉백 대상 추적을 시작하고 실제 넉백 연출 완료 뒤 종료한다.
- [ ] **Step 4:** 실패·사망·대상 소실 경로에서도 카메라 추적이 남지 않도록 정리한다.

### Task 4: 전체 검증

**Files:**
- Verify: 수정된 코드, 테스트, 문서

**Interfaces:**
- Consumes: Tasks 1-3 결과
- Produces: 컴파일 가능한 최종 변경

- [ ] **Step 1:** 관련 테스트 어셈블리를 컴파일한다.
- [ ] **Step 2:** `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] **Step 3:** `git diff --check`와 변경 범위를 검토한다.
- [ ] **Step 4:** Unity 에디터 화면 검증이 필요한 카메라 감각은 미검증 항목으로 기록한다.
