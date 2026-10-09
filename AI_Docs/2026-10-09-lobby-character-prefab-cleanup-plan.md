# Lobby Character Prefab Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 로비 월드 캐릭터 4명을 DB 기반으로 생성하고 미사용 A~C 프리팹과 레거시 로비 코드를 안전하게 제거한다.

**Architecture:** `CharacterPrefabDB.LobbyPrefab`을 월드 캐릭터 전용으로 정의하고 `LobbyWorldCharacterSettingOpenObject`가 ID 기반 배치 설정에 따라 생성한다. 삭제는 GUID와 코드 진입점이 모두 없는 항목으로 제한한다.

**Tech Stack:** Unity 6 C#, ScriptableObject, Unity Scene YAML, NUnit EditMode

**Spec:** `AI_Docs/2026-10-09-lobby-character-prefab-cleanup-design.md`

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 현재 작업공간에서 작업하며 커밋, Push, PR, 브랜치, worktree 작업은 수행하지 않는다.
- 전투 및 멀티플레이 상태 로직은 변경하지 않는다.

## Review Focus

- DB가 초기화되지 않은 상태에서도 로비 프리팹 조회가 정상 동작해야 한다.
- 빈 ID, 중복 ID, 누락 프리팹이 다른 캐릭터 생성을 막지 않아야 한다.
- 로비 씬 재활성화 시 월드 캐릭터가 중복 생성되지 않아야 한다.
- `Char_05`의 임시 프리팹 재사용이 의도치 않게 제거되지 않아야 한다.
- 삭제된 GUID가 씬, DB, Addressables 또는 다른 프리팹에 남지 않아야 한다.

---

### Task 1: DB 역할 정리와 월드 스폰 정책 테스트

**Files:**
- Create: `Assets/Tests/EditMode~/LobbyWorldCharacterSpawnerTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/CharacterPrefabDatabase.cs`
- Modify: `Assets/Project/Scripts/LobbyWorldCharacterSettingOpenObject.cs`

**Interfaces:**
- Consumes: `CharacterPrefabDatabase.TryGetLobbyPrefab(string, out GameObject)`
- Produces: ID 기반 로비 월드 캐릭터 생성 및 배치 검증 API

- [ ] DB 누락, 빈/중복 ID, 중복 생성 방지 테스트를 먼저 작성한다.
- [ ] 테스트가 새 API 부재로 실패하는지 확인한다.
- [ ] 최소 구현으로 테스트를 통과시킨다.
- [ ] 관련 EditMode 테스트와 컴파일을 다시 확인한다.

### Task 2: 로비 씬과 CharacterPrefabDB 마이그레이션

**Files:**
- Modify: `Assets/DB/CharacterPrefabDB.asset`
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`

**Interfaces:**
- Consumes: Task 1의 로비 월드 생성 설정
- Produces: `Char_01`~`Char_04` DB 연결과 기존 배치값을 보존한 런타임 생성 씬

- [ ] DB에 네 월드 프리팹을 연결하고 미사용 PreviewWorld 데이터를 제거한다.
- [ ] 현재 네 인스턴스의 배치·렌더·호버 값을 스폰 설정으로 이전한다.
- [ ] 씬에 직접 배치된 네 프리팹 인스턴스를 제거한다.
- [ ] YAML 참조와 컴파일을 확인한다.

### Task 3: 프리팹과 PartySlot 레거시 제거

**Files:**
- Delete: 승인된 A~C 프리팹 12개와 각 `.meta`
- Delete: `Assets/Project/Scripts/Gameplay/Scene/Lobby/PartySlot.cs`와 `.meta`
- Delete: `Assets/Project/Scripts/UI/Lobby/PartySlotButton.cs`와 `.meta`
- Modify: `CharBtn.cs`, `CharPick.cs`, `SteamLobbyPartySynchronizer.cs`, `LobbyMainPanelKeyboardInputController.cs`

**Interfaces:**
- Consumes: Task 2에서 더 이상 LobbySlot 프리팹을 사용하지 않는 DB
- Produces: 레거시 타입 참조가 없는 컴파일 가능한 로비 코드

- [ ] 레거시 타입을 제거했을 때 실패하는 컴파일/정적 검증을 확인한다.
- [ ] 과거 호환 호출을 제거한다.
- [ ] 프리팹과 메타 파일을 삭제한다.
- [ ] 삭제 GUID 및 타입 참조가 0건인지 확인한다.

### Task 4: 로비 비활성 오브젝트 및 전용 스크립트 감사

**Files:**
- Modify/Delete: 감사로 미사용이 입증된 `Lobby.unity` 오브젝트와 전용 스크립트만 해당
- Modify: `AI_Docs/2026-10-09-lobby-character-prefab-cleanup-design.md`

**Interfaces:**
- Consumes: 설계 문서의 3중 삭제 기준
- Produces: 제거/유지 근거가 기록된 최종 씬

- [ ] 초기 비활성 루트와 직렬화 참조를 목록화한다.
- [ ] 코드 활성화 및 런타임 진입점을 대조한다.
- [ ] 세 조건을 모두 만족하는 항목만 제거하고 근거를 문서에 기록한다.
- [ ] 씬의 누락 스크립트와 삭제 GUID를 검사한다.

### Task 5: 전체 검증

**Files:**
- Verify only

**Interfaces:**
- Consumes: Tasks 1~4 결과
- Produces: 컴파일, 정적 참조, 수동 검증 체크리스트 결과

- [ ] 관련 EditMode 테스트를 Unity 에디터에서 실행 가능한 상태로 확인한다.
- [ ] `Assembly-CSharp` 및 `Assembly-CSharp-Editor`를 빌드한다.
- [ ] 삭제 에셋 GUID, `PartySlot` 타입, missing script 참조를 전체 검색한다.
- [ ] 로비 수동 확인 항목을 결과에 기록한다.
