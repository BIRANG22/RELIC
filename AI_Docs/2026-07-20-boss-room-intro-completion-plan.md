# 보스방 등장 연출 완료 신호 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `St1_boss/appear`의 등장 애니메이션 시퀀스가 끝난 뒤에만 기존 배틀룸 로드와 유닛 스폰을 시작한다.

**Architecture:** 등장 연출은 공통 완료 계약 `IBattleRoomIntroSequence`를 구현한다. `BattleSceneController`는 현재 배틀룸 배경에서 이 계약을 탐색하고, 완료 전이면 이벤트를 기다렸다가 `BattleRoomLoader`를 호출하며 계약이 없거나 이미 완료된 일반방은 즉시 기존 흐름을 실행한다.

**Tech Stack:** Unity 6, C#, Animator, Unity Test Framework EditMode

## Global Constraints

- 전투 핵심 상태 변경과 등장 연출을 분리한다.
- 전투 유닛은 등장 연출 완료 후 기존 `BattleRoomLoader`에서 스폰한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- 사용자 변경이 있는 `St1_boss.prefab`은 수정하지 않는다.
- 커밋과 PR은 생성하지 않는다.

---

### Task 1: 등장 연출 완료 계약

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Intro/IBattleRoomIntroSequence.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Intro/BattleRoomIntroSequence.cs`
- Test: `Assets/Tests/EditMode~/BattleRoomIntroSequenceTests.cs`

**Interfaces:**
- Produces: `bool IsCompleted`, `event Action Completed`, 파생 클래스용 `MarkCompleted()`

- [ ] 완료 이벤트가 한 번만 발생하고 완료 상태가 유지되는 실패 테스트를 작성한다.
- [ ] Unity 테스트 실행이 불가능한 현재 환경에서는 구현 전 MSBuild 실패로 RED를 확인한다.
- [ ] 공통 인터페이스와 중복 완료 방지 기반 클래스를 최소 구현한다.
- [ ] MSBuild로 GREEN을 확인한다.

### Task 2: `AnimationSequence` 완료 통보

**Files:**
- Modify: `Assets/Project/PrefabsR/Monster/Boss/Arabella/SequenceAnimator.cs`

**Interfaces:**
- Consumes: `BattleRoomIntroSequence.MarkCompleted()`
- Produces: `state3` 재생 종료 후 완료 이벤트

- [ ] `AnimationSequence`가 공통 기반 클래스를 상속하도록 변경한다.
- [ ] 전체 시퀀스 마지막에서 `MarkCompleted()`를 호출한다.
- [ ] 비활성화 후 재활성화 시 완료 상태와 시퀀스를 다시 시작할 수 있게 초기화한다.

### Task 3: 배틀룸 로드 대기 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Test: `Assets/Tests/EditMode~/BattleRoomIntroSequenceTests.cs`

**Interfaces:**
- Consumes: 배틀룸 자식의 `IBattleRoomIntroSequence`
- Produces: 완료 전 로드 보류, 완료·계약 없음 시 `LoadBattleFromSceneController()` 호출

- [ ] 활성·비활성 자식에서 인터페이스 구현체를 탐색하는 실패 테스트를 작성한다.
- [ ] 탐색 유틸리티를 구현한다.
- [ ] `BattleSceneController`가 대기 중 이벤트를 안전하게 구독·해제하고 중복 로드를 막도록 구현한다.
- [ ] MSBuild와 정적 검색으로 연결을 검증한다.

### Task 4: 최종 검증

**Files:**
- Verify: 위 변경 파일 전체

- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] `git diff --check`와 변경 범위 검토를 수행한다.
- [ ] Unity 에디터에서 확인할 수동 테스트 절차를 정리한다.

### Task 5: 등장 연출 후 인스펙터 지연

**Files:**
- Modify: `Assets/Project/PrefabsR/Monster/Boss/Arabella/SequenceAnimator.cs`
- Test: `Assets/Tests/EditMode~/BattleRoomIntroSequenceTests.cs`

**Interfaces:**
- Produces: 인스펙터에서 변경 가능한 `postSequenceDelay`와 읽기 전용 `PostSequenceDelay`

- [ ] 기본 지연이 1초인지 확인하는 실패 테스트를 작성한다.
- [ ] 마지막 애니메이션 종료 후 설정된 지연을 기다린 다음 완료 신호를 보내도록 구현한다.
- [ ] 런타임 및 EditMode 테스트 어셈블리 컴파일을 검증한다.
