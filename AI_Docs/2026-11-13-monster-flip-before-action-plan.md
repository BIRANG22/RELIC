# Monster Flip Before Action Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 몬스터가 이동 후 반대 방향을 공격할 때 플립을 다음 행동에 병합해 올바른 방향으로 실행하고 타임라인에 표시한다.

**Architecture:** `MonsterReservedCommand.FlipBeforeAction`을 단일 진실 공급원으로 두고 계획, 시뮬레이션, 실행, UI, 저장 및 네트워크가 공유한다. 플립은 독립 엔트리가 아니므로 기존 행동의 타임라인 비용과 아이콘을 유지한다.

**Tech Stack:** Unity 6, C#, TextMeshPro/UI, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-13-monster-flip-before-action-design.md`

## Global Constraints

- 전투 핵심 로직은 `Command -> State Change -> Result/Event` 흐름을 유지한다.
- 상태 식별은 `RuntimeId` 기반 기존 명령 구조를 유지한다.
- UI는 방향을 계산하지 않고 명령의 `FlipBeforeAction`만 표시한다.
- 문서와 테스트는 각각 `AI_Docs`, `Assets/Tests/EditMode~` 아래에만 작성한다.
- Unity 에디터 직접 제어 및 batchmode 테스트를 실행하지 않는다.

## Review Focus

- 수평 이동이 막혔을 때 실행 방향이 예약과 달라질 수 있는 경우
- 같은 슬롯 안에서 여러 몬스터 행동이 이어지는 경우
- 강제 방향이 없는 대칭/범위 행동이 불필요하게 플립되는 경우
- 저장 복원 또는 네트워크 재구성 후 플립이 유실되는 경우
- 사망한 몬스터 명령이 플립 상태만 변경하는 경우

---

### Task 1: 몬스터 명령 플립 상태와 계획 정책

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/MonsterReservedCommand.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleMonsterTurnPlanner.cs`
- Test: `Assets/Tests/EditMode~/MonsterFlipBeforeActionTests.cs`

**Interfaces:**
- Produces: `MonsterReservedCommand.FlipBeforeAction`, `SetFlipBeforeAction(bool)`

- [ ] **Step 1: 반대 방향 행동만 플립되는 실패 테스트 작성**
- [ ] **Step 2: 테스트가 누락된 API/정책으로 실패하는지 확인**
- [ ] **Step 3: 명령 상태와 예상 방향 기반 병합 정책 구현**
- [ ] **Step 4: 관련 테스트 컴파일 및 통과 조건 확인**

### Task 2: 시뮬레이션과 실행 상태 변경

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionSimulationService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs`
- Test: `Assets/Tests/EditMode~/MonsterFlipBeforeActionTests.cs`

**Interfaces:**
- Consumes: `MonsterReservedCommand.FlipBeforeAction`
- Produces: 실행 및 시뮬레이션에서 동일한 몬스터 방향 전환

- [ ] **Step 1: 플립 적용 순서와 사망 명령 무시 테스트 작성**
- [ ] **Step 2: 실패 확인 후 실행·시뮬레이션에 최소 구현**
- [ ] **Step 3: 이동 및 공격 방향 회귀 검사**

### Task 3: 타임라인 표시와 스냅샷 보존

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineGroupUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/BattleNetworkModels.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/SteamBattleStateSynchronizer.cs`
- Modify: 몬스터 명령 저장/복원 모델 및 변환 지점
- Test: `Assets/Tests/EditMode~/MonsterFlipBeforeActionTests.cs`

**Interfaces:**
- Consumes: `MonsterReservedCommand.FlipBeforeAction`
- Produces: 플레이어와 동일한 병합 하이라이트 및 직렬화 왕복 보존

- [ ] **Step 1: UI 정책과 스냅샷 왕복 실패 테스트 작성**
- [ ] **Step 2: 몬스터 하이라이트 조건과 DTO 필드 구현**
- [ ] **Step 3: 저장/복원 및 네트워크 변환 구현**
- [ ] **Step 4: 전체 컴파일, 정적 회귀 검사, diff 검사**

### Task 4: AI 계획 단계 상태 변경 제거

**Files:**
- Modify: 방향을 직접 변경하는 몬스터 AI 파일들
- Test: `Assets/Tests/EditMode~/MonsterFlipBeforeActionTests.cs`

**Interfaces:**
- Produces: 계획 생성이 런타임 방향 상태를 직접 변경하지 않는 구조

- [ ] **Step 1: 직접 상태 변경 경로를 검출하는 실패 검사 작성**
- [ ] **Step 2: AI가 강제 방향 데이터만 반환하도록 변경**
- [ ] **Step 3: 컴파일 및 전체 방향 정책 회귀 검사**
