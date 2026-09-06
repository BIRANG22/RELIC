# Character Rest Prefab Implementation Plan

> **For agentic workers:** Implement each task in order using test-first development. Do not commit, push, create a branch, or create a PR without explicit user approval.

**Goal:** 휴식방에서 `Char_01~03`의 전용 Rest 프리팹을 ID 기반 데이터베이스 조회로 생성한다.

**Architecture:** `CharacterPrefabDatabase`가 캐릭터 ID별 전용 `RestPrefab` 참조를 소유하고 조회 API를 제공한다. `RestRoomController`는 이 API만 사용하며 다른 프리팹으로 대체하지 않는다.

**Tech Stack:** Unity, C#, ScriptableObject, NUnit EditMode tests, Unity YAML assets

**Spec:** `AI_Docs/2026-09-02-character-rest-prefab-design.md`

## Global Constraints

- 문서는 `AI_Docs` 아래에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에 작성한다.
- `Char_04~05`의 `RestPrefab`은 비워 둔다.
- 전용 Rest 프리팹이 없을 때 이벤트 프리팹 등으로 대체하지 않는다.
- 커밋, Push, PR, 브랜치 및 worktree 작업을 수행하지 않는다.

---

### Task 1: Rest 프리팹 조회 계약

**Files:**
- Create: `Assets/Tests/EditMode~/CharacterPrefabDatabaseTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/Database/CharacterPrefabDatabase.cs`

**Interfaces:**
- Produces: `bool TryGetRestPrefab(string characterId, out GameObject prefab)`

- [ ] `RestPrefab`이 연결된 엔트리는 해당 프리팹을 반환하는 실패 테스트를 작성한다.
- [ ] 테스트 컴파일을 실행하여 API 부재로 실패하는지 확인한다.
- [ ] `CharacterPrefabEntry.RestPrefab`과 `TryGetRestPrefab`을 최소 구현한다.
- [ ] `RestPrefab`이 비어 있는 엔트리는 대체 없이 `false`를 반환하는 테스트를 추가한다.
- [ ] 테스트가 컴파일되고 통과 가능한 상태인지 확인한다.

### Task 2: 휴식방과 에셋 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/RestRoom/RestRoomController.cs`
- Modify: `Assets/DB/CharacterPrefabDB.asset`

**Interfaces:**
- Consumes: `CharacterPrefabDatabase.TryGetRestPrefab(string, out GameObject)`

- [ ] 휴식방 생성 조회를 `TryGetRestPrefab`으로 교체한다.
- [ ] `Char_01~03`에 각각 `hilt_rest`, `kaya_rest`, `haze_rest` 프리팹을 연결한다.
- [ ] `Char_04~05`의 Rest 참조를 비워 둔다.

### Task 3: 검증

**Files:**
- Verify: `Assets/Tests/EditMode~/CharacterPrefabDatabaseTests.cs`
- Verify: `Assets/DB/CharacterPrefabDB.asset`

- [ ] C# 프로젝트를 빌드하여 컴파일 오류가 없는지 확인한다.
- [ ] 가능한 경우 Unity Test Runner에서 EditMode 테스트를 실행한다.
- [ ] 각 Rest 프리팹 메타 GUID와 데이터베이스 참조를 대조한다.
- [ ] `git diff --check` 및 변경 범위를 확인한다.
