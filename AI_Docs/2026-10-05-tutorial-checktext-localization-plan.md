# TutorialPanel CheckText 로컬라이징 구현 계획

1. 실제 `TutorialPanel.prefab`을 검사하는 EditMode 회귀 테스트를 추가하고 현재 실패를 확인한다.
2. `ApplyRequiredStaticUiBindings()`에 `TutorialPanel/CheckText -> common.confirm` 규칙을 추가한다.
3. `TutorialPanel.prefab`의 `CheckText`에 `LocalizedTMPText`를 연결한다.
4. 컴파일, 정적 프리팹 검사 및 가능한 테스트 범위를 검증한다.
