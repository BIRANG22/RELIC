# Shared Battle Scene Room Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Battle 씬의 공용 프레젠테이션과 UI를 Room 콘텐츠 활성 상태에서 분리한다.

**Architecture:** `SharedRoomRoot`는 항상 활성 상태로 유지하고 `BattleSceneController`는 Room별 콘텐츠만 교체한다. 공용 배경·맵 비주얼·비전투 파티 표시·공통 UI는 명시적 참조를 사용하며, BattleHUD와 전투 유닛은 BattleRoom 전용으로 유지한다. 보상 패널은 표시 책임만 갖고 Battle/Event별 완료 정책은 호출자가 제공한다.

**Tech Stack:** Unity 6, C#, Unity Scene YAML, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- BattleHUDCanvas, Grid, 전투 UnitRoot는 BattleRoom 전용으로 유지한다.
- 전투 결과와 네트워크 상태 구조는 변경하지 않는다.

---

### Task 1: 공용 Room 프레젠테이션 경계

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Test: `Assets/Tests/EditMode~/SharedBattleRoomLayoutTests.cs`

**Interfaces:**
- Produces: `BattleSceneController.TryPlaySharedMapVisualAction(string visualObjectId, string actionId)`

- [ ] 공용 루트가 Room 전환과 독립적으로 활성화되는 실패 테스트를 작성한다.
- [ ] 공용 배경과 MapVisual 적용 테스트를 작성한다.
- [ ] `BattleSceneController`에 공용 배경·MapVisual·파티 루트 명시적 참조를 구현한다.
- [ ] 기존 Room 하위 탐색은 공용 참조가 없을 때만 폴백하도록 유지한다.

### Task 2: 보상 View와 완료 정책 분리

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleRewardPanelUI.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleResultChecker.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs`
- Delete after scene migration: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomRewardPanelUI.cs`
- Test: `Assets/Tests/EditMode~/EventRoomRewardPanelFlowTests.cs`

**Interfaces:**
- Consumes: `BattleRewardPanelUI.Open(List<BattleRewardData>, Action)`
- Produces: 호출자가 소유하는 Battle/Event별 완료 콜백

- [ ] 패널 완료가 노드 클리어나 Room 정리를 직접 수행하지 않는 실패 테스트를 작성한다.
- [ ] Battle 완료 콜백이 기존 정리와 지도 복귀를 수행하도록 이동한다.
- [ ] Event Controller가 같은 패널을 사용하고 이벤트 완료 콜백을 전달하도록 변경한다.
- [ ] 중복 Event 보상 View와 스크립트를 제거한다.

### Task 3: Battle 씬 계층 마이그레이션

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/SharedBattleRoomLayoutTests.cs`

**Interfaces:**
- Consumes: `BattleSceneController` 공용 프레젠테이션 참조
- Produces: `SharedRoomRoot`, `SharedWorldRoot`, `SharedUICanvas`, 네 개의 Room Content 루트

- [ ] 씬 구조 기대값 테스트를 먼저 작성한다.
- [ ] 공용 배경, MapVisual, 비전투 파티 앵커, Inventory, Bag, Map, Reward Panel을 공용 루트로 이동한다.
- [ ] BattleHUDCanvas와 전투 UnitRoot가 BattleRoom 하위에 남아 있는지 확인한다.
- [ ] Room Controller와 Scene Controller 직렬화 참조를 새 공용 오브젝트에 연결한다.
- [ ] 중복 Room 배경과 Event 보상 패널을 제거한다.

### Task 4: 검증

**Files:**
- Test: `Assets/Tests/EditMode~/SharedBattleRoomLayoutTests.cs`
- Test: `Assets/Tests/EditMode~/EventRoomRewardPanelFlowTests.cs`

- [ ] `git diff --check`를 실행한다.
- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] 열린 Unity 에디터 로그에서 새 컴파일 오류를 확인한다.
- [ ] Unity Test Runner에서 관련 EditMode 테스트 실행이 필요한 항목을 완료 보고에 기록한다.
