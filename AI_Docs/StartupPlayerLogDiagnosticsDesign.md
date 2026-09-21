# Player.log 시작 진단 설계

## 목적

제출 빌드가 시작 또는 씬 전환 중 멈출 때, Windows `Player.log`의 마지막 체크포인트만으로 정지 구간을 식별한다.

## 범위

- 프로세스 시작 시 제품/버전/빌드 GUID/Unity/플랫폼/초기 씬 정보를 한 줄로 기록한다.
- 시작 단계는 증가하는 순번과 `[Startup]` 태그를 사용해 `BEGIN`, `OK`, `FAIL`로 기록한다.
- Bootstrap의 설정, 세이브, 이벤트 버스, 데이터, 오디오, 입력, UI, 게임 매니저, 로컬라이징 초기화를 기록한다.
- 게임 상태 변경과 씬 비동기 로드의 주요 경계(전환 닫기, 로드 시작/완료, 전환 열기)를 기록한다.
- 처리되지 않은 AppDomain 예외와 관찰되지 않은 Task 예외에는 마지막 체크포인트를 함께 기록한다.
- 첫 상태 진입이 끝나면 `STARTUP COMPLETE`를 기록한다.

## 설계

`StartupDiagnostics` 정적 클래스를 런타임 시작 전에 초기화한다. 로그 출력은 Unity의 `Debug.Log` 계열만 사용하여 별도 파일 I/O 없이 기존 `Player.log`에 남긴다. 마지막 체크포인트는 문자열로만 보관하며 게임 상태에는 관여하지 않는다.

예상 로그 형식:

```text
[Startup] #001 BOOT product=DUSTIUM version=... buildGuid=... unity=... platform=... scene=Bootstrap
[Startup] #002 BEGIN Bootstrap.Data
[Startup] #003 OK Bootstrap.Data
[Startup] #004 BEGIN Scene.Load target=Title current=Bootstrap
[Startup] #005 OK Scene.Load target=Title active=Title
[Startup] #006 STARTUP COMPLETE state=Title scene=Title
```

## 오류 처리

- `Fail(step, exception)`은 단계명, 마지막 체크포인트, 예외 전체 내용을 오류 로그로 남긴다.
- 상태 변경과 씬 로드는 예외를 기록한 뒤 다시 던져 기존 호출자의 실패 동작을 보존한다.
- 전역 예외 핸들러 안에서는 예외를 한 번만 기록하고, Unity 로그 콜백을 재로깅하지 않아 재귀를 방지한다.

## 비기능 영향

진단 코드는 전투 데이터와 상태를 변경하지 않는다. 로그 문자열과 순번만 보관하므로 멀티플레이 동기화 및 결정론에 영향이 없다.
