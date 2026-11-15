# Arabella Camera Neutral Position Implementation Plan

> **For agentic workers:** Implement directly in the current workspace using test-first changes and fresh compile verification.

**Goal:** 아라벨라 등장 연출 종료 줌아웃과 선택 캐릭터 포커스의 기준 위치를 전투 기본 `Y = 1`로 통일한다.

**Architecture:** `BattleCameraController`의 보스 인트로 종료 전용 복원 메서드가 맵 기준 좌표 대신 공통 패널 다운 기준 좌표를 `defaultPosition`에 저장한다. 이후 기존 `FocusOnCharacterSelection()`은 변경 없이 정규화된 기준을 사용한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-15-arabella-camera-neutral-position-design.md`

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터를 직접 제어하거나 batchmode를 사용하지 않는다.
- 카메라 연출 변경은 전투 상태 판정과 분리한다.

## Review Focus

- 보스 인트로 종료 목표 Y가 정확히 `1`인지 확인한다.
- 선택 캐릭터 포커스가 같은 `defaultPosition` 기준을 이어받는지 확인한다.
- 일반 맵 복귀 `mapPosition`은 변경되지 않는지 확인한다.

### Task 1: 보스 인트로 종료 카메라 기준 정규화

**Files:**
- Modify: `Assets/Tests/EditMode~/ReportedBugfixBatchTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs`

- [ ] 보스 인트로 종료 위치 정책이 맵 위치 `(0,0,-20)`을 전투 위치 `(0,1,-20)`으로 변환하는 실패 테스트를 작성한다.
- [ ] 테스트가 현재 정책 부재로 실패하는지 확인한다.
- [ ] `ForceReturnSceneStartPositionImmediate()`가 전투 기본 위치를 `defaultPosition`과 실제 카메라에 함께 적용하도록 최소 구현한다.
- [ ] 런타임·에디터 프로젝트 컴파일과 diff 검사를 실행한다.

