# 프리팹 편집 시 Localization null 로케일 오류 방지 설계

## 문제

프리팹을 Prefab Stage에서 열면 TMP 프리뷰 렌더링이 `TEXT_CHANGED_EVENT`를 발생시킨다. 런타임 전용 `RuntimeTMPTextAutoLocalizer`와 `RuntimeLocalizationMissingRegistry`가 편집 상태의 이벤트까지 처리하면서, `SelectedLocale`이 없는 상태로 String Database를 조회해 연속 예외가 발생한다.

확인된 호출 경로는 다음과 같다.

`TMP 프리뷰 → RuntimeTMPTextAutoLocalizer → DynamicLocalizedTMPText/LocalizedTMPText → GameLocalization.GetForLocale → StringDatabase(locale: null)`

## 설계

- 런타임 TMP 자동 처리 정책을 하나의 메서드로 명시하고 Play Mode가 아닐 때 처리하지 않는다.
- 자동 로컬라이저와 런타임 누락 감지기가 동일한 정책을 사용한다.
- `GameLocalization.GetForLocale`은 locale이 null이면 String Database를 조회하지 않고 전달받은 fallback을 반환한다.
- 정적·동적 TMP의 비동기 갱신과 전체 갱신 코디네이터도 초기화 후 locale이 null이면 비동기 테이블 요청을 건너뛴다.
- 개별 프리팹의 UI 구성이나 로컬라이제이션 키는 변경하지 않는다.

## 검증

- EditMode 테스트로 비 Play Mode 콜백 차단과 null locale fallback을 검증한다.
- 런타임 및 에디터 어셈블리 컴파일을 확인한다.
- 프로젝트 규칙에 따라 batchmode 테스트는 실행하지 않는다.

## 멀티플레이 영향

로컬 UI 문자열 조회 시점의 안전성만 변경하므로 전투 상태와 네트워크 동기화에는 영향이 없다.
