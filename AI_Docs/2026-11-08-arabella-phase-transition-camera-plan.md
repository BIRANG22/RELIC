# Arabella Phase Transition Camera Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 아라벨라 2페이즈 전환에만 적용되는 선택형 2단계 카메라 연출을 인스펙터에서 설정할 수 있게 한다.

**Architecture:** `BattleCameraController`가 전용 직렬화 설정과 카메라 이동 코루틴을 소유하고, `ArabellaPhaseTransition`은 전용 API만 호출한다. 전용 기능이 꺼졌거나 카메라가 없을 때 기존 흐름을 안전하게 유지한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-08-arabella-phase-transition-camera-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs` 안에만 둔다.
- 카메라 연출은 전투 상태 계산과 분리한다.
- 아라벨라 외 전투 카메라 동작은 변경하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 전용 기능 비활성 시 기존 공용 줌 폴백이 유지되는지 검증한다.
- 2차 단계 비활성 시 1차 단계만 정상 종료되는지 검증한다.
- 이동 시간과 유지 시간이 0이어도 즉시 완료되는지 검증한다.
- 카메라 또는 대상이 없을 때 예외 없이 종료되는지 검증한다.
- 기존 씬에 새 필드가 직렬화되지 않은 상태에서도 안전한 기본값을 사용하는지 검증한다.

---

### Task 1: 전용 카메라 설정과 이동 API

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`
- Test: `Assets/Tests/EditMode~/ArabellaPhaseTransitionCameraTests.cs`

**Interfaces:**
- Produces: `IEnumerator PlayArabellaPhaseTransitionCamera(Transform target)`
- Produces: `IEnumerator ReturnFromArabellaPhaseTransitionCamera()`

- [ ] 전용 설정 기본값과 2단계 활성 조건을 검증하는 실패 테스트를 작성한다.
- [ ] 테스트가 새 API 부재로 실패하는지 확인한다.
- [ ] `Arabella Phase Transition Camera` 인스펙터 섹션과 두 단계 설정을 추가한다.
- [ ] 대상 기준 XY 오프셋, Z/직교 크기, 이동 시간, 곡선, 단계 유지 시간을 적용하는 전용 코루틴을 구현한다.
- [ ] 전용 복귀 시간으로 중립 전투 위치에 복귀하는 API를 구현한다.
- [ ] 전용 비활성, 2차 단계 비활성, 0초 설정, null 입력 테스트를 통과시킨다.

### Task 2: 아라벨라 전환 흐름 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Monster/ArabellaPhaseTransition.cs`
- Test: `Assets/Tests/EditMode~/ArabellaPhaseTransitionCameraTests.cs`

**Interfaces:**
- Consumes: `PlayArabellaPhaseTransitionCamera(Transform)`
- Consumes: `ReturnFromArabellaPhaseTransitionCamera()`

- [ ] 전환 루틴이 전용 진입·복귀 API를 사용하는지 검증하는 실패 테스트를 작성한다.
- [ ] 기존 `ZoomTo`/`ReturnPanelDown` 직접 호출을 전용 API 호출로 교체한다.
- [ ] 전환의 런타임 데이터 초기화와 애니메이션 순서가 유지되는지 테스트한다.

### Task 3: 정적 검증

**Files:**
- Verify: 변경 코드 및 EditMode 테스트

- [ ] `Assembly-CSharp.csproj`를 빌드해 런타임 컴파일을 검증한다.
- [ ] `Assembly-CSharp-Editor.csproj`를 빌드해 테스트 컴파일을 검증한다.
- [ ] Unity 에디터 실행 없이 가능한 EditMode 테스트를 실행하고 결과를 확인한다.
- [ ] 씬 YAML에서 기존 `BattleCameraController` 직렬화 호환성을 확인한다.
