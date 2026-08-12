# Persistent Event Choice and Action Slots Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Event_08의 중복 종료 선택지를 공통 선택지 하나로 합치고 선택지 5개 기준 성공/실패 연출 슬롯 10개를 제공한다.

**Architecture:** `EventData`의 지속 플래그를 로더가 읽고, 이벤트룸은 한 번의 이벤트 세션 동안 지속 선택지를 보관해 후속 정의의 선택지와 합성한다. 연출은 기존 행 단위 성공/실패 ID를 유지하면서 MapVisualActor에 10개의 표준 액션 ID를 등록한다.

**Tech Stack:** Unity 6, C#, CSV, NUnit EditMode tests

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 이벤트 결과 계산과 프레젠테이션을 분리한다.
- 테스트는 `Assets/Tests/EditMode~`에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋, Push, PR은 수행하지 않는다.

---

### Task 1: 지속 선택지 데이터와 합성

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Data/QuestEvent/EventMasterData.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs`
- Test: `Assets/Tests/EditMode~/EventDataIntegrationTests.cs`

**Interfaces:**
- Produces: `EventData.PersistAcrossNextEvent`
- Produces: `EventChoiceSequenceUtility.MergeChoices(...)`

- [ ] 지속 선택지가 후속 단계에 한 번만 합성되는 실패 테스트를 작성한다.
- [ ] 데이터 필드와 합성 유틸리티를 구현한다.
- [ ] 이벤트룸 진입/종료 경계에서 지속 목록을 초기화하고 단계 전환 시 합성한다.

### Task 2: Event_08 데이터 정리

**Files:**
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`
- Test: `Assets/Tests/EditMode~/EventDataIntegrationTests.cs`

**Interfaces:**
- Consumes: `PersistAcrossNextEvent`
- Produces: Event_08 계열 네 행

- [ ] Event_08 계열이 네 행이고 돌아선다가 한 행인지 검사한다.
- [ ] 중복 두 행을 제거하고 공통 종료 행에 지속 플래그를 설정한다.

### Task 3: 선택지별 성공/실패 액션 10개

**Files:**
- Modify: `Assets/Project/Data/MapVisual/MapVisual_TestCrystal.prefab`
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`
- Test: `Assets/Tests/EditMode~/MapVisualDatabaseTests.cs`
- Test: `Assets/Tests/EditMode~/EventChoiceVisualActionTests.cs`

**Interfaces:**
- Produces: `event_choice_01_success`부터 `event_choice_05_failure`까지 10개 ID

- [ ] 프리팹에 10개 ID가 각각 한 번 존재하는 실패 테스트를 작성한다.
- [ ] 기존 테스트 액션을 표준 ID로 교체하고 나머지 슬롯을 추가한다.
- [ ] Event_08 행의 성공/실패 액션을 선택지 번호별로 연결한다.
- [ ] 런타임 및 에디터 어셈블리 빌드와 정적 데이터 검사를 수행한다.
