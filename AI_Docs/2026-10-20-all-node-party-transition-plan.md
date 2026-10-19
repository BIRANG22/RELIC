# All Node Party Transition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전투를 포함한 모든 노드의 파티 입장·퇴장과 지도 선택 퇴장을 화면 전환에 맞춰 일관되게 재생한다.

**Architecture:** 기존 파티 전환 Presenter가 호출자가 지정한 루트에서 전투 캐릭터와 지도/이벤트 캐릭터 마커를 수집하도록 일반화한다. `BattleSceneController`가 지도와 비전투 노드의 전환 순서를 조정하고, 전투 로더는 명시적인 스폰 루트를 사용한다.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, Unity scene YAML

**Spec:** `AI_Docs/2026-10-20-all-node-party-transition-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode는 사용하지 않는다.
- 연출은 전투 상태와 결과를 변경하지 않는다.
- 커밋, Push, PR, 브랜치·worktree 작업을 수행하지 않는다.

## Review Focus

- Presenter 밖의 명시적 루트에 생성된 전투 캐릭터도 수집되어야 한다.
- 마커 캐릭터는 `BattleCharacter`가 없어도 수집되어야 한다.
- 한 Transform이 두 유형에 해당해도 한 번만 움직여야 한다.
- 대상이 전환 중 파괴되거나 비활성화돼도 Task가 끝나야 한다.
- 지도 선택 진입에서는 입장 연출이 호출되지 않고 노드 선택 시 퇴장만 호출되어야 한다.

---

### Task 1: 명시적 루트 기반 대상 수집

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleRoomPartyTransitionPresenter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Spawner/BattleUnitSpawner.cs`
- Modify: `Assets/Tests/EditMode~/BattleRoomPartyTransitionTests.cs`

**Interfaces:**
- Produces: `PrepareEntry(Transform root)`, `PlayExitAsync(Transform root)`, `BattleUnitSpawner.PlayerRoot`

- [ ] Presenter 계층 밖 전투 캐릭터와 마커 수집, 중복 제거를 검증하는 실패 테스트를 작성한다.
- [ ] Editor 테스트 컴파일로 새 API 부재 실패를 확인한다.
- [ ] 명시적 루트 수집 API와 `PlayerRoot` 읽기 전용 속성을 최소 구현한다.
- [ ] 표준/Editor 테스트 컴파일을 실행한다.

### Task 2: 전환 흐름 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleRoomLoader.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`
- Modify: `Assets/Tests/EditMode~/BattleRoomPartyTransitionTests.cs`

**Interfaces:**
- Consumes: Task 1의 명시적 루트 API
- Produces: 전투/비전투 노드 입장·퇴장 및 지도 선택 퇴장 순서

- [ ] 전환 정책과 씬 직렬화 연결을 검증하는 실패 테스트를 작성한다.
- [ ] Editor 테스트 컴파일 또는 정적 검증으로 실패를 확인한다.
- [ ] 전투 로더가 `PlayerRoot`를 사용하게 하고, 컨트롤러가 공용 파티 루트의 지도 퇴장·비전투 입장·퇴장을 호출하도록 구현한다.
- [ ] 입장은 화면 전환 종료 뒤 시작하도록 조정하고 씬 타이밍 값을 연결한다.
- [ ] 표준/Editor 테스트 컴파일과 씬 YAML 참조 검증을 실행한다.

### Task 3: 회귀 검증

**Files:**
- Modify: `AI_Docs/2026-10-20-all-node-party-transition-plan.md`

**Interfaces:**
- Consumes: Task 1~2 결과

- [ ] 변경 diff에서 요청 범위 밖 수정이 없는지 확인한다.
- [ ] Assembly-CSharp와 Assembly-CSharp-Editor 빌드를 새로 실행한다.
- [ ] 씬 local object ID 중복, 누락 스크립트 GUID, 직렬화 참조를 검사한다.
- [ ] Unity 에디터 직접 재생 검증이 필요한 항목을 완료 보고에 분리한다.

### Task 4: 비전투 이동 애니메이션 및 앵커 보정

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoomMapSelectionPresenter.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleRoomPartyTransitionPresenter.cs`
- Modify: `Assets/Project/Scripts/MapRoomController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/StartRoom/StartRoomController.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomController.cs`
- Modify: `Assets/Tests/EditMode~/BattleRoomPartyTransitionTests.cs`

- [x] 비전투 프리팹의 빈 이동 상태와 고정 스폰 앵커를 원인으로 확인한다.
- [x] 이동 중 전투용 Move Controller를 사용하고 원래 Controller/Idle을 복원한다.
- [x] 지도·휴식 자원 UI/VFX 앵커를 움직이는 캐릭터 Transform으로 변경한다.
- [x] 지도 선택·시작·휴식 생성 경로 모두 동일 캐릭터의 전투 Controller를 연결한다.
- [x] Runtime 및 Editor 어셈블리 컴파일을 확인한다.
