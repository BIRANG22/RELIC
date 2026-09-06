# 거점 캐릭터 세팅 진입 구현 계획

> **작업 지침:** 구현은 현재 세션에서 단계별로 수행한다. 새 테스트가 필요하면 `Assets/Tests/EditMode~/`에만 작성하며 Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않는다.

**목표:** `Cha1~3`이 슬롯을 선택하지 않고 캐릭터 세팅 화면을 열며, 캐릭터 세팅 화면에서 거점으로 복귀하게 한다.

**구조:** 거점 캐릭터용 진입 컴포넌트는 화면 전환만 담당한다. 기존 패널 전환 컴포넌트의 복귀 대상은 `Position`으로 바꾸며 파티 런타임 데이터와 `CharacterSelectionState.CurrentPartySlotIndex`는 건드리지 않는다.

**기술:** Unity 6, C#, Unity UI/EventSystem

## 공통 제약

- 문서는 `AI_Docs` 안에만 작성한다.
- `Cha1~3` 클릭은 파티 슬롯 인덱스와 캐릭터 ID를 전달하지 않는다.
- 전투 및 파티 런타임 데이터를 직접 변경하지 않는다.
- Unity batchmode 테스트는 실행하지 않는다.
- 커밋은 사용자에게 별도 허락받기 전에는 생성하지 않는다.

---

### 작업 1: 거점 캐릭터 세팅 진입 컴포넌트

**파일:**

- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/PositionCharacterSettingButton.cs`
- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/PositionCharacterSettingButton.cs.meta`
- 테스트: `Assets/Tests/EditMode~/PositionCharacterSettingButtonTests.cs`

**인터페이스:**

- 입력: Unity 버튼 또는 포인터 클릭에서 호출하는 `Execute()`
- 출력: `LobbyViewStateController.ShowCharacterSelection()` 호출
- 파티 슬롯 관련 입력과 출력은 없음

- [ ] 슬롯 인덱스를 사용하지 않고 캐릭터 세팅 상태가 열리는 테스트를 작성한다.
- [ ] 테스트가 새 컴포넌트 부재로 실패하는 상태를 확인한다. 프로젝트 규칙상 Unity batchmode는 실행하지 않으므로 에디터에서 실행할 테스트를 남긴다.
- [ ] `PositionCharacterSettingButton.Execute()`를 최소 구현한다. 직렬화된 컨트롤러가 없으면 씬에서 자동 탐색하고, 그래도 없으면 경고 후 종료한다.
- [ ] `Cha1~3`의 저장된 씬 오브젝트를 찾을 수 있으면 컴포넌트와 클릭 이벤트를 연결한다.

### 작업 2: 캐릭터 세팅 복귀 대상을 거점으로 변경

**파일:**

- 수정: `Assets/Project/Scripts/UI/LobbyPanelTransitionButton.cs`
- 테스트: `Assets/Tests/EditMode~/LobbyPanelTransitionButtonTests.cs`

**인터페이스:**

- 입력: `PanelTransitionMode.CharacterToLobby`의 기존 복귀 실행
- 출력: `LobbyViewStateController.ShowPosition()` 호출

- [ ] 캐릭터 세팅 복귀가 `Position` 상태를 적용하는 테스트를 작성한다.
- [ ] 기존 구현이 `Lobby` 상태를 적용하여 실패하는 조건을 확인한다. 프로젝트 규칙상 Unity batchmode는 실행하지 않으므로 에디터에서 실행할 테스트를 남긴다.
- [ ] `InvokeAfterPanelChange()`의 `CharacterToLobby` 분기 호출을 `ShowPosition()`으로 변경한다.
- [ ] 기존 씬 버튼 연결을 유지하여 별도의 Unity 이벤트 재연결이 필요하지 않게 한다.

### 작업 3: 정적 검증과 수동 검증 항목 전달

**파일:**

- 검증: `Assembly-CSharp.csproj`
- 확인: `Assets/Project/Scenes/YDM/Lobby.unity`

- [ ] `Assembly-CSharp.csproj`를 빌드하여 C# 컴파일 오류가 없는지 확인한다.
- [ ] 씬 YAML에서 `Cha1~3` 저장 여부와 버튼 연결 여부를 확인한다.
- [ ] 씬 오브젝트가 저장되어 있지 않다면 Unity 에디터에서 사용자가 연결할 정확한 컴포넌트와 함수명을 안내한다.
- [ ] 플레이 모드에서 `Cha1`, `Cha2`, `Cha3` 각각의 진입과 캐릭터 세팅 복귀 동작을 확인할 수동 체크리스트를 전달한다.
