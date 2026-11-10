# Time-Aware Player Move Path Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 클릭 이동이 먼저 예약된 아군의 시점별 이동과 충돌하지 않는 최단 우회 경로를 자동 선택하게 한다.

**Architecture:** 순수 시간 확장 BFS 유틸리티가 정적 장애물과 시점별 아군 이동 전이를 입력받아 경로를 계산한다. `PlayerSkillReservationController`는 현재 슬롯에서 새 행동보다 앞선 플레이어 이동 명령을 예약표로 변환하고, 클릭 이동 후보 계산에만 새 유틸리티를 사용한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `Docs/superpowers/specs/2026-11-11-time-aware-player-move-path-design.md`

## Global Constraints

- 기존 벽·몬스터·그리드 효과·이동 비용 검사를 유지한다.
- 동일 시점 동일 칸 점유와 반대 방향 간선 교차를 금지한다.
- 앞 캐릭터가 빠져나간 칸은 이후 시점에 허용한다.
- 직접 드래그한 경로는 변경하지 않는다.
- 대기 행동은 생성하지 않는다.

## Review Focus

- 출발 시점부터 아군이 점유한 칸: 첫 스텝 충돌 없이 우회해야 한다.
- 아군이 직전에 비운 칸: 다음 시점에 뒤따라 진입할 수 있어야 한다.
- A→B와 B→A 교차: 동일 시점 자리 교환을 금지해야 한다.
- 우회 경로가 최대 이동 스텝을 넘는 경우: 경로 없음으로 반환해야 한다.
- 시간 예약표가 없는 경우: 기존 최단 경로와 동일해야 한다.

---

### Task 1: 시간 확장 최단 경로 유틸리티

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/TimeAwareMovePathUtility.cs`
- Create: `Assets/Tests/EditMode~/TimeAwareMovePathUtilityTests.cs`

**Interfaces:**
- Consumes: 그리드 크기, 시작/목적 좌표, 최대 스텝, 정적 차단 좌표, `TimedMoveTransition` 목록
- Produces: `TimeAwareMovePathUtility.FindShortestPath(...) -> List<Vector2Int>` 및 `TimedMoveTransition(time, from, to)`

- [ ] **Step 1: 실패 테스트 작성**
  - `FollowingCharacter_CanEnterCellAfterLeaderLeaves`
  - `SameTimeVertexCollision_UsesShortestDetour`
  - `OppositeEdgeSwap_UsesDetour`
  - `DetourBeyondMaxSteps_ReturnsNull`
  - `NoTimedReservations_ReturnsManhattanShortestPath`

- [ ] **Step 2: 테스트가 타입 부재 또는 기대 경로 불일치로 실패하는지 확인**

Run: Unity EditMode test `TimeAwareMovePathUtilityTests`

- [ ] **Step 3: 최소 시간 확장 BFS 구현**

`FindShortestPath`는 대기 노드 없이 상하좌우 이동만 탐색하고 `(coord, time)`을 방문 상태로 사용한다. 다음 시점 점유 충돌과 같은 시점 반대 방향 전이를 제외한다.

- [ ] **Step 4: 유틸리티 테스트 통과 확인**

### Task 2: 예약 타임라인 통합

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerSkillReservationController.cs`
- Modify: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

**Interfaces:**
- Consumes: Task 1의 `TimedMoveTransition`, `FindShortestPath`
- Produces: 현재 슬롯의 선행 플레이어 이동 명령을 시간 예약표로 변환하는 내부 빌더와 클릭 이동 경로 선택

- [ ] **Step 1: 실패 통합 테스트 작성**
  - 선행 아군 경로가 직선 최단 경로와 충돌할 때 우회 경로 선택
  - 선행 아군이 비운 칸을 한 시점 뒤 따라가는 경로 허용
  - 명시적 드래그 경로는 자동 우회 로직을 호출하지 않고 그대로 유지

- [ ] **Step 2: 기존 경로 선택으로 테스트가 실패하는지 확인**

Run: Unity EditMode tests `BattleTimelineManualControlTests`

- [ ] **Step 3: 선행 명령 예약표 구성 및 클릭 경로에 연결**

현재 슬롯에서 새 명령보다 먼저 실행되는 다른 캐릭터의 `MoveSteps`를 시작 위치부터 재생한다. 클릭으로 `explicitMoveOffsets == null`인 경우에만 시간 확장 BFS를 사용한다.

- [ ] **Step 4: 관련 테스트와 전체 EditMode 테스트 통과 확인**

- [ ] **Step 5: `git diff --check` 및 Unity C# 재컴파일 성공 확인**
