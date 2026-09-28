# 빈 번역 셀 표시 소유권 수정 설계

## 문제

Play Mode에서 영어로 전환했을 때 `Localization.xlsx`의 `English(en)` 셀이 비어 있는 키가 `Untranslated` 대신 한국어로 표시된다.

추가 조사에서 오브젝트를 비활성화했다가 활성화하면 현재 언어가 정상 적용되는 현상과, 로비의 숫자용 `Erosion_Value`에 문장 키가 런타임 연결되는 현상을 확인했다.

공통 조회 함수 `GameLocalization`은 선택 로케일의 원시 엔트리를 확인하고 빈 값이면 `Untranslated`를 반환한다. 그러나 일부 TMP에는 기존 `LocalizeStringEvent`와 `LocalizedTMPText`가 함께 남아 있어, 언어 변경 시 동일한 TMP를 둘 이상의 컴포넌트가 갱신할 수 있다.

## 설계

- 정적 TMP의 최종 표시 소유자는 `LocalizedTMPText` 하나로 유지한다.
- `LocalizedTMPText`가 활성화될 때 같은 GameObject에 남은 `LocalizeStringEvent`를 비활성화한다.
- 활성화 및 로케일 변경 갱신은 Localization 초기화와 대상 로케일 테이블 준비를 기다린다.
- 비동기 갱신마다 버전을 발급해 이전 로케일 요청이 최신 표시를 덮어쓰지 못하게 한다.
- 에디터의 바인딩 복구는 기존처럼 `LocalizedTMPText`를 구성한 뒤 구형 `LocalizeStringEvent`를 제거한다.
- 선택 로케일 셀이 비어 있으면 `GameLocalization`이 반환한 언어별 미번역 표기를 유지한다.
- 로비의 문장용 `Erosion_Value`는 `ErosionDifficultyCatalogUI`의 명시적 출력 대상으로 사용한다.
- 세 `Erosion_Value`는 런타임 출력이므로 `LocalizationIgnore`를 사용하고 정적 키 자동 연결 대상에서 제외한다.
- 동적 텍스트, 번역 데이터 및 전투 로직은 변경하지 않는다.

## 검증

- 두 로컬라이저가 함께 존재하는 TMP를 활성화했을 때 구형 로컬라이저가 비활성화되는 EditMode 회귀 테스트를 추가한다.
- 최신 비동기 갱신만 적용되는 갱신 버전 테스트를 추가한다.
- 로비의 세 `Erosion_Value`가 동적 출력으로 분류되고 문장 출력 참조가 올바른지 검사한다.
- 영어 빈 셀 정책 테스트와 프로젝트 컴파일을 함께 확인한다.
- 가능한 경우 Unity Test Runner에서 관련 EditMode 테스트를 실행한다.

## 멀티플레이 영향

UI 표시 소유권만 변경하며 전투 상태, Command, Result/Event 및 동기화 데이터에는 영향이 없다.
