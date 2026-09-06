# MapId 기반 룸 비주얼 DB 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 맵 노드 선택 시 `MapId` 기준으로 룸별 NPC/오브젝트 비주얼 프리팹을 인스펙터 DB에서 생성할 수 있게 한다.

**Architecture:** `MapVisualDatabase`는 `MapId`별 비주얼 생성 데이터를 보관하고, `MapVisualController`는 룸 하위에서 생성/정리를 담당한다. `BattleSceneController`는 룸을 열 때 현재 `GeneratedMapNodeData.MapId`를 전달한다.

**Tech Stack:** Unity C#, ScriptableObject, NUnit EditMode tests.

## Global Constraints

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성한다.
- Unity batchmode 테스트는 실행하지 않는다.
- 전투 결과에 영향을 주는 상태 변경은 추가하지 않는다.
- `DebugBattle`, `Battletest` 씬은 수정하지 않는다.

---

### Task 1: MapVisualDatabase 테스트와 구현

**Files:**
- Create: `Assets/Tests/EditMode~/MapVisualDatabaseTests.cs`
- Create: `Assets/Project/Scripts/Gameplay/Data/Database/MapVisualDatabase.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Data/DataManager.cs`

**Interfaces:**
- Produces: `MapVisualDatabase.TryGetEntry(string mapId, out MapVisualEntry entry)`
- Produces: `DataManager.MapVisualDatabase`

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void TryGetEntry_ReturnsConfiguredEntryForTrimmedMapId()
{
    MapVisualDatabase database = ScriptableObject.CreateInstance<MapVisualDatabase>();
    MapVisualEntry entry = new() { MapId = "Map_Event_01" };
    SetEntries(database, new List<MapVisualEntry> { entry });

    bool found = database.TryGetEntry(" Map_Event_01 ", out MapVisualEntry loaded);

    Assert.That(found, Is.True);
    Assert.That(loaded, Is.SameAs(entry));
}
```

- [ ] **Step 2: Verify red**

Run `dotnet test` is not available for Unity EditMode tests in this project. Verify red by compiling with MSBuild before production code; expected result is missing type errors for `MapVisualDatabase`.

- [ ] **Step 3: Implement minimal DB**

Add serializable `MapVisualEntry`, `MapVisualSpawnEntry`, and `MapVisualDatabase` with trimmed ID lookup.

- [ ] **Step 4: Verify green**

Run MSBuild for `RELIC.sln`; expected result is successful compile with only existing warnings.

### Task 2: MapVisualController 테스트와 구현

**Files:**
- Modify: `Assets/Tests/EditMode~/MapVisualDatabaseTests.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Battle/MapVisualController.cs`

**Interfaces:**
- Consumes: `MapVisualDatabase.TryGetEntry(string mapId, out MapVisualEntry entry)`
- Produces: `MapVisualController.ApplyMapVisual(string mapId)`
- Produces: `MapVisualController.ClearVisuals()`

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void ApplyMapVisual_SpawnsConfiguredPrefabsUnderNamedAnchor()
{
    GameObject room = new("Room");
    GameObject root = new("VisualRoot");
    GameObject anchor = new("NpcAnchor");
    root.transform.SetParent(room.transform, false);
    anchor.transform.SetParent(root.transform, false);

    MapVisualController controller = room.AddComponent<MapVisualController>();
    SetPrivateField(controller, "visualRoot", root.transform);
    SetPrivateField(controller, "anchors", new[] { anchor.transform });
    SetPrivateField(controller, "databaseOverride", database);

    controller.ApplyMapVisual("Map_Event_01");

    Assert.That(anchor.transform.Find("NpcPrefab"), Is.Not.Null);
}
```

- [ ] **Step 2: Verify red**

Run MSBuild before controller implementation; expected result is missing type errors for `MapVisualController`.

- [ ] **Step 3: Implement controller**

Controller clears previously spawned instances, looks up DB by `MapId`, resolves named anchors, instantiates active spawn entries, and resets local transform.

- [ ] **Step 4: Verify green**

Run MSBuild for `RELIC.sln`; expected result is successful compile with only existing warnings.

### Task 3: BattleSceneController 연동

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`
- Modify: `Assets/Tests/EditMode~/MapVisualDatabaseTests.cs`

**Interfaces:**
- Consumes: `MapVisualController.ApplyMapVisual(string mapId)`

- [ ] **Step 1: Write failing reflection test**

```csharp
[Test]
public void BattleSceneController_HasRoomVisualApplyMethod()
{
    MethodInfo method = typeof(BattleSceneController).GetMethod(
        "ApplyRoomVisual",
        BindingFlags.Instance | BindingFlags.NonPublic);

    Assert.That(method, Is.Not.Null);
}
```

- [ ] **Step 2: Verify red**

Run MSBuild before controller integration; expected result is reflection test compiled but not behavior-ready until method is added.

- [ ] **Step 3: Implement integration**

Add private `ApplyRoomVisual(GameObject room, GeneratedMapNodeData nodeData)` and call it from each room open path after background selection and before `OpenRoom`.

- [ ] **Step 4: Verify green**

Run MSBuild for `RELIC.sln`; expected result is successful compile with only existing warnings.

### Task 4: Battle 씬 연결

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

**Interfaces:**
- Consumes: `MapVisualController`

- [ ] **Step 1: Inspect scene roots**

Find `StartRoom`, `BattleRoom`, `EventRoom`, and `RestRoom` objects in the scene YAML.

- [ ] **Step 2: Add components only where safe**

If a room has a stable transform root, add `MapVisualController` with default fields left inspector-editable. Do not add generated choice buttons or alter `DebugBattle`/`Battletest`.

- [ ] **Step 3: Verify scene text**

Search the scene YAML for the `MapVisualController` script GUID and confirm only `Battle.unity` changed.

### Task 5: Final verification

**Files:**
- All modified files

**Interfaces:**
- Uses existing project build flow.

- [ ] **Step 1: Run whitespace check**

Run `git diff --check`; expected result is no whitespace errors.

- [ ] **Step 2: Run MSBuild**

Run approved Visual Studio MSBuild for `RELIC.sln`; expected result is successful compile with only pre-existing warnings.

- [ ] **Step 3: Report unverified runtime items**

Report that Unity EditMode execution was not run because project rules prohibit batchmode tests in this environment.
