# 데모 타이틀 씬 설계

## 목적

- 정식 `Title` 씬을 변경하지 않고 데모 빌드 전용 진입 화면을 제공한다.
- 로비 씬을 거치지 않고 기존 데모 전투 준비 흐름을 통해 `Battle` 씬으로 이동한다.

## 구성

- 원본 `Title` 씬에서 Main Camera, EventSystem, Background, Canvas 설정을 복사한다.
- Canvas에는 `title/Logo`와 `DemoBattleButton`만 유지한다.
- `DemoBattleButton`을 투명한 전체 화면 클릭 영역으로 확장한다.
- 클릭 이벤트는 기존 `TitleDemoBattleButton.OnClickStartDemo`를 그대로 사용한다.
- Bootstrap 및 Build Settings 연결은 이 작업에서 변경하지 않는다.

## 전투 진입 경계

- UI는 데모 시작 요청만 전달한다.
- 파티와 런타임 데이터 준비는 기존 `DemoBattlePartySetup`과 `LobbyBattleEntryService`가 담당한다.

## 게임 상태 연결

- `DemoTitleState`는 `GameStateType.DemoTitle`에 대응한다.
- 상태 진입 시 `SceneName.DemoTitle`을 통해 데모 타이틀 씬을 로드한다.
- 데모 타이틀에서는 정식 타이틀과 동일한 BGM을 재생한다.
- `GameManager`가 데모 타이틀 상태를 등록하며, Bootstrap의 시작 상태 선택은 씬 인스펙터 설정으로 유지한다.
