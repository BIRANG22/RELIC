# Map Visual Choice Action Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 이벤트 선택지 결과가 MapId 기반 비주얼 오브젝트에 ID 신호를 보내고, 테스트 이미지 프리팹이 그 신호로 변화를 재생하게 한다.

**Architecture:** 이벤트 선택지는 `Success/Failure VisualObjectId + ActionId` 필드를 가진다. `EventChoiceExecutionResult`가 계산 결과와 함께 비주얼 신호를 반환하고, `EventRoomController`가 현재 룸의 `MapVisualController`에 전달한다. `MapVisualActor`는 인스펙터 설정에 따라 애니메이터 트리거, VFX, 색상/스케일/활성 상태 변화를 수행한다.

**Tech Stack:** Unity C#, ScriptableObject, prefab YAML, NUnit EditMode tests.

## Global Constraints

- 문서는 `AI_Docs` 안에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 전투/이벤트 결과 계산은 UI/VFX가 아니라 기존 실행 서비스가 담당한다.
- 커밋, Push, PR은 수행하지 않는다.

---

### Task 1: RED 테스트 추가

**Files:**
- Modify: `Assets/Tests/EditMode~/MapVisualDatabaseTests.cs`
- Modify: `Assets/Tests/EditMode~/EventChoiceExecutionServiceTests.cs`
- Modify: `Assets/Tests/EditMode~/EventDataIntegrationTests.cs`

**Interfaces:**
- Consumes: existing `MapVisualController`, `EventChoiceExecutionService`, `EventCsvLoader`
- Produces: failing expectations for `MapVisualActor`, visual IDs on spawn entries, and visual action result fields

- [ ] `MapVisualController`가 `MapVisualSpawnEntry.VisualObjectId`로 생성된 `MapVisualActor`를 등록하고 `TryPlayAction(string, string)`으로 액션을 재생하는 테스트를 추가한다.
- [ ] 선택지 성공 시 `EventChoiceExecutionResult.VisualObjectId/VisualActionId`가 성공 신호를 반환하는 테스트를 추가한다.
- [ ] 선택지 실패 시 실패 신호를 반환하는 테스트를 추가한다.
- [ ] `EventCsvLoader`가 새 컬럼을 `EventData`에 매핑하는 테스트를 추가한다.
- [ ] 독립 C# 컴파일로 테스트가 새 API 부재 때문에 실패하는 것을 확인한다.

### Task 2: 런타임 코드 구현

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapVisualActor.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/MapVisualDatabase.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/QuestEvent/EventMasterData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapVisualController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventChoiceExecutionService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs`

**Interfaces:**
- Produces: `MapVisualActor.TryPlayAction(string actionId)`
- Produces: `MapVisualController.TryPlayAction(string visualObjectId, string actionId)`
- Produces: `EventChoiceExecutionResult.VisualObjectId`, `VisualActionId`, `HasVisualAction`

- [ ] `MapVisualActor`에 액션 리스트와 런타임 ID 오버라이드를 구현한다.
- [ ] 액션은 애니메이터 트리거, VFX 프리팹 생성, SpriteRenderer 색상, Transform 스케일, GameObject 활성 상태를 지원한다.
- [ ] `MapVisualController`가 생성한 프리팹의 액터를 등록하고 클리어 시 등록도 지운다.
- [ ] `EventData`에 성공/실패 비주얼 신호 필드를 추가한다.
- [ ] `EventChoiceExecutionService`가 성공/실패에 맞는 비주얼 신호를 결과에 담는다.
- [ ] `EventRoomController`가 선택지 처리 후 비주얼 신호를 현재 룸의 컨트롤러로 전달한다.

### Task 3: 테스트 프리팹과 샘플 DB 구성

**Files:**
- Create: `Assets/Project/Data/MapVisual/MapVisual_TestCrystal.prefab`
- Create: `Assets/Project/Data/MapVisual/MapVisual_TestCrystal.prefab.meta`
- Modify: `Assets/Project/Data/MapVisual/Map Visual Database.asset`

**Interfaces:**
- Consumes: `Assets/Project/Data/MapVisual/Test.png`
- Produces: `Map_09` sample spawn with `VisualObjectId = event_visual_test_crystal`

- [ ] `Test.png`를 SpriteRenderer에 연결한 테스트 프리팹을 만든다.
- [ ] 프리팹에 `MapVisualActor`를 붙이고 `event_choice_success`, `event_choice_failure` 액션을 설정한다.
- [ ] `Map Visual Database.asset`의 `Map_09` 스폰을 테스트 프리팹으로 채운다.

### Task 4: 검증

**Files:**
- No new production files.

**Interfaces:**
- Consumes: all changed files
- Produces: verification result

- [ ] `git diff --check`를 실행한다.
- [ ] 새/수정 C# 파일 컴파일 검증을 실행한다.
- [ ] 기존 프로젝트 MSBuild 검증을 실행한다.
- [ ] Unity 에디터 런타임 수동 확인 항목을 완료 보고에 남긴다.
