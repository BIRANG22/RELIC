# Drag Move Path Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 클릭 최단 경로 이동을 유지하면서 드래그한 인접 칸 순서대로 이동 경로를 예약한다.

**Architecture:** `MoveDragPathState`가 좌표 경로의 추가와 접미 취소를 순수 상태로 처리한다. `GridCell`과 `GridManager`는 입력 이벤트만 전달하고, `PlayerSkillReservationController`가 기존 최단 경로로 세션을 시작해 프리뷰와 기존 예약 명령 생성기로 확정한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

**Spec:** `AI_Docs/2026-10-30-drag-move-path-selection-design.md`

## Global Constraints

- 기존 클릭 이동 결과를 변경하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- 예약 결과는 기존 ID·좌표 기반 `PlayerReservedCommand`를 사용한다.

## Review Focus

- 시작 칸으로 돌아왔을 때 빈 이동이 아니라 기존 제자리 방향 전환 규칙과 충돌하지 않는가.
- 빠른 포인터 이동으로 비인접 칸이 들어오면 경로가 순간이동하지 않는가.
- 경로 중간 칸으로 돌아가면 이후 경로만 제거되는가.
- 점유·장애물·비용 초과 칸이 기존 경로를 손상하지 않는가.
- 클릭과 드래그가 이중 확정되지 않는가.

---

### Task 1: 드래그 경로 상태

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Runtime/Battle/MoveDragPathState.cs`
- Test: `Assets/Tests/EditMode~/MoveDragPathStateTests.cs`

- [ ] 경로 추가, 비인접 거부, 접미 제거 테스트를 먼저 작성한다.
- [ ] 테스트의 예상 실패를 확인한다.
- [ ] 최소 상태 구현을 추가한다.
- [ ] 테스트 어셈블리 컴파일을 확인한다.

### Task 2: 그리드 입력과 이동 예약 통합

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Grid/GridCell.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Runtime/Battle/GridManager.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerSkillReservationController.cs`

- [ ] 포인터 다운·업 이벤트를 그리드 입력 경계에 추가한다.
- [ ] 이동 선택 중 초기 최단 경로로 드래그 세션을 시작한다.
- [ ] 호버 시 유효 인접 경로 추가 또는 이전 칸 복귀를 처리한다.
- [ ] 포인터 업 시 기존 예약 생성 경로로 한 번만 확정한다.
- [ ] 클릭·드래그 프리뷰와 취소 정리를 통합한다.
- [ ] 런타임 및 EditMode 테스트 어셈블리 컴파일과 diff를 검증한다.

