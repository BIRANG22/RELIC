# Steam 로비 직접 참가 및 개발 빌드 초기화 개선 설계

## 목적

Steam 오버레이 초대를 매번 사용하지 않고도 개발 중 Lobby ID를 복사하고 직접 입력하여 로비에 참가할 수 있게 한다. Windows 빌드 EXE를 직접 실행할 때 `steam_appid.txt` 누락으로 `SteamAPI.Init()`이 실패하는 실수를 방지하고, 실패 시 원인을 구분하기 쉬운 상태 메시지를 제공한다.

## 확인된 원인

- 프로젝트 루트에는 App ID `480`을 담은 `steam_appid.txt`가 있다.
- 확인한 빌드 실행 파일은 `C:\Users\yundo\Desktop\BuildTest\DUSTIUM.exe`에 있다.
- 해당 빌드 폴더에는 `steam_appid.txt`가 없다.
- Unity 에디터에서는 프로젝트 루트 파일을 찾을 수 있어 `Steam ready: BIRANG`과 로비 생성 성공이 기록됐다.
- 따라서 빌드 EXE 직접 실행 시 발생한 초기화 실패의 직접 원인은 빌드 폴더의 App ID 파일 누락이다.
- Steam 오버레이는 Unity 에디터에서 표시되지 않을 수 있으므로 오버레이 동작 자체는 빌드에서 검증한다.

## 범위

### 포함

- 현재 로비 ID를 클립보드로 복사하는 개발용 버튼
- Lobby ID 문자열을 입력해 기존 Steam 로비에 참가하는 개발용 입력창과 버튼
- Lobby ID 입력값의 공백 제거, 숫자 변환, Steam ID 유효성 검증
- Windows 빌드 완료 후 `steam_appid.txt`를 EXE와 같은 폴더로 자동 복사
- Steam 초기화 실패 시 Steam 실행 여부와 예상 App ID 파일 경로를 상태 메시지 및 로그에 표시
- 입력 검증과 빌드 파일 배치 경로 계산에 대한 EditMode 테스트

### 제외

- 게임 내부 Steam 친구 목록 구현
- Steam 오버레이를 Unity 에디터에서 강제로 주입하는 기능
- 네트워크 패키지 또는 서버 권한 구조 추가
- 로비 검색/브라우저, 자동 매치메이킹
- 전투 동기화 로직 변경

## UI 설계

기존 `SteamLobbyStatusPanel` 아래에 개발 도구 영역을 생성한다.

- Lobby ID 입력창
- `Copy ID` 버튼
- `Join ID` 버튼

UI는 `UNITY_EDITOR` 또는 `DEVELOPMENT_BUILD`에서만 생성한다. 일반 출시 빌드에는 개발용 직접 참가 UI가 나타나지 않는다.

`Copy ID`는 현재 로비가 유효할 때만 `GUIUtility.systemCopyBuffer`에 64비트 Lobby ID를 기록한다. 로비가 없으면 클립보드를 변경하지 않고 상태 메시지로 알린다.

`Join ID`는 입력 문자열을 검증한 뒤 기존 `JoinLobby(CSteamID)` 흐름을 사용한다. 빈 값, 숫자가 아닌 값, 0 또는 유효하지 않은 Steam ID는 참가 요청을 보내지 않고 구체적인 상태 메시지를 표시한다.

## 구성 요소

### `SteamLobbyIdParser`

Steamworks 호출과 분리된 순수 입력 검증기다.

```csharp
public static bool TryParse(string input, out ulong lobbyId, out string error)
```

공백을 제거한 10진수 문자열만 허용한다. 성공하면 0이 아닌 `ulong`을 반환한다. 실제 `CSteamID.IsValid()` 검사는 컨트롤러가 Steamworks 타입으로 한 번 더 수행한다.

### `SteamLobbyInviteController`

기존 로비 생성, 초대, 콜백, 멤버 동기화 역할은 유지한다. 개발 도구 UI 참조와 다음 공개 UI 이벤트만 추가한다.

```csharp
public void CopyCurrentLobbyId()
public void JoinLobbyByIdInput()
```

직접 참가는 검증 성공 후 기존 비공개 `JoinLobby(CSteamID)`로 위임하여 초대 수락과 동일한 참가 흐름을 재사용한다.

### `SteamAppIdBuildPostprocessor`

Unity Editor 전용 빌드 후 처리기다. Windows Standalone 빌드에 한해 프로젝트 루트의 `steam_appid.txt`를 빌드 EXE와 같은 폴더에 복사한다.

- 원본이 없으면 빌드를 조용히 성공 처리하지 않고 명확한 오류 로그를 남긴다.
- 대상 파일이 있으면 같은 내용으로 덮어쓴다.
- Windows 외 플랫폼에는 아무 작업도 하지 않는다.

경로 계산은 별도 순수 메서드로 분리해 EditMode 테스트에서 검증한다.

## 데이터 흐름

### 직접 참가

```text
Join ID 클릭
→ 입력 문자열 검증
→ CSteamID 유효성 검증
→ SteamMatchmaking.JoinLobby
→ LobbyEnter_t
→ 멤버 데이터 동기화
→ 파티 런타임 및 화면 갱신
```

### 빌드

```text
Windows Player 빌드 완료
→ 빌드 EXE 경로 확인
→ 프로젝트 루트 steam_appid.txt 확인
→ EXE 폴더로 복사
→ EXE 직접 실행 시 SteamAPI.Init에서 App ID 480 사용
```

## 오류 처리

- Steam 클라이언트 미실행: 기존처럼 별도 메시지를 유지한다.
- `SteamAPI.Init()` 반환값이 `false`: 실행 파일 기준 예상 `steam_appid.txt` 경로와 Steam 실행 상태를 로그에 남긴다.
- Lobby ID 입력 오류: 빈 값, 숫자 형식 오류, 0을 구분한다.
- 현재 로비 없이 복사: 복사를 수행하지 않는다.
- 빌드 후 원본 App ID 파일 누락: 빌드 결과 로그에 오류를 남겨 누락을 즉시 발견할 수 있게 한다.

## 테스트 전략

테스트는 모두 `Assets/Tests/EditMode~/` 아래에 둔다.

- 앞뒤 공백이 있는 유효 Lobby ID를 파싱한다.
- 빈 문자열을 거부한다.
- 숫자가 아닌 문자열을 거부한다.
- `0`을 거부한다.
- Windows EXE 경로에서 같은 폴더의 `steam_appid.txt` 대상 경로를 계산한다.
- 다른 플랫폼 대상에서는 자동 복사 대상이 아님을 판정한다.

Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않는다. EditMode 테스트 소스의 컴파일과 프로젝트 C# 빌드를 우선 확인하고, 사용자가 Unity Test Runner에서 테스트할 수 있도록 테스트 이름을 보고한다.

## 멀티플레이 경계

이번 변경은 Steam 로비 접속 진입점과 개발 편의 UI에만 영향을 준다. 전투 상태, 랜덤, Command/State Change/Event 흐름은 변경하지 않는다. Lobby ID라는 안정적인 Steam 식별자를 사용하며 Scene Object 참조를 네트워크 상태로 전달하지 않는다.

## 완료 조건

- 에디터 또는 Development Build에서 현재 Lobby ID를 복사할 수 있다.
- 다른 Steam 계정의 클라이언트가 복사한 Lobby ID를 입력해 참가 요청을 보낼 수 있다.
- Windows 빌드 후 EXE 옆에 `steam_appid.txt`가 자동 생성된다.
- 빌드 EXE 직접 실행에서 App ID 파일 누락으로 인한 초기화 실패가 재발하지 않는다.
- 일반 출시 빌드에는 직접 참가 개발 UI가 노출되지 않는다.

## 후속 수정: Steam 오버레이 재실행 안정화

### 확인된 현상과 원인

- 빌드 로그에서 Steam 계정 연결, 로비 생성, 초대 오버레이 호출은 성공했다.
- 창을 닫은 직후에도 이전 `DUSTIUM.exe` 프로세스가 Unity 종료 정리를 끝낼 때까지 잠시 남았다.
- Steam API 초기화가 Lobby 씬의 `Awake()`에서 수행되어 D3D 렌더러보다 늦다.
- `SteamLobbyInviteController.OnDestroy()`가 `SteamAPI.Shutdown()`을 호출하므로 Steam API 수명이 앱이 아니라 Lobby 씬 오브젝트에 묶여 있다.

### 변경 설계

- `RuntimeInitializeOnLoadType.BeforeSplashScreen`에서 Steam API 초기화를 먼저 시도한다.
- 조기 초기화가 환경 문제로 실패한 경우 Lobby 컨트롤러가 기존처럼 한 번 재시도한다.
- Steam API는 프로세스에서 한 번만 소유하며 Lobby 씬 오브젝트 파괴 시 종료하지 않는다.
- `Application.quitting`에서만 소유한 Steam API를 한 번 종료한다.
- `GameOverlayActivated_t` 콜백을 등록하여 실제 오버레이 활성화와 비활성화를 로그에 기록한다.
- 화면의 초대 메시지는 API 호출 요청과 실제 오버레이 활성화를 구분한다.

### 완료 조건

- Steam 초기화 메서드가 `BeforeSplashScreen` 단계에 등록된다.
- Lobby 씬 오브젝트가 파괴되어도 Steam API가 종료되지 않는다.
- 앱 종료 시 소유한 Steam API가 한 번 종료된다.
- 오버레이가 실제 활성화되면 `GameOverlayActivated_t` 로그가 남는다.
