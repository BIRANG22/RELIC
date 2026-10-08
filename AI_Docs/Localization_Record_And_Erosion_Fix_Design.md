# 기록서 데이터 동기화 및 침식 툴팁 로컬리제이션 수정 설계

## 목표

- 로비 `TooltipPanel/ErosionText`가 빠른 선택 변경 중 이전 번역으로 되돌아가는 현상을 제거한다.
- Localization Manager의 `전체 검사 및 적용`이 최신 `GameData.xlsx`를 런타임 데이터에도 반영하게 한다.
- 기록서의 Skill/Rune/Relic 로컬리제이션 키 누락을 검사하고, 최신 수치 데이터로 설명 토큰을 치환한다.

## 원인

1. `ErosionText`에 붙은 `LocalizationIgnore`는 자동 바인딩 제외 표식이 아니다. 런타임 자동 바인더는 이 표식을 동적 텍스트로 해석해 `DynamicLocalizedTMPText`를 연결하며, 비동기 완료 시 카탈로그 UI가 쓴 최신 텍스트를 덮어쓸 수 있다.
2. Localization Manager는 `GameData.xlsx`에서 키를 수집하지만 `GameDataRuntime.csv`를 갱신하지 않는다. 기록서는 런타임 CSV에서 데이터 수치를 읽으므로 최신 번역 템플릿과 오래된 수치가 섞인다.
3. `S_Unique_05`는 Excel에서 `ValueRate=1;2`지만 현재 런타임 CSV에는 과거 데이터인 `ValueRate=2`만 남아 있어 `{ValueRate2}`를 치환할 수 없다.

## 설계

- 카탈로그가 직접 갱신하는 침식 텍스트에는 `LocalizationAutoBindingIgnore`를 부착하고, 기존 `LocalizedTMPText`, `DynamicLocalizedTMPText`, `LocalizeStringEvent`를 비활성화한다.
- Excel 변환기의 핵심 작업을 실패를 호출자에게 전달하는 동기 API로 노출한다. 기존 메뉴와 Localization Manager가 같은 API를 사용한다.
- `전체 검사 및 적용`이 GameData 후보를 반영한 뒤 Excel→Runtime CSV 동기화를 실행한다. 변환 실패 시 성공 로그를 남기지 않고 적용 자체를 실패 처리한다.
- Skill/Rune/Relic의 사용자 표시 필드가 Localization.xlsx에 모두 존재하는지 회귀 테스트로 고정한다.
- 토큰을 임의 기본값으로 숨기지 않는다. 최신 Excel의 다중 수치가 런타임 CSV에 전달되는 것으로 `ValueRate2` 문제를 해결한다.

## 멀티플레이 경계

전투 상태·판정·랜덤에는 영향을 주지 않는다. 에디터 데이터 동기화와 표시 UI 소유권만 변경한다.
