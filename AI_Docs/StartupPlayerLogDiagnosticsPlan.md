# Player.log Startup Diagnostics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 제출 빌드의 Player.log에서 시작 및 씬 전환 정지 구간을 마지막 체크포인트로 식별할 수 있게 한다.

**Architecture:** 런타임 시작 전에 초기화되는 `StartupDiagnostics`가 일관된 로그 형식과 마지막 체크포인트를 담당한다. 기존 Bootstrap, 상태 머신, 씬 흐름은 계산 로직을 바꾸지 않고 경계 로그와 예외 기록만 호출한다.

**Tech Stack:** Unity C#, `Debug.Log`, `RuntimeInitializeOnLoadMethod`, NUnit EditMode tests

**Spec:** `AI_Docs/StartupPlayerLogDiagnosticsDesign.md`

## Global Constraints

- 문서와 계획은 `AI_Docs` 아래에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 둔다.
- Unity batchmode는 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업은 수행하지 않는다.
- 기존 게임 진행 및 전투 상태를 변경하지 않는다.

## Review Focus

- Domain reload 비활성화 상태에서도 핸들러가 중복 등록되지 않아야 한다.
- 예외 로깅이 Unity 로그 콜백 재귀를 만들지 않아야 한다.
- 씬 로드가 실패해도 `isLoading`이 원복되어야 한다.
- 상태 `Exit` 또는 `Enter` 예외가 원래 호출자에게 전파되어야 한다.
- 첫 상태 완료 로그에는 실제 활성 씬과 상태가 포함되어야 한다.

---

### Task 1: 진단 로그 코어

**Files:**
- Create: `Assets/Project/Scripts/Core/Diagnostics/StartupDiagnostics.cs`
- Test: `Assets/Tests/EditMode~/StartupDiagnosticsTests.cs`

**Interfaces:**
- Produces: `Begin(string)`, `Success(string)`, `Fail(string, Exception)`, `Complete(string)`, `LastCheckpoint`

- [ ] 테스트에서 고정된 태그, 증가 순번, 마지막 체크포인트, 재초기화 안전성을 먼저 요구한다.
- [ ] 테스트가 구현 부재로 실패하는지 확인한다.
- [ ] 최소 진단 클래스를 구현한다.
- [ ] 컴파일 및 테스트 검증을 수행한다.

### Task 2: Bootstrap 단계 계측

**Files:**
- Modify: `Assets/Project/Scripts/Core/Bootstrap.cs`
- Test: `Assets/Tests/EditMode~/StartupDiagnosticsTests.cs`

**Interfaces:**
- Consumes: Task 1의 진단 API
- Produces: 초기화 단계별 `BEGIN/OK/FAIL`과 첫 상태 완료 로그

- [ ] Bootstrap 필수 체크포인트를 요구하는 실패 테스트를 작성한다.
- [ ] 실패를 확인하고 각 초기화 경계를 계측한다.
- [ ] 예외는 마지막 체크포인트와 함께 기록한 뒤 기존 실패를 보존한다.
- [ ] 컴파일 및 테스트 검증을 수행한다.

### Task 3: 상태 및 씬 전환 계측

**Files:**
- Modify: `Assets/Project/Scripts/Core/GameState/GameStateMachine.cs`
- Modify: `Assets/Project/Scripts/Core/GameState/SceneFlow/SceneFlowManager.cs`
- Test: `Assets/Tests/EditMode~/StartupDiagnosticsTests.cs`

**Interfaces:**
- Consumes: Task 1의 진단 API
- Produces: 상태 Exit/Enter와 씬 전환 연출/로드 경계 로그

- [ ] 상태 및 씬 전환 필수 체크포인트를 요구하는 실패 테스트를 작성한다.
- [ ] 실패를 확인하고 경계 로그 및 예외 원복 처리를 구현한다.
- [ ] 전체 C# 프로젝트를 빌드하고 변경 파일을 검토한다.
- [ ] Player.log 실기동 확인이 필요한 항목을 완료 보고에 분리한다.
