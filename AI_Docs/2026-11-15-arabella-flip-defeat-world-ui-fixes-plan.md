# Arabella Flip Defeat World UI Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 아라벨라 등장 카메라, 미결합 플립 교체 취소, 탐사 종료 월드 표시물 정리 회귀를 수정한다.

**Architecture:** 기존 `BossSpawnPresentation`과 예약 취소 API를 재사용하고, 전투 종료 공통 경계에 표현 정리 API를 연결한다. 전투 상태 계산과 네트워크 모델은 변경하지 않는다.

**Tech Stack:** Unity 6, C#, NUnit EditMode, Unity YAML

**Spec:** `AI_Docs/2026-11-15-arabella-flip-defeat-world-ui-fixes-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.
- UI와 카메라는 전투 결과를 계산하지 않는다.

## Review Focus

- 같은 캐릭터 재선택은 미결합 플립을 취소하지 않아야 한다.
- 캐릭터 교체는 `FlipBeforeAction`으로 병합된 기존 행동을 제거하지 않아야 한다.
- 팝업 정리는 생성된 전투 팝업만 제거하고 씬의 다른 UI를 제거하지 않아야 한다.
- 전투 종료 정리는 승리·일반 패배·침식 100 패배에 모두 적용되어야 한다.
- 아라벨라 조명·대사·VFX 타이밍은 카메라 단계 복원 후에도 유지되어야 한다.

---

### Task 1: 회귀 계약 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/ReportedBugfixBatchTests.cs`

- [ ] 아라벨라 첫 sequence step이 `CameraMove(0)`인지 검사한다.
- [ ] `SelectCharacter()`가 실제 캐릭터 변경에서 미결합 플립을 취소하는지 검사한다.
- [ ] 전투 종료 공통 준비가 팝업·캐릭터 HUD·몬스터 HUD 정리를 호출하는지 검사한다.
- [ ] 현재 코드에서 테스트가 실패하는지 확인한다.

### Task 2: 최소 수정 구현

**Files:**
- Modify: `Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Arabella.prefab`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/UI/BattleDamageTextPopupUI.cs`
- Modify: `Assets/Project/Scripts/BattleCharacterHUDController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleResultChecker.cs`

- [ ] 첫 카메라 단계를 `CameraMove`로 복원한다.
- [ ] 아라벨라에서만 종료 복귀 블렌드를 끄고 현재 자세를 유지한 채 카메라 소유권을 해제한다.
- [ ] 캐릭터 ID 변경 시에만 `CancelUnmergedFacingFlips()`를 호출한다.
- [ ] 팝업 인스턴스가 소유한 생성 오브젝트와 대기열을 정리하는 API를 구현한다.
- [ ] 캐릭터 HUD 즉시 정리 API와 몬스터 임시 HUD 정리를 전투 종료 공통 지점에 연결한다.
- [ ] 회귀 테스트가 통과하도록 최소 구현한다.

### Task 3: 검증

- [ ] 런타임 어셈블리를 빌드한다.
- [ ] EditMode 테스트 어셈블리를 빌드한다.
- [ ] `git diff --check`와 변경 범위를 확인한다.
- [ ] Unity 에디터 화면 검증 미수행 항목을 기록한다.

### Task 4: 아라벨라 거미줄 카메라 동기화

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleStatusEffectService.cs`
- Modify: `Assets/Tests/EditMode~/ReportedBugfixBatchTests.cs`

- [ ] 현재 카메라 이동 코루틴 완료를 기다리는 공개 API 계약 테스트를 작성한다.
- [ ] 아라벨라 거미줄 애니메이션 전에 해당 API를 기다리는 호출 순서를 고정한다.
- [ ] 런타임·Editor 어셈블리를 컴파일한다.
