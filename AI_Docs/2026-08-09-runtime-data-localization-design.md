+# 런타임 데이터 및 툴팁 전체 Localization 설계

## 목표

Title, Lobby, Battle에서 플레이어에게 표시될 수 있는 런타임 생성 문자열을 모두 Localization String Table을 통해 표시한다. Console 로그, 디버그 전용 UI, 내부 ID, 오브젝트 이름은 제외한다.

## 범위

- `GameData.xlsx` 전체 시트 중 표시 문자열 컬럼:
  - Character, Monster, SkillMaster, MonsterSkill, MonsterPatternInfo
  - Rune, GridEffect, Effect, Map, BattleMap, SkillRange, Relic, Item
  - EventMaster, EventChoice, Quest
- 코드에서 조합되는 툴팁, 상태이상, 희귀도, 자원, 범위, 보상, 경고 및 확인 문장
- 현재 데이터뿐 아니라 향후 UI에서 표시될 가능성이 있는 비어 있지 않은 데이터 행 전체
- 제외: DebugBattle, Console 로그, 개발자 오류 진단, Inspector 라벨, 안정적인 ID와 숫자만 있는 출력

## 키 규칙

- 데이터 키: `data.<sheet>.<stable-id>.<field>`
- 공통 런타임 어휘: `runtime.common.*`
- 로비 조합 문장: `runtime.lobby.*`
- 전투 조합 문장: `runtime.battle.*`
- 데이터 키에는 한국어 원문이 아니라 CharacterId, SkillId, MonsterId 같은 안정적인 ID를 사용한다.
- 키 구성요소는 소문자 snake_case로 정규화하며 중복 발생 시 importer가 오류를 보고한다.

## 데이터 구조

`GameData.xlsx`는 게임 수치와 한국어 원문을 계속 보유한다. `Localization.xlsx`는 모든 언어 번역의 단일 원본이다. GameData importer가 데이터 객체를 만들 때 안정적인 ID는 그대로 유지하고, 화면 표시 시 `GameLocalization`이 ID와 필드로 Localization 키를 구성해 번역을 조회한다.

원본 한국어는 누락 키나 초기화 실패 시의 fallback으로 사용한다. 저장 데이터와 멀티플레이 스냅샷에는 번역된 문자열을 저장하지 않고 기존 ID만 저장한다.

## 런타임 API

- `GameLocalization.Get(key, fallback, args)`: 일반 키 조회
- `GameLocalization.GetData(category, stableId, field, fallback)`: 데이터 표시 문자열 조회
- `GameLocalization.Format(key, fallback, args)`: 숫자와 이름이 들어가는 문장 포맷
- 키가 없거나 Localization 초기화가 실패하면 한국어 원문을 반환한다.
- Locale 변경 후 새로 열린 툴팁은 즉시 선택 언어를 사용한다. 열린 채 유지되는 패널은 Locale 변경 이벤트에서 refresh한다.

## 연결 전략

데이터 모델 자체의 Name/Description을 번역 문자열로 덮어쓰지 않는다. UI 표현 계층 또는 공용 formatter에서 번역한다. 반복되는 스킬 설명 우선순위와 상태 효과 표시명은 각각 공용 formatter 한 곳으로 모아 동일 데이터가 모든 UI에서 같은 번역을 사용하게 한다.

## Excel 생성 및 번역

`GameData.xlsx`에서 대상 문자열을 추출하여 `Localization.xlsx`의 Text 시트에 병합한다. 기존 109개 키와 번역은 보존한다. 한국어, 영어, 중국어 간체, 일본어, 스페인어를 모두 채우며 줄바꿈과 `{0}` 형식 인자는 동일하게 유지한다.

## 검증

- 데이터 ID/필드로 생성된 키의 중복 및 누락 검사
- 5개 언어의 키 수와 빈 셀 검사
- 모든 `GameLocalization.GetData` 및 `Format` 참조가 테이블에 존재하는지 검사
- UI에 직접 전달되는 데이터 Name/Desc/Tooltip 경로 감사
- MSBuild 컴파일
- EditMode 테스트 소스 컴파일 및 Unity Test Runner 수동 실행 대상 제공

## 멀티플레이 경계

번역은 표현 계층에서만 수행한다. Command, State Change, Result/Event, 저장 데이터, 네트워크 스냅샷에는 기존 안정 ID와 원본 수치만 사용하므로 전투 판정과 동기화 결과에 영향을 주지 않는다.

