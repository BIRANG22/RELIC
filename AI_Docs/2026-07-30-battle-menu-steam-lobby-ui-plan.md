# 배틀 메뉴 복구·공유 MenuPanel·Steam 멀티 UI 구현 계획

> **For agentic workers:** 작업은 현재 세션에서 순서대로 수행한다. 각 동작 변경은 실패하는 EditMode 회귀 테스트를 먼저 작성하고 최소 구현으로 통과시킨다.

**Goal:** 전투 종료 후 맵 복귀 시 메뉴를 복원하고, 로비와 배틀이 같은 `MenuPanel` 프리팹을 사용하며, Steam 및 멀티 패널은 사용자의 명시적 멀티 진입 전까지 시작하지 않게 한다.

**Architecture:** 전투 UI 복구는 방에서 맵으로 이동하는 `BattleSceneController` 경계에 둔다. Steam 초기화와 런타임 멀티 UI 생성은 `SteamLobbyInviteController`의 명시적 진입 메서드로 지연한다. 로비의 자체 완결 `MenuPanel` 계층을 공용 프리팹으로 추출하고 두 씬의 로컬 복사본을 프리팹 인스턴스로 교체한다.

**Tech Stack:** Unity 6 C#, Unity UI/TMP, Steamworks.NET, Unity YAML 프리팹·씬 직렬화, NUnit EditMode 테스트

## Global Constraints

- 문서는 반드시 `AI_Docs` 내부에만 작성한다.
- 테스트는 반드시 `Assets/Tests/EditMode~/` 아래에 작성한다.
- Unity 에디터가 열려 있으므로 Unity batchmode 테스트는 실행하지 않는다.
- 커밋과 PR은 별도 사용자 허락 없이 진행하지 않는다.
- 전투 결과 계산과 UI 표시 상태를 분리한다.
- 기존 Steam 로비 ID, 권한, 공유 상태, 배틀 시작 명령 형식은 변경하지 않는다.

---

### Task 1: 맵 복귀 시 전투 실행 UI 복구

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleExecutionUiRootVisibilityTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleTurnExecutor.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs`

**Interfaces:**
- Produces: `public void BattleTurnExecutor.RestoreBattleExecutionUiAfterRoomEnd()`
- Consumes: `BattleSceneController.OpenMapPanelImmediate()`

- [ ] **Step 1: 실패하는 UI 복구 테스트 작성**

`BattleExecutionUiRootVisibilityTests`에 다음 동작을 추가한다.

```csharp
[Test]
public void RestoreBattleExecutionUiAfterRoomEnd_ReactivatesSuppressedRoots()
{
    GameObject executorObject = new("BattleTurnExecutorUiRestore");
    GameObject playerHudRoot = new("ConfiguredPlayerHUD_Root");
    GameObject menuRoot = new("ConfiguredMenuRoot");

    try
    {
        BattleTurnExecutor executor = executorObject.AddComponent<BattleTurnExecutor>();
        SetPrivateField(executor, "playerHudRoot", playerHudRoot);
        SetPrivateField(executor, "menuRoot", menuRoot);
        SetPrivateField(executor, "battleExecutionUiSuppressed", true);
        playerHudRoot.SetActive(false);
        menuRoot.SetActive(false);

        executor.RestoreBattleExecutionUiAfterRoomEnd();

        Assert.That(playerHudRoot.activeSelf, Is.True);
        Assert.That(menuRoot.activeSelf, Is.True);
        Assert.That(
            GetPrivateField<bool>(executor, "battleExecutionUiSuppressed"),
            Is.False);
    }
    finally
    {
        Object.DestroyImmediate(executorObject);
        Object.DestroyImmediate(playerHudRoot);
        Object.DestroyImmediate(menuRoot);
    }
}
```

테스트 보조 함수 `GetPrivateField<TValue>`도 같은 테스트 파일에 둔다.

- [ ] **Step 2: RED 확인**

`BattleTurnExecutor.RestoreBattleExecutionUiAfterRoomEnd()`가 아직 없으므로 테스트 소스의 대상 API 컴파일이 실패하는지 확인한다. Unity batchmode는 실행하지 않는다.

- [ ] **Step 3: 최소 복구 메서드 구현**

`BattleTurnExecutor`에 다음 메서드를 추가한다.

```csharp
public void RestoreBattleExecutionUiAfterRoomEnd()
{
    battleExecutionUiSuppressed = false;
    SetBattleExecutionUiVisible(true);
}
```

`BattleSceneController.OpenMapPanelImmediate()`에서 `battleMapPanel.Open(mapRuntime)` 직전에 비활성 오브젝트까지 포함하여 `BattleTurnExecutor`를 찾고 위 메서드를 호출한다.

```csharp
BattleTurnExecutor turnExecutor =
    Object.FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
turnExecutor?.RestoreBattleExecutionUiAfterRoomEnd();
```

- [ ] **Step 4: GREEN 확인**

메인 런타임 어셈블리를 빌드하여 새 API와 호출부가 컴파일되는지 확인하고 테스트 소스의 대상 API 컴파일 실패가 해소되는지 확인한다.

---

### Task 2: Steam과 멀티 패널의 명시적 진입

**Files:**
- Modify: `Assets/Tests/EditMode~/SteamOverlayLifecycleTests.cs`
- Modify: `Assets/Tests/EditMode~/SteamLobbyInviteControllerDevUiTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyInviteController.cs`

**Interfaces:**
- Produces: `private static bool ShouldInitializeSteamForLaunchCommand(string commandLine)`
- Produces: `private void EnsureMultiplayerUiVisible()`
- Consumes: `OpenInviteFlow()`, `CopyCurrentLobbyId()`, `JoinLobbyByIdInput()`, Steam `+connect_lobby` 실행 인자

- [ ] **Step 1: 실패하는 지연 초기화 테스트 작성**

`SteamOverlayLifecycleTests`에서 기존 `BeforeSplashScreen` 기대를 제거하고 다음 동작을 검사한다.

```csharp
[TestCase("RELIC.exe", false)]
[TestCase("RELIC.exe -logFile player.log", false)]
[TestCase("RELIC.exe +connect_lobby 109775241199441234", true)]
public void ShouldInitializeSteamForLaunchCommand_RequiresConnectLobby(
    string commandLine,
    bool expected)
{
    MethodInfo method = typeof(SteamLobbyInviteController).GetMethod(
        "ShouldInitializeSteamForLaunchCommand",
        BindingFlags.Static | BindingFlags.NonPublic);

    Assert.That(method, Is.Not.Null);
    Assert.That((bool)method.Invoke(null, new object[] { commandLine }), Is.EqualTo(expected));
}
```

`SteamLobbyInviteControllerDevUiTests`에는 컨트롤러 생성만으로 두 패널이 생기지 않고 `OpenInviteFlow()` 후 생성되는지 검사하는 테스트를 추가한다.

```csharp
[Test]
public void Controller_CreatesMultiplayerPanelsOnlyAfterInviteFlowStarts()
{
    var root = new GameObject("Root", typeof(RectTransform));
    var button = new GameObject("Invite", typeof(RectTransform));
    button.transform.SetParent(root.transform, false);
    SteamLobbyInviteController controller =
        button.AddComponent<SteamLobbyInviteController>();

    Assert.That(FindDescendant(root.transform, "SteamLobbyStatusPanel"), Is.Null);
    Assert.That(FindDescendant(root.transform, "SteamLobbyDevelopmentTools"), Is.Null);

    controller.OpenInviteFlow();

    Assert.That(FindDescendant(root.transform, "SteamLobbyStatusPanel"), Is.Not.Null);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Assert.That(FindDescendant(root.transform, "SteamLobbyDevelopmentTools"), Is.Not.Null);
#endif

    Object.DestroyImmediate(root);
}
```

- [ ] **Step 2: RED 확인**

새 판단 메서드가 없고 현재 `Awake()`가 두 패널을 즉시 생성하므로 두 테스트가 각각 누락 API와 잘못된 초기 상태로 실패하는지 확인한다.

- [ ] **Step 3: Steam 선행 초기화 제거**

- `RuntimeInitializeOnLoadType.BeforeSplashScreen` 메서드를 제거한다.
- `Awake()`에서는 참조 바인딩만 수행한다.
- `ShouldInitializeSteamForLaunchCommand(Environment.CommandLine)`이 참일 때만 멀티 UI 생성과 `InitializeSteam()`을 수행한다.
- `ProcessLaunchCommandLine()`은 `SteamApps.GetLaunchCommandLine` 대신 `Environment.CommandLine`을 기존 `SteamLobbyLaunchCommandParser`에 전달하여 Steam 초기화 전에도 진입 의도를 판정할 수 있게 한다.

판단 메서드는 다음과 같다.

```csharp
private static bool ShouldInitializeSteamForLaunchCommand(string commandLine)
{
    return SteamLobbyLaunchCommandParser.TryParseLobbyId(
        commandLine,
        out _);
}
```

- [ ] **Step 4: 멀티 패널 지연 생성 구현**

`EnsureMultiplayerUiVisible()`은 기존 `CreateStatusPanelIfNeeded()`를 한 번 호출하고 생성된 패널을 활성화한 뒤 상태를 새로고침한다. `OpenInviteFlow()`는 Steam 준비 확인보다 먼저 이 메서드를 호출하여 초기화 실패 메시지도 보이게 한다.

```csharp
private void EnsureMultiplayerUiVisible()
{
    CreateStatusPanelIfNeeded();
    SetGeneratedPanelActive("SteamLobbyStatusPanel", true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    SetGeneratedPanelActive("SteamLobbyDevelopmentTools", true);
#endif
    RefreshStatusPanel();
}
```

`CopyCurrentLobbyId()`와 `JoinLobbyByIdInput()`도 직접 호출될 수 있으므로 같은 진입 메서드를 먼저 호출한다. `CreateStatusPanelIfNeeded()`는 이름이 같은 패널이 이미 있으면 중복 생성하지 않는다.

- [ ] **Step 5: 초대 오버레이 호출 경계 확인**

`SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyId)`는 `OpenInviteDialog()` 한 곳에만 남기며 `pendingInviteDialog`는 `OpenInviteFlow()`에서만 설정한다. 자동 초기화 경로에서는 `OpenInviteDialog()`를 호출하지 않는다.

- [ ] **Step 6: GREEN 확인**

런타임 어셈블리 빌드와 테스트 소스 컴파일을 다시 수행한다. 일반 실행 문자열은 Steam 초기화를 요구하지 않고 `+connect_lobby` 문자열만 요구하는지 테스트 기대값과 대조한다.

---

### Task 3: 공용 MenuPanel 프리팹과 씬 인스턴스

**Files:**
- Create: `Assets/Project/PrefabsR/MenuPanel.prefab`
- Create: `Assets/Project/PrefabsR/MenuPanel.prefab.meta`
- Create: `Assets/Tests/EditMode~/SharedMenuPanelPrefabTests.cs`
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Modify: `Assets/Project/Scenes/YDM/Battle.unity`

**Interfaces:**
- Produces: `Assets/Project/PrefabsR/MenuPanel.prefab`
- Consumes: 로비의 현재 `MenuPanel` 25개 오브젝트 계층

- [ ] **Step 1: 실패하는 공유 프리팹 테스트 작성**

```csharp
#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SharedMenuPanelPrefabTests
{
    private const string PrefabPath =
        "Assets/Project/PrefabsR/MenuPanel.prefab";

    [Test]
    public void MenuPanelPrefab_UsesLobbyHierarchyAndStartsClosed()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.name, Is.EqualTo("MenuPanel"));
        Assert.That(prefab.activeSelf, Is.False);
        Assert.That(prefab.transform.Find("Continue"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Option"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Giveup"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Quit"), Is.Not.Null);
    }

    [TestCase("Assets/Project/Scenes/YDM/Lobby.unity")]
    [TestCase("Assets/Project/Scenes/YDM/Battle.unity")]
    public void Scene_ReferencesSharedMenuPanelPrefab(string scenePath)
    {
        string guid = AssetDatabase.AssetPathToGUID(PrefabPath);
        string sceneYaml = File.ReadAllText(scenePath);

        Assert.That(guid, Is.Not.Empty);
        Assert.That(
            sceneYaml,
            Does.Contain(
                $"m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}"));
    }
}
#endif
```

- [ ] **Step 2: RED 확인**

`MenuPanel.prefab`이 아직 없으므로 프리팹 로드와 두 씬의 소스 프리팹 참조 검사가 실패하는지 확인한다.

- [ ] **Step 3: 로비 MenuPanel 계층을 프리팹으로 추출**

- 로비 `MenuPanel` 루트와 24개 자식 오브젝트의 GameObject 및 Component YAML 블록을 프리팹으로 옮긴다.
- 프리팹 루트 `RectTransform.m_Father`는 `{fileID: 0}`으로 바꾼다.
- `MenuPanel`의 `m_IsActive`는 `0`을 유지한다.
- 씬 오브젝트 참조가 프리팹 내부에서 외부로 새지 않는지 확인한다.
- 고정 GUID의 `.meta`를 함께 만든다.

- [ ] **Step 4: 로비 씬을 프리팹 인스턴스로 교체**

- 로비 씬의 기존 25개 오브젝트 로컬 계층 블록을 제거한다.
- 원래 부모 Canvas에 `MenuPanel.prefab`의 `PrefabInstance`를 연결한다.
- `LobbyMenuController.menuPanel`, `LobbyMainPanelKeyboardInputController.menuPanel`, `continueButton`, 메뉴 버튼의 `panelToOpen`이 프리팹 인스턴스의 stripped 오브젝트를 가리키도록 기존 씬 fileID 참조를 보존한다.

- [ ] **Step 5: 배틀 씬을 같은 프리팹 인스턴스로 교체**

- 배틀 씬의 기존 19개 오브젝트 로컬 `MenuPanel` 계층을 제거한다.
- 기존 `MenuRoot` 아래에 같은 `MenuPanel.prefab` 인스턴스를 연결한다.
- `UIPanelButton.panelToOpen`, `BattleMenuEscapeInputController`의 자동 검색, Canvas 참조가 새 인스턴스를 사용하도록 stripped 루트 참조를 보존한다.

- [ ] **Step 6: GREEN 확인**

두 씬 YAML에 동일 GUID의 `m_SourcePrefab`이 정확히 한 번씩 존재하고, `AssetDatabase`에서 프리팹 루트와 필수 버튼 계층을 로드할 수 있는지 확인한다.

---

### Task 4: 전체 검증

**Files:**
- No production file creation

**Interfaces:**
- Consumes: Tasks 1–3의 코드·테스트·씬·프리팹
- Produces: 컴파일 및 직렬화 검증 근거

- [ ] **Step 1: 변경 범위 점검**

`git status --short`와 `git diff --stat`으로 설계 문서, 계획 문서, 지정된 코드·테스트·씬·프리팹 외 변경이 없는지 확인한다.

- [ ] **Step 2: 런타임과 에디터 어셈블리 빌드**

다음 두 명령을 실행한다.

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp-Editor.csproj /t:Build /p:RestorePackages=false /v:minimal
```

- [ ] **Step 3: 씬·프리팹 참조 검증**

- 프리팹 GUID가 로비와 배틀 씬에 각각 한 번 존재하는지 확인한다.
- 두 씬의 프리팹 인스턴스 부모가 각각 기존 Canvas와 `MenuRoot`인지 확인한다.
- 외부 참조가 사라진 scene-local fileID를 가리키지 않는지 점검한다.

- [ ] **Step 4: 정적 품질 검사**

`git diff --check`를 실행하여 공백 오류와 손상된 패치가 없는지 확인한다.

- [ ] **Step 5: 수동 Unity 확인 항목 보고**

Unity batchmode 테스트는 실행하지 않았음을 명시하고, 열린 Unity 에디터에서 다음 항목을 확인할 수 있도록 보고한다.

- 전투 행동 중 마지막 몬스터 사망 → 보상 획득 → 맵 복귀 → 메뉴 버튼 사용
- 로비와 배틀의 MenuPanel 외형 및 Continue/Option/Giveup/Quit 동작
- 일반 실행 시 Steam UI가 나타나지 않음
- 초대 클릭 후 상태·개발 패널 표시 및 Steam 초대 오버레이 열림

- [ ] **Step 6: 커밋 전 중단**

커밋이나 PR은 만들지 않는다. 사용자가 별도로 요청할 때만 변경 파일을 스테이징하고 커밋한다.
