# 시작 검정 화면 멈춤 대응

## 목적

Windows Standalone에서 회사 로고 뒤 검정 화면이 유지되고 운영체제가 응답 없음으로 표시할 수 있는 시작 경로를 제거한다.

## 원인

Bootstrap 씬에서 상태 머신의 비동기 Title 전환과 `BootstrapToTitleTransitionLoader`의 직접 동기 Title 전환이 동시에 활성화되어 있다. 후자는 검정 캔버스를 숨긴 뒤 `SceneManager.LoadScene`을 호출한다. 또한 Discord Rich Presence가 첫 씬 이전에 즉시 네이티브 호출을 수행한다.

## 설계

1. Bootstrap의 Title 전환은 `Bootstrap -> GameStateMachine -> SceneFlowManager.LoadSceneAsync`만 사용한다.
2. Bootstrap 씬에서 직접 Title을 여는 전환 컴포넌트를 비활성화한다. 기존 컴포넌트와 에셋은 삭제하지 않아 되돌릴 수 있다.
3. Discord Rich Presence는 게임 시작 직후에는 클라이언트만 준비하고, 첫 씬 전환 이후에 첫 전송을 시도한다. 이 단계의 예외는 기존처럼 비활성화 처리하며 게임 시작을 막지 않는다.
4. Bootstrap과 SceneFlow에 시작 단계 로그를 남겨, 이후 Player.log만으로 데이터 초기화·로컬라이제이션·Title 로드 중 어느 단계에서 멈췄는지 판별한다.

## 검증

- Bootstrap 씬에 직접 전환기가 활성화되어 있지 않은지 EditMode 회귀 검사로 확인한다.
- C# 컴파일과 씬 직렬화 검사를 실행한다.
- Unity 에디터가 열려 있으면 Test Runner에서 EditMode 검사를 실행한다. batchmode는 사용하지 않는다.
