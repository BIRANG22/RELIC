# Arabella Spawn Appear Dialogue Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 아라벨라 최초 스폰 시 두 등장 애니메이션과 두 대사를 동기화해 재생하고 완료 뒤 Idle 및 기존 전투 시작 흐름으로 전환한다.

**Architecture:** 프리팹 소유 `ArabellaSpawnPresentation`이 Animator 상태, 클립 길이, 대사 패널을 함께 제어한다. `BattleRoomLoader`가 스폰된 아라벨라 Presenter를 제공하고 `BattleSceneController`가 기존 보스 인트로 게이트를 Presenter 완료까지 유지한다.

**Tech Stack:** Unity 6, C#, Animator Controller YAML, TMP UI, NUnit EditMode tests

**Spec:** `AI_Docs/2026-11-10-arabella-spawn-appear-dialogue-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity 에디터 직접 제어와 batchmode 테스트를 수행하지 않는다.
- UI는 전투 결과를 계산하지 않고 표현만 담당한다.
- 기존 아라벨라 ID, 전투 상태, AI, 랜덤 및 네트워크 스냅샷은 변경하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

## Review Focus

- 등장 Presenter 참조가 빠져도 보스 인트로가 영구 대기하지 않고 기존 전투가 진행되어야 한다.
- 이어하기에서는 아라벨라 등장과 대사가 반복되지 않아야 한다.
- 두 등장 클립이 끝나기 전에 초기 행동 계획이나 입력이 열리지 않아야 한다.
- 등장 완료 후 반드시 기존 `Idle` 및 지속 Idle VFX 흐름으로 복귀해야 한다.
- 프리팹 UI와 Animator 상태의 fileID/GUID 참조가 실제 에셋과 일치해야 한다.

---

### Task 1: 등장 Presenter 계약과 테스트

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Monster/ArabellaSpawnPresentation.cs`
- Create: `Assets/Tests/EditMode~/ArabellaSpawnPresentationTests.cs`

**Interfaces:**
- Produces: `IEnumerator Play()`, `bool HasPlayed`, `bool CanPlay`
- Consumes: `Animator`, `BattleUnitAnimator`, `AnimationClip`, `GameObject dialogueRoot`, `TMP_Text dialogueText`

- [x] **Step 1: 실패하는 Presenter 테스트 작성**

재생 순서가 `Appear01 -> Appear02 -> Idle`이고 두 문장 전환, 패널 숨김, 중복 재생 방지를 제공하는지 소스·프리팹 계약 테스트로 고정한다.

- [x] **Step 2: RED 확인**

새 클래스 부재로 테스트 포함 에디터 어셈블리 빌드가 실패하는지 확인한다.

- [x] **Step 3: Presenter 최소 구현**

`ArabellaSpawnPresentation.Play()`가 `GameLocalization.Get`으로 문장을 설정하고 각 클립 길이만큼 unscaled time으로 기다린 뒤 `PlayIdle()`을 호출하도록 구현한다.

- [x] **Step 4: GREEN 컴파일 확인**

새 테스트를 명시적으로 포함해 `Assembly-CSharp-Editor.csproj` 빌드가 성공하는지 확인한다.

### Task 2: 보스 스폰 게이트 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleRoomLoader.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Test: `Assets/Tests/EditMode~/ArabellaSpawnPresentationTests.cs`

**Interfaces:**
- Produces: `ArabellaSpawnPresentation FindDeferredBossSpawnPresentation()`
- Consumes: `ArabellaSpawnPresentation.Play()`

- [x] **Step 1: 실패하는 호출 순서 테스트 작성**

`CompleteDeferredMonsterSpawn` 뒤 Presenter를 찾아 `Play()` 완료 후에만 `pendingBattleRoomUsesBossIntro`와 `IsBattleRoomIntroPlaying`을 해제하는 순서를 검사한다.

- [x] **Step 2: 스폰된 아라벨라 Presenter 검색 구현**

`spawnedMonsterUnits` 중 `ArabellaPhaseTransition.IsArabellaMonsterId(runtime.MonsterId)`인 유닛의 자식 Presenter만 반환한다.

- [x] **Step 3: SceneController 코루틴 연결**

기존 배경 reveal이 없거나 완료된 뒤 몬스터를 스폰하고 Presenter를 재생하며, 누락 시 즉시 기존 완료 경로로 fallback한다.

- [x] **Step 4: 컴파일과 정적 순서 검사**

런타임과 테스트 어셈블리 빌드 및 소스 호출 순서를 확인한다.

### Task 3: Animator 및 프리팹 UI 구성

**Files:**
- Modify: `Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Controller/Arabella_BattleAnimatorController.controller`
- Modify: `Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Arabella.prefab`
- Test: `Assets/Tests/EditMode~/ArabellaSpawnPresentationTests.cs`

**Interfaces:**
- Consumes: Presenter 직렬화 필드
- Produces: `Appear01`, `Appear02` Animator 상태 및 프리팹 배치 대사 패널

- [x] **Step 1: Animator 상태 실패 테스트 작성**

두 상태가 각각 `appear01.anim`, `appear02.anim`을 참조하고 기본 상태는 기존 `Idle`인지 검사한다.

- [x] **Step 2: Controller 상태 추가**

자동 전이 없는 `Appear01`, `Appear02` 상태를 기존 Base Layer에 추가한다.

- [x] **Step 3: 대사 UI와 Presenter 프리팹 배치**

비활성 대사 루트, 검은 배경과 테두리, 흰색 TMP 텍스트를 아라벨라 프리팹에 배치하고 모든 Presenter 참조를 연결한다.

- [x] **Step 4: 프리팹 YAML 및 테스트 컴파일 검증**

필수 fileID, clip GUID, 상태명, 대사 키와 문구가 정확한지 확인한다.

### Task 4: 최종 검증

**Files:**
- Verify: 위 변경 파일 전체

**Interfaces:**
- Consumes: Task 1~3 결과
- Produces: 컴파일 및 정적 무결성 검증 결과

- [x] **Step 1: `git diff --check` 및 변경 범위 확인**
- [x] **Step 2: `Assembly-CSharp.csproj` 재빌드**
- [x] **Step 3: 새 테스트 포함 `Assembly-CSharp-Editor.csproj` 재빌드**
- [x] **Step 4: Unity 에디터 화면/실행 미검증 항목 기록**
