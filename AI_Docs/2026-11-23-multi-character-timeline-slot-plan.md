# Multi-Character Timeline Slot Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 하나의 전투 타임라인 슬롯에 서로 다른 캐릭터의 명령을 예약·삽입·이동·복원할 수 있게 한다.

**Architecture:** `ReserveTurnSlotUI`의 캐릭터 독점 정책만 제거하고 슬롯 용량 및 명령 리스트 구조는 유지한다. `BattleTimelineController`의 모든 진입 경로가 동일한 용량 규칙을 사용하며, `BattleActionBatchBuilder`는 캐릭터별 예약 순서와 이동-효과 공간 충돌을 보존한다. 호스트는 슬롯 내 명령 전체가 요청자 제어 범위인지 검증한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-23-multi-character-timeline-slot-design.md`

## Global Constraints

- 문서는 `AI_Docs` 안에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- 슬롯 최대 행동 용량 5와 멀티플레이 타 사용자 편집 잠금은 유지한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR, 브랜치 또는 worktree 작업은 수행하지 않는다.

## Review Focus

- 서로 다른 캐릭터의 일반 명령 추가가 성공해야 한다.
- 서로 다른 캐릭터 명령 삽입과 슬롯 간 이동이 원래 순서를 보존해야 한다.
- 스냅샷 복원에서 서로 다른 `CharacterId`가 손실되지 않아야 한다.
- 다중 캐릭터를 허용해도 `TimelineEntryCost` 용량 초과는 거부되어야 한다.
- 동일 캐릭터 전용 이동 병합과 캐릭터별 비용 계산이 다른 캐릭터 명령을 섞지 않아야 한다.

---

### Task 1: 슬롯 다중 캐릭터 수락 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`
- Create: `Assets/Tests/EditMode~/BattleActionBatchBuilderMultiCharacterTests.cs`

**Interfaces:**
- Consumes: `BattleTimelineController.ConfirmPlayerCommandFromNetwork`, `TryMovePlayerReservation`, `AddPlayerCommandFromNetworkSnapshot`
- Produces: 동일 슬롯 다중 캐릭터 동작을 고정하는 NUnit 테스트

- [ ] **Step 1: 실패 테스트 작성**
  - 서로 다른 캐릭터의 명령 두 개를 동일 슬롯에 추가한다.
  - 다른 캐릭터 명령이 있는 슬롯으로 명령을 이동한다.
  - 네트워크 스냅샷 경로로 서로 다른 캐릭터 명령을 복원한다.
  - 다중 캐릭터 상태에서도 용량 초과는 거부되는지 확인한다.
  - 다중 캐릭터 실행 배치가 캐릭터별 순서와 이동-효과 충돌을 보존하는지 확인한다.
  - 호스트 슬롯 정책이 동일 소유자의 여러 캐릭터는 허용하고 타 사용자 명령 혼합은 거부하는지 확인한다.

- [ ] **Step 2: 컴파일로 테스트 소스 검증**
  - `Assembly-CSharp-Editor.csproj`를 빌드한다.
  - 예상 결과: 새 테스트가 컴파일되고 구현 전 동작 기준으로 다중 캐릭터 수락 assertion은 실패 대상이다.

### Task 2: 캐릭터 독점 정책 제거

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/ReserveTurnSlotUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleActionBatchBuilder.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/SteamBattleStateSynchronizer.cs`

**Interfaces:**
- Consumes: `PlayerReservedCommand`, `TimelineEntryCost`, 기존 슬롯 잠금 및 용량 검사
- Produces: 캐릭터 ID와 무관하게 명령을 수락하는 추가·삽입·이동·스냅샷 복원 경로

- [ ] **Step 1: `ReserveTurnSlotUI`의 독점 검사 제거**
  - `AddCommand`와 `InsertCommand`에서 `CanAcceptCharacter` 의존성을 제거한다.
  - 사용되지 않는 `ReservedCharacter` 및 `CanAcceptCharacter`를 제거한다.

- [ ] **Step 2: `BattleTimelineController`의 중복 독점 검사 제거**
  - 일반 확정, 스냅샷 복원, 슬롯 간 이동에서 `CanAcceptCharacter` 조건만 제거한다.
  - 슬롯 잠금, 장비 제한, 용량, 비용 검사는 유지한다.

- [ ] **Step 3: 전체 참조 검사**
  - `rg "CanAcceptCharacter|ReservedCharacter|SlotOccupiedByOtherCharacter"`로 제거된 정책의 잔여 런타임 참조를 확인한다.

- [ ] **Step 4: 실행 순서와 멀티플레이 경계 보존**
  - 같은 캐릭터의 후속 명령이 앞선 명령보다 이른 배치에 들어가지 않게 한다.
  - 이동 목적지와 다른 캐릭터 효과 대상의 교차 충돌을 분리한다.
  - 호스트 예약·이동·삭제 경로에서 슬롯 명령 소유권을 검사한다.

### Task 3: 검증

**Files:**
- Verify: `Assembly-CSharp.csproj`
- Verify: `Assembly-CSharp-Editor.csproj`

**Interfaces:**
- Consumes: Task 1 테스트와 Task 2 구현
- Produces: 컴파일 및 정적 검증 결과

- [ ] **Step 1: 런타임 어셈블리 빌드**
  - `MSBuild Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal /nologo`
  - 예상 결과: 오류 0.

- [ ] **Step 2: 에디터 테스트 어셈블리 빌드**
  - `MSBuild Assembly-CSharp-Editor.csproj /t:Build /p:RestorePackages=false /v:minimal /nologo`
  - 예상 결과: 오류 0.

- [ ] **Step 3: 변경 범위 및 멀티플레이 경계 확인**
  - diff에서 네트워크 편집 잠금과 슬롯 용량 코드가 변경되지 않았는지 확인한다.

- [ ] **Step 4: 커밋 생략**
  - 사용자 승인 없는 커밋, Push, PR은 수행하지 않는다.
