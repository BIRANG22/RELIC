# Battle Reservation Tutorial Arabella Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전투 예약, 튜토리얼 입력, 각성 아라벨라 턴 복귀의 다섯 회귀 문제를 수정한다.

**Architecture:** 기존 전투 명령과 타임라인 구조를 유지하면서 입력 허용, 예약 거절 정리, 이동 병합, 코루틴 완료 보장을 각각 작은 정책 단위로 만든다. UI는 정책 결과를 소비하고 핵심 상태 변경은 기존 명령 계층에서 수행한다.

**Tech Stack:** Unity, C#, Unity Localization, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-19-battle-reservation-tutorial-arabella-fixes-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/`에만 작성한다.
- Unity batchmode 테스트를 실행하지 않는다.
- 현재 브랜치와 작업 공간을 사용하며 커밋, Push, PR, 브랜치/worktree 작업을 하지 않는다.
- 전투 상태는 안정적인 ID와 `Command -> State Change -> Result/Event` 흐름을 유지한다.

## Review Focus

- 후속 코루틴이 예외로 끝나도 완료 신호가 설정되고 턴 실행이 복구되는지 확인한다.
- 같은 슬롯이라도 다른 행동이 사이에 있으면 이동을 병합하지 않는지 확인한다.
- 용량 거절 뒤 기존 행동 취소로 자리가 생겨도 실패 명령이 살아나지 않는지 확인한다.
- 포커스 가이드가 허용한 숫자/행동 키만 통과하고 ESC는 유지되는지 확인한다.
- 기존 사용자 로컬라이제이션 변경을 덮어쓰지 않고 새 키만 추가하는지 확인한다.

---

### Task 1: 로컬라이제이션 경고 키

**Files:**
- Modify: `Assets/Project/Scripts/Core/Localization/LocalizationKeys.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Language/Text Shared Data.asset`
- Modify: `Assets/Language/Text_ko.asset`
- Modify: `Assets/Language/Text_en.asset`
- Test: `Assets/Tests/EditMode~/BattleReservationRegressionTests.cs`

**Interfaces:**
- Produces: `LocalizationKeys.Battle.SkillSlotBlocked`, `LocalizationKeys.Battle.SkillSlotLimit`

- [ ] 키와 한국어/영어 값이 존재하는 실패 테스트를 작성한다.
- [ ] 정적 테스트가 실패하는지 확인한다.
- [ ] 키 상수와 문자열 테이블 엔트리를 추가하고 호출부를 상수로 교체한다.
- [ ] 테스트와 컴파일 검사를 다시 수행한다.

### Task 2: 겹침 행동 완료 및 제한시간 보장

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Monster/ArabellaPhaseTransition.cs`
- Test: `Assets/Tests/EditMode~/BattleActionRunnerSafetyTests.cs`

**Interfaces:**
- Produces: 후속 행동 완료를 항상 알리는 열거자 래퍼와 제한시간 대기 정책

- [ ] 정상 완료, 예외 종료, 제한시간 초과 상태 전이를 검증하는 실패 테스트를 작성한다.
- [ ] 테스트 실패를 확인한다.
- [ ] 후속 코루틴 완료를 `finally`로 보장하고 대기에 `actionRoutineTimeout` 및 진단 로그를 적용한다.
- [ ] 페이즈 전환 중단 시 입력 게이트를 해제하는 안전 종료를 추가한다.
- [ ] 관련 테스트와 컴파일 검사를 수행한다.

### Task 3: 튜토리얼 키보드 입력 정책

**Files:**
- Modify: `Assets/Project/Scripts/TutorialBattleSequence.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleTurnExecutor.cs`
- Test: `Assets/Tests/EditMode~/TutorialKeyboardInputPolicyTests.cs`

**Interfaces:**
- Produces: `TutorialBattleSequence.IsBattleKeyboardInputAllowed(KeyCode key)`

- [ ] 대화, 포커스 가이드, 일반 전투 상태별 키 허용 실패 테스트를 작성한다.
- [ ] 테스트 실패를 확인한다.
- [ ] 단일 키보드 허용 정책을 구현하고 전투 키 입력 호출부에 적용한다.
- [ ] 관련 테스트와 컴파일 검사를 수행한다.

### Task 4: 같은 슬롯 연속 이동 병합

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerReservedCommand.cs`
- Test: `Assets/Tests/EditMode~/BattleReservationRegressionTests.cs`

**Interfaces:**
- Consumes: `PlayerReservedCommand.MergeMoveReservation(PlayerReservedCommand nextCommand)`
- Produces: 마지막 명령과의 연속성에 기반한 이동 병합 판정

- [ ] 연속 이동 병합과 중간 행동/명시 삽입 비병합 테스트를 작성한다.
- [ ] 테스트 실패를 확인한다.
- [ ] 마지막 실질 명령만 검사하도록 병합 판정을 구현하고 기존 병합 메서드를 재사용한다.
- [ ] 비용·경로·최종 위치·방향 검증 테스트를 수행한다.

### Task 5: 최종 거절 예약 상태 폐기

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerSkillReservationController.cs`
- Test: `Assets/Tests/EditMode~/BattleReservationRegressionTests.cs`

**Interfaces:**
- Produces: 선택 스킬, 프리뷰, pending insertion을 함께 정리하는 공통 거절 경로

- [ ] 용량 거절 후 선택/프리뷰/삽입 상태가 모두 사라지는 실패 테스트를 작성한다.
- [ ] 테스트 실패를 확인한다.
- [ ] 최종 거절 공통 정리 메서드를 구현하고 시작·확정·복수 명령 실패 경로에 적용한다.
- [ ] 기존 행동 취소 후 자동 등록되지 않음을 검증한다.

### Task 6: 통합 검증

**Files:**
- Verify: 위 변경 파일 전체

- [ ] 변경 파일 diff와 사용자 기존 변경 보존 여부를 확인한다.
- [ ] 사용 가능한 C# 프로젝트 컴파일을 실행한다.
- [ ] 테스트 파일 위치가 규칙에 맞는지 확인한다.
- [ ] Unity 에디터 화면에서만 확인 가능한 항목을 별도로 기록한다.
- [ ] 커밋, Push, PR이 수행되지 않았음을 확인한다.
