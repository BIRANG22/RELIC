# Battle Presentation And Reservation Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 예약 직접 취소 리두, 아라벨라 카메라·전기음, 배속 VFX 사운드, 기억 보상 넘기기 버튼의 회귀 문제를 수정한다.

**Architecture:** 전투 명령 히스토리는 로컬 입력 경계에서 기록하고 네트워크 상태 적용과 분리한다. 카메라와 사운드 수정은 전투 판정이 아닌 프레젠테이션 계층에 한정한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity YAML

**Spec:** `AI_Docs/2026-11-13-battle-presentation-and-reservation-fixes-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode와 에디터 직접 제어를 사용하지 않는다.
- 현재 작업공간을 사용하며 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 네트워크로 수신한 삭제가 로컬 리두 히스토리를 중복 기록하지 않아야 한다.
- 새 예약 이후 이전 직접 취소 리두 분기가 무효화되어야 한다.
- VFX cue의 사용자 지정 pitch와 배속 배율이 함께 보존되어야 한다.
- 시네마틱 종료 직후 LateUpdate에서 잔여 오프셋이 다시 적용되지 않아야 한다.
- 비활성 SkipButton은 클릭음을 재생하지 않아야 한다.

---

### Task 1: 직접 취소 리두

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

- [x] 기존 직접 취소 후 리두 회귀 테스트의 실패 원인을 확인한다.
- [x] 로컬 삭제와 네트워크 적용 경계를 분리하고 제거 항목을 리두 스택에 기록한다.
- [x] 관련 EditMode 테스트 소스를 보강한다.

### Task 2: 카메라 인계 안정화

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`
- Test: `Assets/Tests/EditMode~/BattleCameraOwnershipBoundaryTests.cs`

- [x] 시네마틱 종료 상태 초기화 테스트를 추가한다.
- [x] 인계 직전 패럴랙스·외부 흔들림·추적 속도를 초기화한다.
- [x] 카메라 경계 테스트 소스를 보강한다.

### Task 3: VFX SFX 배속 동기화 및 아라벨라 cue

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleVfxAudioUtility.cs`
- Modify: `Assets/Project/Scripts/Core/Managers/AudioManager.cs`
- Modify: `Assets/DB/SoundDatabase.asset`
- Test: `Assets/Tests/EditMode~/SoundDatabaseVfxOwnershipTests.cs`

- [x] 유효 cue pitch와 아라벨라 일회성 cue 테스트를 추가한다.
- [x] VFX 전용 cue에만 연출 배속 pitch를 전달한다.
- [x] 아라벨라 준비 cue를 행동당 한 번으로 설정한다.

### Task 4: 기억 보상 넘기기 버튼 SFX

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/BattleRewardSkillSkipButtonLockTests.cs`

- [x] 기억 보상 SkipButton 클릭 순서 테스트를 추가한다.
- [x] 잠금 전에 `ui.normal.click`을 명시적으로 재생하고 기존 포인터 클릭 재생을 끈다.
- [x] 보상 버튼 테스트 소스를 보강한다.

### Task 5: 전체 검증

- [x] Editor 프로젝트 빌드로 컴파일을 검증한다.
- [x] 런타임 프로젝트를 빌드한다.
- [x] diff와 YAML 참조를 점검한다.

## Verification Note

프로젝트 규칙에 따라 Unity batchmode와 에디터 직접 제어를 사용하지 않았다. `Assets/Tests/EditMode~/`는 일반 프로젝트 빌드 대상에서 제외되므로 테스트 실행은 Unity Test Runner 수동 확인 대상으로 남긴다.
