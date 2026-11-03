# Battle Camera Ownership Unification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 배틀 메인 카메라의 몬스터 포커스, 보스 시네마틱, 외부 셰이크 소유권을 `BattleCameraController`로 통합한다.

**Architecture:** 외부 UI와 시퀀스는 카메라 데이터를 계산하거나 직접 Transform을 쓰지 않고 컨트롤러 API만 호출한다. 범용 컴포넌트는 배틀 컨트롤러가 없는 씬에서 기존 fallback을 유지한다.

**Tech Stack:** Unity 6, C#, Coroutine, NUnit EditMode tests, Unity scene/prefab YAML

**Spec:** `AI_Docs/2026-11-03-battle-camera-ownership-unification-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 기존 미커밋 변경을 보존한다.
- 커밋, Push, PR, 브랜치/worktree 작업을 수행하지 않는다.

## Review Focus

- 몬스터 정보창을 닫을 때 캐릭터 선택이 남아 있으면 카메라 포커스가 잘못 복귀하지 않는가.
- 보스 시네마틱 중 오브젝트 비활성화/중단 시 카메라가 캡처 상태로 복구되는가.
- 시네마틱 복귀가 오래된 상태를 적용해 새 전투 포커스를 덮지 않는가.
- 외부 셰이크 종료/비활성화 시 오프셋이 카메라 기준 위치에 남지 않는가.
- 비배틀 씬의 `SpriteTransformSequence`와 `CameraShakeController` fallback이 유지되는가.

---

### Task 1: 몬스터 정보 카메라 통합

**Files:**
- Modify: `Assets/Project/Scripts/BattleMonsterInfoCanvasUI.cs`
- Modify: `Assets/Project/Scripts/UI/Battle/Canvas/UIDissolveReveal.cs`
- Test: `Assets/Tests/EditMode~/BattleCameraOwnershipBoundaryTests.cs`

**Interfaces:**
- Consumes: `BattleCameraController.FocusMonsterInfoWithPanelSide`, `ReturnDefaultFromMonsterInfoFocus`
- Produces: UI 계층의 단일 카메라 요청 경로

- [ ] 직접 `Camera.main`/Transform 변경을 탐지하는 실패 테스트를 작성한다.
- [ ] UI 자체 카메라 필드와 코루틴을 제거하고 컨트롤러 호출로 교체한다.
- [ ] 중복 요청을 만드는 `UIDissolveReveal` 카메라 호출을 제거한다.
- [ ] 소스 경계와 컴파일을 검증한다.

### Task 2: 보스 시네마틱 카메라 통합

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`
- Modify: `Assets/Project/Scripts/Art/SpriteTransformSequence.cs`
- Test: `Assets/Tests/EditMode~/BattleCameraOwnershipBoundaryTests.cs`

**Interfaces:**
- Produces: `BeginCinematicCamera`, `ApplyCinematicCameraPose`, `RestoreCinematicCameraImmediate`, 시네마틱 활성 상태
- Consumes: 프리팹의 기존 camera motion 위치/회전/size 데이터

- [ ] 배틀 컨트롤러 사용 여부와 직접 메인 카메라 쓰기 경계를 테스트한다.
- [ ] 컨트롤러에 시네마틱 캡처/적용/복귀 API를 추가한다.
- [ ] 시퀀스가 배틀에서는 컨트롤러 API, 비배틀에서는 기존 fallback을 사용하게 한다.
- [ ] 보스 프리팹 데이터가 보존되었는지 YAML을 확인한다.

### Task 3: 외부 셰이크 통합

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`
- Modify: `Assets/Project/Scripts/Art/CameraShakeController.cs`
- Test: `Assets/Tests/EditMode~/BattleCameraOwnershipBoundaryTests.cs`

**Interfaces:**
- Produces: `UsesCameraTransform(Transform)`, `PlayExternalShake`, `StopExternalShake`
- Consumes: 기존 `Shake(duration, strength, frequency)` 요청

- [ ] 배틀 메인 카메라 소유 여부 판정 테스트를 작성한다.
- [ ] 컨트롤러 외부 셰이크 오프셋 계층을 구현한다.
- [ ] 범용 셰이크가 배틀 카메라일 때 위임하고 다른 카메라에서는 기존 동작을 유지하게 한다.
- [ ] 씬 YAML에서 메인 카메라 구성과 직렬화 값 보존을 확인한다.

### Task 4: 최종 검증

**Files:**
- Verify: `Assembly-CSharp.csproj`
- Verify: `Assembly-CSharp-Editor.csproj`
- Verify: `Assets/Project/Scenes/YDM/Battle.unity`
- Verify: `Assets/Project/PrefabsR/Battle/BackGround/St1_boss.prefab`

**Interfaces:**
- Consumes: Task 1~3 구현
- Produces: 컴파일, 경계 검사, 미검증 항목 보고

- [ ] 런타임/에디터 프로젝트를 컴파일한다.
- [ ] 직접 메인 카메라 변경 잔여 경로를 재검색한다.
- [ ] 변경 diff, 기존 사용자 변경 보존, 멀티플레이 경계를 점검한다.
