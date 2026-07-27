# Steam 로비 직접 참가 및 개발 빌드 초기화 개선 구현 계획

> **작업 지침:** `superpowers:executing-plans`를 사용해 현재 세션에서 작업별 검토 지점을 두고 실행한다.

**목표:** 개발 환경에서 Lobby ID 복사·직접 참가를 제공하고, Windows 빌드 후 `steam_appid.txt`를 EXE 옆에 자동 배치한다.

**아키텍처:** Lobby ID 문자열 검증과 빌드 경로 계산은 Steamworks 및 UI와 분리된 순수 클래스로 둔다. 기존 `SteamLobbyInviteController`는 검증 결과를 기존 `JoinLobby(CSteamID)`에 전달하며, 빌드 후 처리기는 Windows Standalone에만 App ID 파일을 복사한다.

**기술 스택:** Unity 6, C#, Steamworks.NET 2025.163.0, TextMeshPro, NUnit EditMode tests

## 전체 제약

- 문서는 `AI_Docs` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity 에디터가 열려 있으므로 Unity batchmode 테스트는 실행하지 않는다.
- 일반 출시 빌드에는 개발용 Lobby ID UI를 노출하지 않는다.
- 전투 로직과 네트워크 권한 구조는 변경하지 않는다.
- 사용자 허락 없이 커밋 또는 PR을 만들지 않는다.

---

### 작업 1: Lobby ID 입력 검증기

**파일:**
- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyIdParser.cs`
- 테스트: `Assets/Tests/EditMode~/SteamLobbyIdParserTests.cs`

**인터페이스:**
- 생성: `public static bool SteamLobbyIdParser.TryParse(string input, out ulong lobbyId, out string error)`
- 소비: 작업 2의 `SteamLobbyInviteController.JoinLobbyByIdInput()`

- [ ] **1단계: 실패하는 파서 테스트 작성**

```csharp
[TestCase(" 109775244533745760 ", 109775244533745760UL)]
public void TryParse_ValidDecimalLobbyId_ReturnsValue(string input, ulong expected)
{
    Assert.That(SteamLobbyIdParser.TryParse(input, out ulong value, out string error), Is.True);
    Assert.That(value, Is.EqualTo(expected));
    Assert.That(error, Is.Empty);
}

[TestCase(null, "Lobby ID is empty.")]
[TestCase("", "Lobby ID is empty.")]
[TestCase("abc", "Lobby ID must be a positive decimal number.")]
[TestCase("0", "Lobby ID must be greater than zero.")]
public void TryParse_InvalidLobbyId_ReturnsSpecificError(string input, string expectedError)
{
    Assert.That(SteamLobbyIdParser.TryParse(input, out ulong value, out string error), Is.False);
    Assert.That(value, Is.Zero);
    Assert.That(error, Is.EqualTo(expectedError));
}
```

- [ ] **2단계: C# 빌드로 RED 확인**

실행:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp-Editor.csproj /t:Build /p:RestorePackages=false /v:minimal
```

예상: `SteamLobbyIdParser` 타입이 없어 컴파일 실패한다.

- [ ] **3단계: 최소 파서 구현**

`string.IsNullOrWhiteSpace`, `ulong.TryParse`와 0 검사를 순서대로 수행하고 설계에 명시된 오류 문자열을 반환한다.

- [ ] **4단계: Editor 프로젝트 빌드 GREEN 확인**

같은 MSBuild 명령이 오류 없이 성공해야 한다.

---

### 작업 2: 개발 전용 Lobby ID UI와 기존 참가 흐름 연결

**파일:**
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyInviteController.cs`
- 테스트: `Assets/Tests/EditMode~/SteamLobbyInviteControllerDevUiTests.cs`

**인터페이스:**
- 소비: `SteamLobbyIdParser.TryParse(string, out ulong, out string)`
- 생성: `public void CopyCurrentLobbyId()`
- 생성: `public void JoinLobbyByIdInput()`

- [ ] **1단계: 실패하는 구조·행동 테스트 작성**

Reflection 기반 EditMode 테스트로 다음을 검증한다.

```csharp
Assert.That(typeof(SteamLobbyInviteController).GetMethod("CopyCurrentLobbyId"), Is.Not.Null);
Assert.That(typeof(SteamLobbyInviteController).GetMethod("JoinLobbyByIdInput"), Is.Not.Null);
```

개발 UI 생성 테스트는 컨트롤러를 비활성 GameObject에 붙인 뒤 `CreateStatusPanelIfNeeded()`를 호출하고 `LobbyIdInput`, `CopyLobbyIdButton`, `JoinLobbyIdButton` 자식이 생성되는지 검증한다.

- [ ] **2단계: Editor 프로젝트 빌드 또는 EditMode Test Runner로 RED 확인**

예상: 공개 메서드 또는 개발 UI 오브젝트가 없어 실패한다.

- [ ] **3단계: 최소 UI 및 이벤트 구현**

- `TMP_InputField lobbyIdInput` 직렬화 필드를 추가한다.
- `UNITY_EDITOR || DEVELOPMENT_BUILD` 조건에서 기존 상태 패널 아래 개발 도구 패널을 생성한다.
- 복사 시 현재 로비가 없으면 상태만 갱신한다.
- 직접 참가 시 Steam 준비 여부, 파서 결과, `CSteamID.IsValid()`를 순서대로 검사한다.
- 성공 시 기존 `JoinLobby(CSteamID)`를 호출한다.

- [ ] **4단계: 런타임 프로젝트와 Editor 프로젝트 빌드 GREEN 확인**

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp-Editor.csproj /t:Build /p:RestorePackages=false /v:minimal
```

두 프로젝트 모두 오류 없이 성공해야 한다.

---

### 작업 3: Windows 빌드 App ID 자동 배치

**파일:**
- 생성: `Assets/Project/Editor/SteamAppIdBuildPostprocessor.cs`
- 테스트: `Assets/Tests/EditMode~/SteamAppIdBuildPostprocessorTests.cs`

**인터페이스:**
- 생성: `internal static bool IsSupportedTarget(BuildTarget target)`
- 생성: `internal static string GetDestinationPath(string builtPlayerPath)`
- Unity 콜백: `IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)`

- [ ] **1단계: 실패하는 경로 계산 테스트 작성**

```csharp
[Test]
public void GetDestinationPath_WindowsExe_ReturnsSiblingSteamAppId()
{
    string result = SteamAppIdBuildPostprocessor.GetDestinationPath(
        @"C:\Builds\RELIC\DUSTIUM.exe");
    Assert.That(result, Is.EqualTo(@"C:\Builds\RELIC\steam_appid.txt"));
}

[TestCase(BuildTarget.StandaloneWindows, true)]
[TestCase(BuildTarget.StandaloneWindows64, true)]
[TestCase(BuildTarget.StandaloneLinux64, false)]
public void IsSupportedTarget_ReturnsExpected(BuildTarget target, bool expected)
{
    Assert.That(SteamAppIdBuildPostprocessor.IsSupportedTarget(target), Is.EqualTo(expected));
}
```

- [ ] **2단계: Editor 프로젝트 빌드로 RED 확인**

예상: `SteamAppIdBuildPostprocessor` 타입이 없어 실패한다.

- [ ] **3단계: 최소 빌드 후 처리기 구현**

- `Application.dataPath`의 부모에서 원본 `steam_appid.txt`를 찾는다.
- Windows Standalone 빌드에서만 동작한다.
- 대상은 `report.summary.outputPath`의 디렉터리와 `steam_appid.txt`를 결합한다.
- 원본이 없으면 `Debug.LogError`를 남기고 반환한다.
- 원본이 있으면 `File.Copy(source, destination, true)`를 수행하고 결과 경로를 로그로 남긴다.

- [ ] **4단계: Editor 프로젝트 빌드 GREEN 확인**

Editor 프로젝트가 오류 없이 빌드되어야 한다.

---

### 작업 4: Steam 초기화 진단 개선

**파일:**
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyInviteController.cs`
- 테스트: `Assets/Tests/EditMode~/SteamLobbyInitializationDiagnosticsTests.cs`

**인터페이스:**
- 생성: `internal static string GetExpectedSteamAppIdPath()`
- 생성: `internal static string BuildSteamInitFailureMessage(bool isSteamRunning, string expectedAppIdPath)`

- [ ] **1단계: 실패하는 진단 메시지 테스트 작성**

Steam 실행 여부와 예상 파일 경로가 메시지에 포함되는지 검증한다.

```csharp
string message = SteamLobbyInviteController.BuildSteamInitFailureMessage(
    true,
    @"C:\BuildTest\steam_appid.txt");

StringAssert.Contains("Steam running: True", message);
StringAssert.Contains(@"C:\BuildTest\steam_appid.txt", message);
```

- [ ] **2단계: Editor 프로젝트 빌드 또는 테스트로 RED 확인**

예상: 진단 메서드가 없어 실패한다.

- [ ] **3단계: 최소 진단 구현**

- 에디터에서는 프로젝트 루트, 플레이어에서는 `Application.dataPath` 부모를 예상 경로로 사용한다.
- `SteamAPI.Init()`이 `false`이면 짧은 화면 상태와 상세 오류 로그를 분리해 남긴다.
- 로그인 성공/실패를 추측하지 않고 확인 가능한 Steam 실행 여부와 파일 존재 여부만 출력한다.

- [ ] **4단계: 런타임 및 Editor 프로젝트 빌드 GREEN 확인**

두 C# 프로젝트가 오류 없이 성공해야 한다.

---

### 작업 5: 종합 검증

**파일:**
- 검토: 위에서 생성·수정한 모든 파일

- [ ] **1단계: 변경 범위 확인**

```powershell
git status --short
git diff -- Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby Assets/Project/Editor Assets/Tests/EditMode~ AI_Docs
```

사용자의 기존 폰트·렌더러 변경은 건드리지 않았는지 확인한다.

- [ ] **2단계: 정적 프로젝트 빌드**

런타임 및 Editor C# 프로젝트를 순서대로 빌드한다. Unity 에디터가 열려 있으므로 batchmode는 실행하지 않는다.

- [ ] **3단계: 사용자가 Unity에서 확인할 수동 절차 보고**

1. 에디터에서 로비 생성 후 `Copy ID`.
2. 다른 Steam 계정의 Development Build에서 ID 입력 후 `Join ID`.
3. Windows Development Build 완료 후 EXE 옆의 `steam_appid.txt` 확인.
4. Steam 실행 상태에서 EXE 직접 실행 후 계정 이름과 로비 상태 확인.
5. 오버레이는 빌드에서만 최종 확인.

## 자체 검토

- 설계의 포함 범위는 작업 1~4에 모두 대응한다.
- 전투 로직, 로비 검색, 네트워크 패키지는 변경 대상에 없다.
- 모든 신규 동작은 실패 테스트를 먼저 작성하도록 분리했다.
- 문서와 테스트 경로는 프로젝트 규칙을 따른다.
- 커밋과 PR 단계는 포함하지 않았다.

---

### 후속 작업 6: Steam 오버레이 프로세스 생명주기 안정화

**파일:**
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyInviteController.cs`
- 테스트: `Assets/Tests/EditMode~/SteamOverlayLifecycleTests.cs`

**검증 인터페이스:**
- 조기 초기화: `InitializeSteamBeforeSplashScreen()`
- 앱 종료: `ShutdownSteamOnApplicationQuit()`
- 실제 활성 콜백: `OnGameOverlayActivated(GameOverlayActivated_t)`

- [ ] **1단계: 실패하는 조기 초기화 테스트 작성**

Reflection으로 `InitializeSteamBeforeSplashScreen()`에
`RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)`가 지정됐는지 검증한다.

- [ ] **2단계: RED 확인**

예상: 조기 초기화 메서드가 없어 실패한다.

- [ ] **3단계: 프로세스 단위 초기화·종료 구현**

- 정적 소유 상태와 종료 이벤트 등록 상태를 둔다.
- BeforeSplashScreen에서 Steam 실행 여부를 확인하고 `SteamAPI.Init()`을 시도한다.
- Lobby `Awake()`에서는 이미 초기화됐으면 콜백만 등록하고, 실패 상태면 재시도한다.
- `OnDestroy()`에서는 Steam API를 종료하지 않는다.
- `Application.quitting`에서만 소유한 Steam API를 종료한다.

- [ ] **4단계: 실제 오버레이 활성 콜백 구현**

`GameOverlayActivated_t.m_bActive`를 확인해 활성/비활성 로그를 남기고, 활성 시 상태창에 실제 활성화 메시지를 표시한다.

- [ ] **5단계: 런타임과 Editor 프로젝트 컴파일**

Unity 에디터가 열려 있으므로 batchmode는 실행하지 않는다. MSBuild로 두 프로젝트의 컴파일 오류가 없는지 확인한다.
