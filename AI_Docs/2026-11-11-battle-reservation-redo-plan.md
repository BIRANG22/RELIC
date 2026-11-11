# Battle Reservation Redo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 예약 추가와 슬롯 이동을 모두 단계별 Undo/Redo하고, 이동 Undo는 명령 삭제 대신 직전 슬롯·순서로 복원한다.

**Architecture:** 히스토리 엔트리를 예약 추가와 예약 이동으로 구분한다. 이동 엔트리는 출발/도착 슬롯과 순서를 모두 보유하며 Undo/Redo는 기존 트랜잭션 이동 검증을 사용해 반대 방향으로 적용한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-11-battle-reservation-redo-design.md`

## Global Constraints

- 문서와 계획은 `AI_Docs`에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/`에만 추가한다.
- Unity batchmode는 실행하지 않는다.
- 전투 상태 변경은 UI 입력 처리와 분리한다.
- 커밋, Push, PR은 수행하지 않는다.

## Review Focus

- 여러 슬롯에서 연속 Undo한 뒤 Redo 순서와 삽입 위치가 유지되는가.
- Undo 뒤 새 예약을 만들면 오래된 Redo 분기가 폐기되는가.
- 직접 삭제·전체 초기화가 Redo 기록을 남기지 않고, 정상 이동은 독립 Undo 작업으로 기록되는가.
- 여러 슬롯 이동과 같은 슬롯 순서 변경이 단계별로 왕복되는가.
- 슬롯 용량이나 캐릭터 점유가 달라졌을 때 Redo가 부분 상태를 만들지 않는가.
- Alt+우클릭이 기존 몬스터 정보 닫기와 선택 취소 우선순위를 깨지 않는가.

---

### Task 1: Redo 상태 규칙 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`

**Interfaces:**
- Consumes: `UndoLastPlayerReservation()`, `ConfirmPlayerCommand(...)`, 테스트용 슬롯 구성
- Produces: `RedoLastPlayerReservation(): bool`의 기대 동작을 고정하는 테스트

- [ ] **Step 1: 실패 테스트 작성**
  - Undo 후 Redo가 같은 명령을 같은 순서에 복원
  - 연속 Undo/Redo의 LIFO 동작
  - Undo 후 새 예약 시 Redo 무효화
  - 복원 대상 슬롯이 유효하지 않을 때 무변경 실패

- [ ] **Step 2: 컴파일로 실패 확인**
  - Run: `MSBuild.exe Assembly-CSharp-Editor.csproj /t:Build /p:RestorePackages=false /v:minimal`
  - Expected: `RedoLastPlayerReservation` 미정의로 실패

### Task 2: Redo 스택과 복원 구현

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`

**Interfaces:**
- Consumes: `PlayerReservationHistoryEntry`, `ReserveTurnSlotUI.InsertCommand`, 예약 검증 및 갱신 메서드
- Produces: `public bool RedoLastPlayerReservation()` 및 Redo 기록 무효화 규칙

- [ ] **Step 1: Redo 히스토리와 Undo 캡처 구현**
  - 제거 전 명령 순서를 `PlayerReservationHistoryEntry`에 저장하고 Undo 성공 시 Redo 스택에 추가한다.

- [ ] **Step 2: Redo 복원 구현**
  - 입력 잠금과 슬롯/캐릭터/장비/비용/용량을 검증하고 원래 인덱스에 삽입한다.
  - 성공 시 Undo 히스토리에 기록하고 파생 상태를 전체 갱신한다.

- [ ] **Step 3: 분기 무효화 구현**
  - 신규 예약, 이동, 직접 삭제, 전체 초기화에서 Redo 스택을 지운다.
  - Redo 내부 복원과 네트워크 스냅샷 재구성은 의도하지 않은 분기 삭제를 피한다.

- [ ] **Step 4: Alt+우클릭 입력 연결**
  - 기존 우클릭 우선순위와 입력 잠금 검사를 유지한 채 Alt 키가 눌린 경우 Redo를 호출한다.

- [ ] **Step 5: 컴파일 및 관련 테스트 검증**
  - Run: `MSBuild.exe Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal`
  - Run: `MSBuild.exe Assembly-CSharp-Editor.csproj /t:Build /p:RestorePackages=false /v:minimal`
  - Expected: 오류 0개
  - Unity 에디터의 관련 EditMode 테스트 결과는 사용 가능한 기존 결과 파일 또는 프로젝트 테스트 도구로 확인한다.

### Task 3: 최종 회귀 점검

**Files:**
- Review: 위 코드와 테스트

**Interfaces:**
- Consumes: Task 1~2 결과
- Produces: 요구사항 대비 검증 결과

- [ ] **Step 1: 변경 diff 검토**
  - Undo/Redo 외 예약 경로의 의도치 않은 상태 변경이 없는지 확인한다.

- [ ] **Step 2: 정적 검색과 테스트 결과 확인**
  - 모든 예약 제거 경로의 Redo 무효화 여부와 모든 초기화 경로의 스택 정리 여부를 확인한다.

- [ ] **Step 3: 완료 보고**
  - 변경 파일, 구현 내용, 검증 결과, 미검증 항목, 멀티플레이 영향, Git 외부 작업 미수행 여부만 보고한다.

### Task 4: 예약 이동 Undo/Redo

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleTimelineManualControlTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs`

**Interfaces:**
- Consumes: `TryMovePlayerReservationInternal(...)`, `PlayerReservationHistoryEntry`, Undo/Redo 스택
- Produces: 이동 전·후 위치를 왕복하는 Undo/Redo 작업 기록

- [ ] **Step 1: 실패 테스트 작성**
  - 다른 슬롯 이동 직후 Undo가 명령을 삭제하지 않고 원래 슬롯·순서로 복원
  - 이동 Undo 직후 Redo가 다시 목적 슬롯·순서로 복원
  - 같은 슬롯 순서 변경과 연속 이동을 단계별로 Undo/Redo
  - 복원 목적지가 다른 캐릭터로 점유되면 무변경 실패

- [ ] **Step 2: 히스토리 작업 종류와 이동 위치 기록 구현**
  - 예약 추가와 이동 엔트리를 구분하고 이동 성공 직전에 캡처한 출발/도착 위치를 기록한다.

- [ ] **Step 3: 트랜잭션 이동 복원 구현**
  - Undo/Redo 내부 이동은 새 히스토리를 만들거나 Redo 스택을 지우지 않는다.
  - 기존 이동 검증·전체 비용 재계산·실패 롤백을 재사용한다.

- [ ] **Step 4: 컴파일 및 회귀 검증**
  - 런타임과 변경 테스트를 포함한 에디터 어셈블리를 빌드하고 오류 0개를 확인한다.
