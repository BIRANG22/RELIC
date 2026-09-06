# 배틀 메뉴 복구·공유 MenuPanel·Steam 멀티 UI 설계

## 목적

- 전투 행동 도중 숨겨진 `MenuRoot`가 전투 종료 후 맵으로 복귀할 때 다시 활성화되게 한다.
- 로비 씬의 `MenuPanel`을 공용 프리팹으로 만들고 로비와 배틀 씬이 같은 프리팹을 사용하게 한다.
- 게임 실행만으로 Steam 초대 오버레이가 열리거나 Steam 오버레이 초기 알림이 나타나는 흐름을 제거한다.
- 로비의 Steam 멀티 상태·개발 패널은 사용자가 초대 버튼을 누르기 전까지 표시하지 않는다.

## 확인된 원인

### 전투 종료 후 MenuRoot 미복구

`BattleTurnExecutor`는 전투 행동 실행 시 `PlayerHUD_Root`와 `MenuRoot`를 숨긴다. 전투가 그 행동 도중 종료되면 `CanAcceptPlayerInput`이 다시 참이 되지 않으므로 종료 정리에서도 두 루트가 숨겨진 채 유지된다. 이후 보상 흐름이 맵 패널을 활성화하지만, 맵 복귀 경계에는 전투 실행 UI 숨김 상태를 해제하는 호출이 없다.

### Steam 실행 직후 동작

`SteamLobbyInviteController`는 `BeforeSplashScreen`에서 Steam API를 초기화하고 로비의 `Awake()`에서도 초기화를 시도한다. 실제 친구 초대 오버레이를 여는 `SteamFriends.ActivateGameOverlayInviteDialog` 호출은 초대 버튼 흐름 안에 있지만, 선행 Steam 초기화 때문에 게임 실행 시 Steam 오버레이 초기 알림이 나타날 수 있다.

### 멀티 패널 즉시 표시

`SteamLobbyStatusPanel`과 개발 빌드의 `SteamLobbyDevelopmentTools`는 로비 오브젝트의 `Awake()`에서 즉시 생성된다. 사용자의 멀티 진입 의사와 관계없이 패널이 표시되는 구조다.

## 설계

### 1. 맵 복귀 경계에서 전투 UI 복원

`BattleTurnExecutor`에 전투방 종료 후 UI 숨김 상태를 명시적으로 해제하는 공개 메서드를 추가한다. 이 메서드는 내부의 `battleExecutionUiSuppressed`를 해제하고 `PlayerHUD_Root`와 `MenuRoot`를 활성화한다.

`BattleSceneController`가 `BattleMapPanel`을 실제로 여는 공통 경계에서 이 메서드를 호출한다. 이 위치를 사용하면 전투 종료 판정이나 보상 계산과 UI 상태를 섞지 않고, 보상 화면이 표시되는 동안에는 기존 숨김 상태를 유지하며 맵 복귀 순간에만 메뉴를 복원할 수 있다.

### 2. 로비 MenuPanel을 공용 프리팹으로 전환

로비 씬의 현재 `MenuPanel` 계층을 기준으로 `Assets/Project/PrefabsR/MenuPanel.prefab`을 만든다.

- 로비 씬의 로컬 `MenuPanel`을 공용 프리팹 인스턴스로 교체한다.
- 배틀 씬의 기존 로컬 `MenuPanel`을 제거하고 같은 프리팹 인스턴스로 교체한다.
- 기존 `LobbyMenuController`, `LobbyMainPanelKeyboardInputController`, `UIPanelButton`, `BattleMenuEscapeInputController`의 참조와 자동 검색이 새 인스턴스를 가리키게 유지한다.
- 프리팹은 씬 외부 오브젝트를 직접 참조하지 않는 자체 완결 계층으로 유지한다.
- 두 씬 모두 초기 상태는 닫힘(`inactive`)을 유지한다.

### 3. Steam 초기화를 명시적 멀티 진입 시점으로 지연

일반 게임 실행에서는 Steam API를 초기화하지 않는다.

다음 경우에만 Steam 초기화와 콜백 등록을 수행한다.

- 사용자가 초대 버튼을 눌러 `OpenInviteFlow()`를 호출한 경우
- 사용자가 개발용 Lobby ID 참가 기능을 호출한 경우
- Steam이 게임을 `+connect_lobby <LobbyId>` 실행 인자와 함께 시작한 경우

초대 오버레이 호출은 사용자가 시작한 `OpenInviteFlow()`에서 로비 생성 또는 재사용이 완료된 경우에만 허용한다. 콜백 등록과 `GameOverlayActivated_t` 관찰 자체는 오버레이를 여는 동작으로 취급하지 않는다.

이 변경으로 일반 실행 중 Steam 초대를 실시간으로 수신하는 기능은 Steam 초기화 전까지 대기한다. 사용자가 초대 기능을 한 번 사용하거나 Steam 초대 실행 인자로 게임이 시작된 뒤에는 기존 콜백 흐름을 유지한다.

### 4. 멀티 패널 지연 생성

`SteamLobbyStatusPanel`과 `SteamLobbyDevelopmentTools`는 `Awake()`에서 생성하지 않는다. `OpenInviteFlow()` 또는 직접 참가 기능으로 멀티 기능이 명시적으로 시작될 때 한 번만 생성하고 표시한다.

- 초대 버튼을 누르기 전에는 두 패널이 씬 계층에 생성되지 않는다.
- 첫 멀티 진입 후에는 같은 컨트롤러 생명주기 동안 재사용한다.
- 일반 빌드에서는 기존과 같이 개발 도구 패널을 만들지 않는다.
- Steam 초기화 실패 시에도 사용자가 원인을 확인할 수 있도록 상태 패널은 표시한다.

## 데이터 및 책임 경계

- 전투 결과 판정, 보상 계산, 랜덤 처리에는 변경을 가하지 않는다.
- `BattleTurnExecutor`는 전투 실행 UI 표시 상태만 관리한다.
- `BattleSceneController`는 방에서 맵으로 전환되는 시점을 소유하며 UI 복구를 요청한다.
- `SteamLobbyInviteController`는 사용자의 멀티 진입 의도, Steam 초기화, 로비 초대 UI를 관리한다.
- 전투 핵심 상태와 Steam 네트워크 상태 사이에 새 직접 참조를 추가하지 않는다.

## 테스트

테스트는 모두 `Assets/Tests/EditMode~/` 아래에 둔다.

- 전투 UI가 숨겨진 상태에서 맵 복귀용 복원 메서드를 호출하면 `MenuRoot`와 `PlayerHUD_Root`가 활성화되는지 검증한다.
- `BattleSceneController`의 맵 열기 경계가 전투 UI 복원을 호출하는 구조인지 검증한다.
- 로비와 배틀 씬 모두 `MenuPanel.prefab` 인스턴스를 사용하고 로컬 중복 계층이 없는지 검증한다.
- Steam 선행 초기화 어트리뷰트가 제거되었는지 검증한다.
- 컨트롤러 초기화만으로 멀티 패널이 생성되지 않는지 검증한다.
- 명시적 멀티 진입 시 상태 패널과 개발 도구가 생성되는지 검증한다.
- Steam 초대 오버레이 호출 경계가 사용자 초대 흐름에만 남아 있는지 소스 경계 테스트로 검증한다.

Unity 에디터가 열려 있다는 프로젝트 규칙에 따라 Unity batchmode 테스트는 실행하지 않는다. C# 프로젝트 빌드, 테스트 어셈블리 컴파일 가능 여부, 씬·프리팹 YAML 참조, `git diff --check`로 검증한다.

## 완료 조건

- 전투 종료 보상 처리 후 맵 화면에서 `MenuRoot`가 활성화되어 메뉴 버튼을 사용할 수 있다.
- 로비와 배틀 씬의 `MenuPanel`은 동일한 `MenuPanel.prefab` 인스턴스다.
- 일반 실행만으로 Steam API 초기화 또는 초대 오버레이 호출이 일어나지 않는다.
- 로비에서 초대 버튼을 누르기 전에는 두 멀티 패널이 생성되거나 표시되지 않는다.
- 기존 Steam 로비 생성, 직접 참가, 파티 동기화, 배틀 시작 동기화 구조는 명시적 멀티 진입 후 그대로 동작한다.

## 멀티플레이 구조 영향

Steam 초기화 시점을 자동 실행에서 사용자 명시 실행으로 옮긴다. 로비 ID, 파티 권한, 공유 상태, 배틀 시작 명령의 형식과 권한 구조는 변경하지 않는다. 전투 결과 계산과 네트워크 동기화 경계에도 변경이 없다.
