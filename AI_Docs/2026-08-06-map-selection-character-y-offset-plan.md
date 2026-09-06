# Map Selection Character Y Offset Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development for the behavior change. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 모든 룸의 공통 맵 선택 캐릭터 위치를 Y축으로 1 높인다.

**Architecture:** 공통 `BattleRoomMapSelectionPresenter`의 기본 위치와 공개 위치 계산 결과를 함께 `-0.25`로 변경한다. 기존 EditMode 테스트에서 최종 좌표를 검증한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 전투 상태를 변경하지 않는다.

---

### Task 1: 공통 맵 선택 Y 위치 변경

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleRoomMapSelectionPresenterTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoomMapSelectionPresenter.cs`

**Interfaces:**
- Consumes: 캐릭터 순번
- Produces: `(-5.5 + index * 2.1, -0.25, -2)` 위치

- [ ] **Step 1: 실패 테스트 작성**
  - 첫째·둘째·셋째 캐릭터의 Y가 모두 `-0.25`인지 검증한다.
- [ ] **Step 2: RED 확인**
  - 기존 구현의 `-1.25` 결과 때문에 실패하는지 확인한다.
- [ ] **Step 3: 최소 구현**
  - 직렬화 기본 위치와 `CalculateCharacterPosition`의 Y를 `-0.25`로 변경한다.
- [ ] **Step 4: 검증**
  - 런타임·에디터 어셈블리를 빌드하고 가능한 테스트 결과를 확인한다.
