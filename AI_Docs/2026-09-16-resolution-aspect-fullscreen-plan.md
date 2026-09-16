# Resolution Aspect and Fullscreen Implementation Plan

1. `ResolutionManager`에 전체화면 저장·복원·전환 API를 추가하고 해상도 변경 시 모드를 유지한다.
2. `OptionPanelUI`에 `FullscreenToggle` 참조와 상태 동기화/이벤트 처리를 추가한다.
3. `Option.prefab`의 해상도 행 우측에 전체화면 토글과 라벨을 배치하고 직렬화한다.
4. EditMode 테스트를 추가하고 컴파일·프리팹 참조 무결성을 검증한다.
