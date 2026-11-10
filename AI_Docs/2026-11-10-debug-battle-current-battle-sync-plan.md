# DebugBattle Current Battle Sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** DebugBattle의 공용 전투 기능과 UI를 현재 Battle 씬과 동일하게 갱신하면서 DebugBattle 단독 실행용 DataManager와 테스트 도구를 보존한다.

**Architecture:** `Battle.unity`를 공용 씬 구성의 단일 기준으로 복제하고, DebugBattle 전용 `DataManager` YAML 문서와 SceneRoots 참조만 추가한다. EditMode 회귀 테스트가 허용된 차이와 필수 디버그 계약을 정적으로 고정한다.

**Tech Stack:** Unity scene YAML, C#, NUnit EditMode tests, MSBuild

**Spec:** `AI_Docs/2026-11-10-debug-battle-current-battle-sync-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- Battle 씬과 전투 핵심 계산 로직은 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- DataManager YAML 문서가 누락되면 DebugBattle 단독 실행이 실패하므로 정확히 한 개와 필수 DB 참조를 검사한다.
- Battle 공용 씬 변경이 DebugBattle에 누락되면 다시 노후화되므로 허용 차이를 제외한 정규화 결과가 같아야 한다.
- SceneRoots에 DataManager Transform이 빠지면 Unity가 루트로 복원하지 못하므로 루트 참조를 검사한다.
- DebugBattle `.meta` GUID가 바뀌면 Build Settings 참조가 깨지므로 씬 본문만 교체하고 meta는 보존한다.
- 디버그 런타임 도구가 제거되면 샌드박스 목적이 사라지므로 부트스트랩과 대상 컨트롤러 소스 계약을 검사한다.

---

### Task 1: 씬 동기화 회귀 테스트

**Files:**
- Create: `Assets/Tests/EditMode~/DebugBattleSceneSyncTests.cs`
- Test: `Assets/Tests/EditMode~/DebugBattleSceneSyncTests.cs`

**Interfaces:**
- Consumes: `Battle.unity`, `DebugBattle.unity` YAML 텍스트
- Produces: 허용된 DebugBattle 전용 차이와 필수 DataManager/디버그 계약 검증

- [x] **Step 1: 실패하는 씬 동기화 테스트 작성**

`DebugBattleScene_MatchesCurrentBattleExceptStandaloneDataManager`, `DebugBattleScene_PreservesStandaloneDataManagerReferences`, `DebugBattleRuntimeTools_RemainSceneScoped` 테스트를 작성한다.

- [x] **Step 2: 현재 씬에서 실패 확인**

Unity batchmode는 사용하지 않는다. MSBuild 테스트 프로젝트 컴파일과 정적 비교 명령으로 현재 불일치가 존재함을 확인한다.

- [x] **Step 3: 테스트 컴파일 확인**

`Assembly-CSharp-Editor.csproj`를 빌드하여 NUnit 테스트 소스가 컴파일되는지 확인한다.

### Task 2: DebugBattle 씬 전면 최신화

**Files:**
- Modify: `Assets/Project/Scenes/YDM/DebugBattle.unity`
- Preserve: `Assets/Project/Scenes/YDM/DebugBattle.unity.meta`

**Interfaces:**
- Consumes: 현재 `Battle.unity`, 기존 DebugBattle의 `DataManager` YAML 문서
- Produces: Battle 공용 구성과 동일하고 독립 실행 가능한 DebugBattle 씬

- [x] **Step 1: Battle 씬 본문을 DebugBattle 기준으로 복제**

`.meta`는 건드리지 않고 `Battle.unity` 본문만 기준으로 사용한다.

- [x] **Step 2: DebugBattle 전용 DataManager 복원**

기존 fileID `910050501`~`910050503` 문서와 `SceneRoots`의 Transform 참조 `910050502`를 추가한다.

- [x] **Step 3: 정적 동기화 테스트 조건 확인**

정규화한 두 씬이 동일하고 DataManager 및 필수 데이터베이스 GUID가 유지되는지 검사한다.

### Task 3: 전체 검증

**Files:**
- Verify: `Assets/Project/Scenes/YDM/Battle.unity`
- Verify: `Assets/Project/Scenes/YDM/DebugBattle.unity`
- Verify: `Assets/Tests/EditMode~/DebugBattleSceneSyncTests.cs`

**Interfaces:**
- Consumes: Task 1~2 결과
- Produces: YAML 무결성, 테스트 컴파일, 런타임 컴파일 검증 결과

- [x] **Step 1: 변경 범위와 YAML 참조 검사**

`git diff --check`, SceneRoots, DataManager 개수, DebugBattle meta 미변경을 확인한다.

- [x] **Step 2: 런타임 프로젝트 컴파일**

`Assembly-CSharp.csproj`를 MSBuild로 빌드하고 오류 0개를 확인한다.

- [x] **Step 3: 에디터/테스트 프로젝트 컴파일**

`Assembly-CSharp-Editor.csproj`를 MSBuild로 빌드하고 오류 0개를 확인한다.

- [x] **Step 4: 미검증 항목 기록**

Unity 에디터에서 DebugBattle 직접 실행, HUD 표시, 스킬 시전, 허수아비 회복 동작은 화면 검증 미수행 항목으로 남긴다.
