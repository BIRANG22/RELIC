# Discord Rich Presence 연동 설계

## 목표

Discord Social SDK 1.10.18247을 RELIC에 연결하고, 별도 Discord 로그인 없이 데스크톱 Discord RPC로 현재 플레이 상태를 표시한다.

표시 범위는 다음과 같다.

- 애플리케이션: `RELIC 플레이 중`
- 현재 위치: 타이틀, 로비 또는 현재 챕터·스테이지·맵
- 현재 캐릭터: 파티 슬롯에 편성된 캐릭터 이름
- 플레이 시간: 게임 실행 후 경과 시간
- 연결 상태: 초기화 중, 사용 가능, Discord 미실행, SDK 오류

## 접근 방식 비교

### 채택: Direct Rich Presence

`Client.SetApplicationId(1533104947875549325)` 후 `UpdateRichPresence`를 호출한다. OAuth 로그인 UI와 토큰 저장이 필요 없고 현재 요구 범위를 가장 작게 충족한다. Discord 데스크톱 앱이 실행 중일 때만 동작한다.

### 보류: OAuth 계정 연결

친구 목록, 메시지, 음성 기능 확장에는 적합하지만 로그인 UI, PKCE, 토큰 수명 관리가 필요해 현재 범위를 벗어난다.

### 제외: 구형 Discord RPC/GameSDK

현재 권장 Social SDK가 아니므로 신규 연동에 사용하지 않는다.

## 구조

### `DiscordPresenceSnapshot`

SDK와 무관한 불변 표시 모델이다. details, state, Unix 시작 시각을 보유한다.

### `DiscordPresenceSnapshotBuilder`

씬 이름, `MapRuntimeData`, `PartyRuntimeStore`, `CharacterDatabase`를 읽어 표시 문자열을 만든다. SDK 호출과 분리하여 EditMode 테스트가 가능하게 한다.

### `DiscordRichPresenceService`

런타임 시작 시 한 번 자동 생성되는 `DontDestroyOnLoad` 서비스다. Discord SDK Client를 소유하고 다음 시점에 Presence를 갱신한다.

- 서비스 초기화 직후
- 활성 씬 변경 직후
- 5초 주기

마지막 갱신 콜백 결과로 연결 상태를 기록하고 로그를 남긴다. SDK 또는 Discord가 없어 실패해도 게임 흐름에는 영향을 주지 않는다. 종료 시 Presence를 지우고 SDK 자원을 해제한다.

### `DiscordPresenceStatus`

`Initializing`, `Ready`, `Unavailable`, `Error` 상태를 제공한다. Direct RPC는 OAuth `Client.Status.Ready`를 사용하지 않으므로 `UpdateRichPresence` 성공 여부를 실제 연결 기준으로 삼는다.

## 표시 규칙

- 타이틀: details `메인 메뉴`, state `모험 준비 중`
- 로비: details `로비`, state `캐릭터: {편성 이름}`
- 진행 중: details `{챕터} · {스테이지 또는 맵}`, state `캐릭터: {편성 이름}`
- 캐릭터가 없으면 state `파티 편성 중`
- 캐릭터 이름을 찾지 못하면 안정적인 `CharacterId`를 대체 표시한다.
- 시작 Unix timestamp는 서비스 생성 시 한 번 정하고 Presence 갱신 때 유지한다.

## 오류 처리

- Application ID가 0이면 SDK 호출 없이 Error 상태와 명확한 로그를 남긴다.
- Discord 데스크톱 앱 미실행 등 갱신 실패는 Unavailable로 기록하고 다음 주기에 재시도한다.
- 예외는 서비스 경계에서 포착하여 게임 상태와 Steam 멀티플레이에 전파하지 않는다.
- 액세스 토큰, Client Secret은 사용하거나 저장하지 않는다.

## 검증

- EditMode 테스트로 씬/맵/캐릭터 표시 조합과 fallback을 검증한다.
- 프로젝트 C# 컴파일로 Social SDK API 및 네이티브 플러그인 참조를 확인한다.
- Unity 에디터가 열려 있다는 프로젝트 규칙에 따라 batchmode 테스트는 실행하지 않는다.
- 실제 Discord 표시 확인은 실행 중인 에디터와 Discord 데스크톱 클라이언트에서 수동 확인 항목으로 남긴다.

## 멀티플레이 경계

이 기능은 `DataManager`와 Steam 로비 상태를 읽기만 하는 표현 계층이다. 전투 명령, 상태 변경, 결과 이벤트 또는 Steam 네트워크 동기화에는 관여하지 않는다.
