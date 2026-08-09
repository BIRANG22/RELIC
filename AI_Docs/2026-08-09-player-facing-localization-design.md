# 플레이어 노출 텍스트 전체 Localization 설계

## 범위

- 중심 씬: `Title`, `Lobby`, `Battle`
- 포함: 씬의 TMP 텍스트, 연결 UI 프리팹, 런타임 경고·확인 문장, 플레이어에게 표시되는 데이터 이름과 설명
- 제외: `DebugBattle`, Console 로그, Inspector 라벨, 오브젝트 이름, 숫자 전용 자리표시자, 즉시 런타임 값으로 덮어쓰는 빈 텍스트

## Key 규칙

- 공통 UI: `common.*`
- 타이틀: `title.*`
- 로비: `lobby.*`
- 전투: `battle.*`
- 데이터 표시명과 설명: `data.<category>.*`
- 문장은 의미 단위 하나를 Key 하나로 관리하고 동적 값은 Smart String 인자로 전달한다.

## 연결 방식

- 고정 TMP 문구는 Unity `LocalizeStringEvent`로 연결한다.
- 코드에서 변경되는 문구는 `GameLocalization` API로 조회한다.
- 씬·프리팹 수정은 Editor 마이그레이션 도구가 SerializedObject/Prefab API로 수행하도록 하며 YAML 문자열을 직접 대량 치환하지 않는다.
- 마이그레이션은 동일한 source 문구와 Key 매핑을 사용하고 반복 실행해도 중복 컴포넌트를 만들지 않는다.

## 번역

- `Localization.xlsx`를 단일 원본으로 유지한다.
- 한국어, 영어, 중국어 간체, 일본어, 스페인어를 모두 채운다.
- 번역 초안은 간결하고 중립적인 게임 UI 문체를 사용한다.
- 고유명사는 원문 표기를 우선하고, 문맥이 불명확한 개발 임시 문구는 감사 목록에 남긴다.

## 검증

- 중복 Key, 빈 번역, 존재하지 않는 Key 참조를 검사한다.
- 대상 씬·프리팹의 고정 플레이어 문구가 Localization 연결 없이 남았는지 감사한다.
- 동적 문구 API와 Smart String 인자 처리를 테스트한다.
- Unity Editor 컴파일 후 각 Locale에서 레이아웃과 폰트 glyph를 수동 확인한다.

## 멀티플레이 경계

표시 문자열만 변경하며 전투 Command, State Change, Result/Event 데이터는 변경하지 않는다. 동기화 데이터에는 번역 결과 대신 안정적인 Key 또는 기존 ID를 사용한다.
