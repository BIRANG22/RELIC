# TutorialPanel CheckText 로컬라이징 연결 설계

## 조사 결과

- `TutorialPanel.prefab`의 `CheckText`에는 `TMP_Text`만 있고 `LocalizedTMPText`가 없다.
- 한국어 원문 `확인`은 `common.confirm`, `event.dice.confirm`, `ui.tutorialpanel.checktext.7e9116d8`에 중복되어 원문만으로 안전한 자동 선택이 불가능하다.
- `ui.tutorialpanel.checktext.7e9116d8`은 다른 언어 번역이 비어 있지만 `common.confirm`은 번역이 준비되어 있다.
- `Localization Manager`의 `전체 검사 및 적용`은 중복 원문 고정 UI를 `ApplyRequiredStaticUiBindings()`에서 명시적으로 연결하는 구조를 이미 사용한다.

## 권장 설계

1. `TutorialPanel/CheckText`를 공용 키 `common.confirm`에 명시적으로 연결한다.
2. `ApplyRequiredStaticUiBindings()`가 `TutorialPanel.prefab`의 `CheckText`를 같은 키로 복구하도록 한다.
3. EditMode 회귀 테스트에서 실제 프리팹을 로드하여 `LocalizedTMPText.LocalizationKey`를 검증한다.

## 영향 범위

- 표시 문자열 연결만 변경하며 전투 상태와 결과 계산에는 영향을 주지 않는다.
- 자동 검사 및 적용을 다시 실행해도 동일한 명시적 연결이 유지된다.
