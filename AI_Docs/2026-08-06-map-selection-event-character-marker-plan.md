# Map Selection Event Character Marker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development for this fix. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 스타트룸과 휴식룸의 이벤트 월드 캐릭터도 공통 맵 선택 위치로 이동시킨다.

**Architecture:** 생성된 파티 캐릭터 루트에 전용 마커를 추가하고 프레젠터 자동 수집 범위를 마커까지 확장한다. NPC는 마커가 없으므로 제외된다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- UI 연출은 전투 상태를 변경하지 않는다.

---

### Task 1: 마커 캐릭터 자동 수집

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoomMapSelectionPresenter.cs`
- Test: `Assets/Tests/EditMode~/BattleRoomMapSelectionPresenterTests.cs`

**Interfaces:**
- Consumes: 활성 룸 아래 `BattleCharacter` 또는 `BattleMapSelectionCharacterMarker`
- Produces: 중복 없는 플레이어 Transform 목록

- [ ] **Step 1: 실패 테스트 작성**
  - BattleCharacter 없이 마커만 있는 캐릭터가 `(-5.5, -0.25, -2)`로 이동하는지 검증한다.
- [ ] **Step 2: 최소 구현**
  - 프레젠터 파일에 빈 마커 컴포넌트를 추가하고 마커 Transform을 수집하도록 한다.
- [ ] **Step 3: 검증**
  - 테스트 어셈블리 컴파일과 위치 결과를 확인한다.

### Task 2: 스타트·휴식룸 스폰 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/StartRoom/StartRoomController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomController.cs`

**Interfaces:**
- Consumes: 생성된 파티 캐릭터 루트
- Produces: 마커가 부착된 이벤트 월드 캐릭터

- [ ] **Step 1: 스폰 마커 추가**
  - Instantiate 직후 `GetComponent`로 중복을 피하면서 마커를 추가한다.
- [ ] **Step 2: 최종 검증**
  - 런타임·에디터 어셈블리를 빌드하고 두 스폰 경로의 마커 연결을 확인한다.
