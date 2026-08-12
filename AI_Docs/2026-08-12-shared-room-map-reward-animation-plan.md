# Shared Room Map, Reward, and Choice Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 지도 표시를 별도 MapRoom 없이 공용 룸 프레젠테이션으로 통합하고, 이벤트 보상 패널 표시 회귀와 Map_09 선택 애니메이션 연결을 복구한다.

**Architecture:** `SharedRoomRoot`가 배경, 맵 비주얼, 파티 표시를 계속 소유하며 지도 열기 시 기존 `MapRoomController`를 공용 루트의 프레젠테이션 갱신기로 재사용한다. 이벤트 결과는 안정적인 `VisualObjectId/VisualActionId`를 전달하고, `MapVisualActor`가 액션에 설정된 Animator 상태를 재생한다. 보상 패널은 공용 Canvas에서 정상 RectTransform을 유지한다.

**Tech Stack:** Unity 6, C#, Unity scene/prefab YAML, NUnit EditMode tests

## Global Constraints

- 문서와 계획은 `AI_Docs` 내부에만 작성한다.
- `BattleHUDCanvas`는 배틀룸 전용으로 유지한다.
- UI와 애니메이션은 이벤트 결과를 계산하지 않고 결과 ID만 소비한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR, 브랜치 변경은 수행하지 않는다.

---

### Task 1: MapRoom 제거와 공용 지도 프레젠테이션

**Files:**
- Modify: `Assets/Project/Scripts/MapRoomController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/SharedBattleSceneIntegrationTests.cs`

**Interfaces:**
- Produces: 공용 루트에 배치된 `MapRoomController.RefreshNow()`
- Consumes: `BattleMapPanel`, `StageBackgroundController`, 파티 Runtime ID와 캐릭터 프리팹 데이터베이스

- [ ] 공용 루트가 지도 열기 시 배경과 파티 표시를 갱신하는 테스트를 작성한다.
- [ ] `BattleSceneController`에서 MapRoom 탐색·활성화 의존성을 제거한다.
- [ ] Battle 씬의 빈 MapRoom과 MapPanel의 가시성 동기화 컴포넌트를 제거한다.
- [ ] `SharedRoomRoot`에 공용 프레젠테이션 컴포넌트와 기존 참조를 연결한다.

### Task 2: 공용 보상 패널 표시 복구

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Test: `Assets/Tests/EditMode~/SharedBattleSceneIntegrationTests.cs`

**Interfaces:**
- Consumes: `EventRoomController.rewardPanel`, `BattleRewardPanelUI.Open(...)`
- Produces: 글로벌 Canvas 하위에서 크기 1로 표시되는 `BattleRewardCanvas`

- [ ] `BattleRewardCanvas`가 공용 Canvas 하위이고 로컬 스케일이 1인지 검사하는 테스트를 작성한다.
- [ ] 씬 RectTransform의 스케일과 stretch anchor를 정상화한다.
- [ ] 이벤트룸과 배틀 결과가 동일한 `BattleRewardPanelUI` 참조를 사용하는지 검사한다.

### Task 3: Event_01 선택 애니메이션 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapVisualActor.cs`
- Modify: `Assets/Project/Data/MapVisual/MapVisual_TestCrystal.prefab`
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`
- Test: `Assets/Tests/EditMode~/MapVisualDatabaseTests.cs`
- Test: `Assets/Tests/EditMode~/EventChoiceVisualActionTests.cs`

**Interfaces:**
- Consumes: `event_visual_test_crystal/event_choice_success`
- Produces: `MapVisualActionEntry.AnimatorStateName`을 통한 선택 시점 애니메이션 재생

- [ ] Trigger가 없는 단일 상태 Controller에서도 명시적 상태 재생이 가능한 테스트를 작성한다.
- [ ] 액션에 `AnimatorStateName`을 추가하고 Trigger보다 뒤의 명시적 대체 경로로 구현한다.
- [ ] MapVisual_TestCrystal의 성공·실패 액션을 `New Animation` 상태에 연결한다.
- [ ] Event_01 선택 행에 성공 비주얼 오브젝트/액션 ID를 입력한다.
- [ ] C# 프로젝트 빌드와 정적 씬/데이터 검사를 수행한다.
