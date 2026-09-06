# Lobby View State Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 로비 씬을 로비·캐릭터 선택·거점 상태로 명시적으로 전환하고, 거점에서만 카메라 드래그를 허용하며 기존 월드 배경 두 개를 Canvas 이미지로 옮긴다.

**Architecture:** `LobbyViewStateController`가 화면 표현 상태의 단일 소유자가 되어 직렬화된 UI·이펙트·조명·카메라 입력 참조를 일괄 적용한다. 기존 범용 `UIPanelButton`은 수정하지 않고 로비 씬의 기존 버튼 이벤트만 상태 컨트롤러에 연결한다.

**Tech Stack:** Unity 6, C#, uGUI, NUnit EditMode tests, Unity YAML scene serialization

## Global Constraints

- 작성 문서는 `AI_Docs` 내부에만 둔다.
- 테스트는 `Assets/Tests/EditMode~/` 또는 `Assets/Tests/PlayMode~/` 아래에만 둔다.
- Unity 에디터가 열려 있으므로 batchmode 테스트를 실행하지 않는다.
- 요청 범위 밖의 리팩터링을 하지 않는다.
- 커밋과 PR은 별도 사용자 허락 전에는 실행하지 않는다.
- 이 변경은 로비 화면 표현만 제어하고 전투 상태나 네트워크 구조를 변경하지 않는다.

---

### Task 1: 화면 상태 컨트롤러를 테스트 주도로 추가

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyViewStateController.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyViewStateController.cs.meta`
- Modify: `Assets/Tests/EditMode~/LobbyPositionToggleButtonTests.cs`

**Interfaces:**
- Produces: `LobbyViewState` enum
- Produces: `LobbyViewStateController.CurrentState`
- Produces: `ShowLobby()`, `ShowCharacterSelection()`, `ShowPosition()`, `TogglePosition()`

- [ ] **Step 1: 세 상태와 거점 토글을 검증하는 실패 테스트 작성**

  기존 테스트를 `LobbyViewStateControllerTests`로 교체한다. 테스트용 계층을 만들고 private 직렬화 필드는 `SerializedObject`로 연결한다. `ShowLobby`, `ShowCharacterSelection`, `ShowPosition` 호출 뒤 설계 문서의 상태 표와 모든 `activeSelf` 및 카메라 드래그 `enabled` 값을 각각 검증한다. `TogglePosition`은 `Lobby -> Position -> Lobby`를 검증한다.

- [ ] **Step 2: 컴파일 실패 확인**

  Run: Visual Studio MSBuild로 `Assembly-CSharp-Editor.csproj` 빌드

  Expected: `LobbyViewStateController` 형식이 없어서 실패

- [ ] **Step 3: 최소 상태 컨트롤러 구현**

```csharp
public enum LobbyViewState
{
    Lobby,
    CharacterSelection,
    Position
}

public sealed class LobbyViewStateController : MonoBehaviour
{
    [SerializeField] private GameObject backMain;
    [SerializeField] private GameObject effectLobby;
    [SerializeField] private GameObject effectCharacter;
    [SerializeField] private GameObject lobbyMainPanel;
    [SerializeField] private GameObject characterSettingPanel;
    [SerializeField] private GameObject position;
    [SerializeField] private GameObject positionPanel;
    [SerializeField] private GameObject lobbyDirectionalLight;
    [SerializeField] private GameObject positionDirectionalLight;
    [SerializeField] private HorizontalHubCameraDrag hubCameraDrag;

    public LobbyViewState CurrentState { get; private set; }

    private void Start() => ShowLobby();

    public void ShowLobby() => ApplyState(LobbyViewState.Lobby);
    public void ShowCharacterSelection() => ApplyState(LobbyViewState.CharacterSelection);
    public void ShowPosition() => ApplyState(LobbyViewState.Position);

    public void TogglePosition()
    {
        ApplyState(CurrentState == LobbyViewState.Position
            ? LobbyViewState.Lobby
            : LobbyViewState.Position);
    }

    private void ApplyState(LobbyViewState state)
    {
        CurrentState = state;
        bool lobby = state == LobbyViewState.Lobby;
        bool character = state == LobbyViewState.CharacterSelection;
        bool hub = state == LobbyViewState.Position;

        SetActive(backMain, !hub);
        SetActive(effectLobby, lobby);
        SetActive(effectCharacter, character);
        SetActive(lobbyMainPanel, lobby);
        SetActive(characterSettingPanel, character);
        SetActive(position, hub);
        SetActive(positionPanel, hub);
        SetActive(lobbyDirectionalLight, !hub);
        SetActive(positionDirectionalLight, hub);

        if (hubCameraDrag != null)
            hubCameraDrag.enabled = hub;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
```

- [ ] **Step 4: 에디터 프로젝트 빌드로 테스트 코드 컴파일 확인**

  Run: Visual Studio MSBuild로 `Assembly-CSharp-Editor.csproj` 빌드

  Expected: 빌드 성공, 오류 0개

---

### Task 2: 카메라 드래그 비활성화 정리

**Files:**
- Modify: `Assets/Project/Art/testYDM/position/스크립트/HorizontalHubCameraDrag.cs`
- Modify: `Assets/Tests/EditMode~/LobbyPositionToggleButtonTests.cs`

**Interfaces:**
- Consumes: `LobbyViewStateController`가 `HorizontalHubCameraDrag.enabled`를 상태에 따라 설정
- Produces: 컴포넌트 비활성화 시 진행 중 입력 상태 정리

- [ ] **Step 1: 비활성화 시 런타임 상태가 정리되는 실패 테스트 작성**

  Reflection으로 `isDragging = true`, `snapVelocity = 3f`를 설정하고 컴포넌트를 비활성화한 뒤 두 값이 각각 `false`, `0f`인지 검증한다.

- [ ] **Step 2: 에디터 프로젝트 빌드로 현재 실패 확인**

  Expected: 테스트가 요구하는 `OnDisable` 동작 부재

- [ ] **Step 3: 입력 상태 정리 구현**

```csharp
private void OnDisable()
{
    isDragging = false;
    snapVelocity = 0f;
}
```

- [ ] **Step 4: 에디터 프로젝트 빌드 확인**

  Expected: 빌드 성공, 오류 0개

---

### Task 3: Lobby 씬의 상태 전환 연결

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Delete after replacement: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyPositionToggleButton.cs`
- Delete after replacement: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyPositionToggleButton.cs.meta`

**Interfaces:**
- Consumes: `LobbyViewStateController.ShowLobby()`
- Consumes: `LobbyViewStateController.ShowCharacterSelection()`
- Consumes: `LobbyViewStateController.TogglePosition()`

- [ ] **Step 1: `TestPosition`의 기존 토글 컴포넌트를 상태 컨트롤러로 교체**

  현재 참조인 `Back_Main`, `Effect_Lobby`, `Effect_Char`, `LobbyMainPanel`, `CharacterSettingPanel`, `Position`, 두 Directional Light를 새 컨트롤러에 연결한다. `PositionPanel`의 실제 씬 오브젝트와 Main Camera의 `HorizontalHubCameraDrag`도 연결한다.

- [ ] **Step 2: 기존 버튼 이벤트에 상태 전환 추가**

  캐릭터 설정 진입 버튼의 기존 `UIPanelButton.Execute` 호출은 유지하고 `ShowCharacterSelection` 호출을 추가한다. 캐릭터 설정 뒤로가기의 기존 닫기 호출은 유지하고 `ShowLobby` 호출을 추가한다. `TestPosition` 버튼에는 `TogglePosition`을 연결한다.

- [ ] **Step 3: 사용처가 사라진 기존 토글 스크립트 제거**

  `rg "LobbyPositionToggleButton" Assets` 결과가 설계·계획 문서 외에는 없어야 한다.

- [ ] **Step 4: 씬 YAML 정적 검증**

  각 버튼의 persistent call 대상과 메서드 이름, 상태 컨트롤러의 직렬화 참조, Main Camera 드래그 참조가 모두 유효한 fileID인지 확인한다.

---

### Task 4: 월드 배경을 RenderTexture 기반 Canvas 이미지로 전환

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Lobby.unity`

**Interfaces:**
- Consumes: 기존 `Back_Main`, `Effect_Char` Plane·MeshRenderer·Material 표현
- Produces: Canvas 하위 `Background/Back_Main`, `Background/Effect_Lobby`, `Background/Effect_Char` uGUI RawImage

- [ ] **Step 1: Canvas 아래 `Background` RectTransform 추가**

  Canvas의 첫 번째 자식으로 배치하고 anchor min `(0,0)`, anchor max `(1,1)`, offset 및 size delta `(0,0)`, pivot `(0.5,0.5)`를 사용한다.

- [ ] **Step 2: `Back_Main`을 전용 RenderTexture와 RawImage로 전환**

  원본을 `Back_Main_Source`로 유지하고 전용 레이어·카메라로 `LobbyBackMain.renderTexture`에 렌더링한다. Canvas RawImage는 전체 Stretch와 `raycastTarget = false`를 사용한다.

- [ ] **Step 3: `Effect_Char`를 전용 RenderTexture와 RawImage로 전환**

  원본을 `Effect_Char_Source`로 유지하고 별도 전용 레이어·카메라로 `LobbyEffectChar.renderTexture`에 렌더링한다. Canvas RawImage는 `Back_Main` 다음 형제로 두어 투명 배경 위에 합성한다.

- [ ] **Step 3-1: `Effect_Lobby`를 전용 RenderTexture와 RawImage로 전환**

  원본을 `Effect_Lobby_Source`로 유지하고 별도 전용 레이어·카메라로 `LobbyEffectLobby.renderTexture`에 렌더링한다. Canvas RawImage는 `Back_Main`과 `Effect_Char` 사이에 두어 로비 상태에서만 활성 원본 효과가 합성되게 한다.

- [ ] **Step 4: 상태 컨트롤러 참조 갱신 및 정적 검증**

  상태 컨트롤러가 원본 Source 활성화를 제어하는지, 메인 카메라가 전용 레이어를 제외하는지, 두 RawImage가 각 RenderTexture에 연결됐는지, `Background`가 Canvas의 첫 번째 자식인지 확인한다.

---

### Task 5: 전체 검증

**Files:**
- Verify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyViewStateController.cs`
- Verify: `Assets/Project/Art/testYDM/position/스크립트/HorizontalHubCameraDrag.cs`
- Verify: `Assets/Project/Scenes/YDM/Lobby.unity`
- Verify: `Assets/Tests/EditMode~/LobbyPositionToggleButtonTests.cs`

- [ ] **Step 1: 런타임 프로젝트 빌드**

  Run: 승인된 Visual Studio MSBuild 명령으로 `Assembly-CSharp.csproj` 빌드

  Expected: 빌드 성공, 오류 0개

- [ ] **Step 2: 에디터 프로젝트 빌드**

  Run: 승인된 Visual Studio MSBuild 명령으로 `Assembly-CSharp-Editor.csproj` 빌드

  Expected: 빌드 성공, 오류 0개

- [ ] **Step 3: batchmode를 사용하지 않고 씬 정적 검증**

  `rg`와 Unity YAML fileID 확인으로 상태 컨트롤러, 버튼 이벤트, Canvas 계층, Image 컴포넌트, 카메라 참조를 검증한다.

- [ ] **Step 4: 변경 범위 검토**

  `git diff --check`, `git diff --stat`, `git status --short`를 확인한다. 사용자 기존 변경을 포함하거나 요청 범위 밖 파일을 수정하지 않았는지 검토한다.

- [ ] **Step 5: 커밋 여부 확인**

  사용자에게 커밋 허락을 별도로 받은 경우에만 관련 파일을 스테이징하고 커밋한다. 허락이 없으면 작업 트리에 변경을 남긴 채 완료 보고한다.
