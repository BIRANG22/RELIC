# Contextual Room BGM Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 방별 기본 BGM, 전투 상황 우선순위 전환, 미연결 상태 fallback, BGM 구간 루프를 구현한다.

**Architecture:** `BattleBgmDirector`가 방/상황에서 재생할 상태를 결정하고 `AudioManager`가 설정된 상태만 재생한다. 구간 루프는 각 BGM 레이어의 sample 위치를 `BgmClipData` 설정에 따라 되감는다.

**Tech Stack:** Unity 6, C#, AudioSource, NUnit EditMode tests, Unity YAML

**Spec:** `AI_Docs/2026-11-13-contextual-room-bgm-design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode와 에디터 직접 제어를 사용하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.
- BGM은 전투 결과를 읽기만 하고 전투 상태를 변경하지 않는다.

## Review Focus

- 미연결 고우선 상태가 재생 중인 저우선 BGM을 중지하지 않아야 한다.
- 같은 상태 재평가가 트랙을 처음부터 재시작하지 않아야 한다.
- 회복 후 저체력 상태에서 방/보스 상태로 복귀해야 한다.
- 구간 끝을 여러 sample 초과해도 반복 위치를 정확히 보존해야 한다.
- 씬 재진입과 이벤트 구독 해제에서 중복 콜백이 없어야 한다.

---

### Task 1: BGM 데이터와 구간 루프

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/SoundDatabase.cs`
- Modify: `Assets/Project/Scripts/Core/Managers/AudioManager.cs`
- Test: `Assets/Tests/EditMode~/ContextualBgmTests.cs`

- [ ] BGM 구성 조회와 loop sample 계산 테스트를 작성한다.
- [ ] `IsBgmConfigured`와 구간 루프 데이터를 구현한다.
- [ ] 메인/레이어 AudioSource의 구간 루프를 갱신한다.

### Task 2: 상황 우선순위 디렉터

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleBgmDirector.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/SoundDatabase.cs`
- Test: `Assets/Tests/EditMode~/ContextualBgmTests.cs`

- [ ] 미연결 fallback, 우선순위, 저체력 판정 테스트를 작성한다.
- [ ] 방 매핑과 조건 상태 선택을 순수 정책으로 구현한다.
- [ ] 기존 전투 결과 이벤트를 구독하는 프레젠테이션 컴포넌트를 구현한다.

### Task 3: 방·보스·패배 이벤트 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleResultChecker.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/ContextualBgmTests.cs`

- [ ] 방 진입과 패배 이벤트 연결 테스트를 작성한다.
- [ ] 현재 노드 타입을 디렉터에 전달하고 패배 이벤트를 발행한다.
- [ ] Battle 씬에 디렉터와 기본 방 매핑을 명시적으로 배치한다.

### Task 4: 검증

- [ ] 런타임/에디터 C# 프로젝트를 빌드한다.
- [ ] SoundDatabase 미연결 상태 fallback과 씬 YAML을 정적 검사한다.
- [ ] 전체 diff를 검토한다.
