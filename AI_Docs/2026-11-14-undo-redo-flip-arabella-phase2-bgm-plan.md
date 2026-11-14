# Undo/Redo 피드백·플립 무결성·아라벨라 2페이즈 BGM Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Undo/Redo 입력 피드백과 가용 상태를 일치시키고, 고립된 플립을 자동 정리하며, 아라벨라 2페이즈 전용 BGM 상태를 추가한다.

**Architecture:** 예약 상태는 `BattleTimelineController`가 소유하고 UI는 공개 상태와 결과 이벤트만 표현한다. 아라벨라 전환은 이벤트로 BGM 디렉터에 전달하며, 미설정 상태는 기존 방 BGM으로 폴백한다.

**Tech Stack:** Unity 6, C#, uGUI, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-14-undo-redo-flip-arabella-phase2-bgm-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs`에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/`에만 둔다.
- Unity 에디터 직접 제어와 batchmode 테스트를 하지 않는다.
- 전투 상태 변경과 UI/BGM 표현을 분리한다.
- 커밋, Push, PR, 브랜치/worktree 작업을 하지 않는다.

## Review Focus

- 무효 히스토리 항목만 남은 경우 버튼이 활성화되지 않아야 한다.
- 실패한 Redo는 상태와 피드백을 성공으로 보고하지 않아야 한다.
- 후속 행동에 병합된 플립은 슬롯 변경이나 다른 편집에서 제거되지 않아야 한다.
- 네트워크 적용 경로의 이동·취소도 로컬과 동일한 플립 정리 결과를 내야 한다.
- 아라벨라 2페이즈 BGM 클립 미연결 시 재생 중인 보스 BGM이 중단되지 않아야 한다.

---

### Task 1: Undo/Redo 상태 및 공통 피드백

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/BattleCharacterPanelUI.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

**Interfaces:**
- Produces: `CanUndoPlayerReservation`, `CanRedoPlayerReservation`, 히스토리 상태/실행 결과 이벤트

- [ ] 히스토리 없음·무효 항목·성공/실패 실행을 고정하는 실패 테스트를 추가한다.
- [ ] 테스트 컴파일 실패 또는 어설션 실패를 확인한다.
- [ ] 컨트롤러 공개 상태와 이벤트를 구현하고 모든 히스토리 변경 지점에서 통지한다.
- [ ] 패널이 버튼 상호작용·어두운 그래픽·공통 클릭 피드백을 이벤트로 갱신하게 한다.
- [ ] 관련 테스트와 프로젝트 컴파일을 실행한다.

### Task 2: 미병합 플립 자동 정리

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Test: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

**Interfaces:**
- Consumes: 기존 `IsFacingFlipCommand`, `FlipBeforeAction`, 예약 이동/삭제/슬롯 선택 API
- Produces: 편집 경계에서 미병합 플립만 정리하는 내부 정책

- [ ] 슬롯 변경, 예약 이동·재정렬, 직접 취소와 병합 플립 보존 테스트를 추가한다.
- [ ] 기존 구현에서 테스트가 실패함을 확인한다.
- [ ] 활성 슬롯 이탈과 성공한 이동/삭제 경계에 최소 정리 로직을 추가한다.
- [ ] 제거된 플립의 히스토리/비용/프리뷰 갱신을 일관되게 처리한다.
- [ ] 관련 테스트와 프로젝트 컴파일을 실행한다.

### Task 3: 아라벨라 2페이즈 BGM 상태

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/SoundDatabase.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Monster/ArabellaPhaseTransition.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleBgmDirector.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/ContextualBgmTests.cs`
- Test: `Assets/Tests/EditMode~/ArabellaPhaseTransitionTests.cs`

**Interfaces:**
- Produces: `BgmState.ArabellaPhase2`, 2페이즈 실제 적용 이벤트, 전용 BGM 후보 우선순위

- [ ] 상태 값, 전환 이벤트, 우선순위 및 미설정 폴백 테스트를 추가한다.
- [ ] 기존 구현에서 테스트가 실패함을 확인한다.
- [ ] enum과 전환 이벤트를 추가하고 실제 2페이즈 적용 시점에 발행한다.
- [ ] BGM 디렉터 구독·후보 우선순위와 씬 직렬화 기본값을 추가한다.
- [ ] 관련 테스트와 두 C# 프로젝트 컴파일을 실행한다.

### Task 4: 통합 검증

**Files:**
- Verify all files above

- [ ] 변경 diff와 YAML 직렬화 값을 검토한다.
- [ ] `Assembly-CSharp.csproj` 빌드를 실행해 오류 0개를 확인한다.
- [ ] `Assembly-CSharp-Editor.csproj` 빌드를 실행해 오류 0개를 확인한다.
- [ ] 에디터 화면/실제 음원처럼 자동 검증하지 못한 항목을 완료 보고에 기록한다.
