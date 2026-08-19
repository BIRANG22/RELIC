# Localization Editing Lock 설계

## 문제

`LocalizeStringEvent`는 `ExecuteAlways` 컴포넌트이며 에디터 `OnValidate`에서 `RefreshString`을 호출한다. 그래서 Localization Scene Controls의 Active Locale이 `None`이어도, TMP 텍스트를 씬에서 직접 수정하면 연결된 String Table 값으로 다시 덮일 수 있다.

## 목표

씬과 대상 UI 프리팹의 텍스트 원문을 편집할 때만 `LocalizeStringEvent`를 일괄 비활성화하고, 편집이 끝나면 다시 일괄 활성화할 수 있는 에디터 메뉴를 제공한다.

## 설계

- `Tools/Localization/Disable Text Localization Editing Lock` 메뉴는 대상 씬과 `Assets/Project/PrefabsR` 프리팹의 `LocalizeStringEvent.enabled`를 `false`로 바꾼다.
- `Tools/Localization/Enable Text Localization Editing Lock` 메뉴는 같은 범위의 `LocalizeStringEvent.enabled`를 `true`로 되돌린다.
- 열린 씬에 저장되지 않은 변경이 있으면 기존 마이그레이션 도구처럼 사용자 저장 확인을 거친다.
- 런타임 번역 구조, Excel import, String Table 값은 변경하지 않는다.
- 핵심 토글 로직은 계층 단위 메서드로 분리해 EditMode 테스트가 검증할 수 있게 한다.

## 검증

- EditMode 테스트로 계층 내 활성/비활성 자식의 `LocalizeStringEvent`가 일괄 토글되는지 확인한다.
- EditMode 테스트로 이미 목표 상태인 컴포넌트는 변경 개수에 포함하지 않는지 확인한다.
- MSBuild로 Editor 및 런타임 어셈블리 컴파일을 확인한다.

## 멀티플레이 영향

표시용 에디터 도구만 추가한다. 전투 Command, State Change, Result/Event, 네트워크 동기화 데이터에는 영향을 주지 않는다.
