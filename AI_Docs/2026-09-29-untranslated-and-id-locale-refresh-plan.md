# Untranslated 전수 수정 및 ID 기반 표시 갱신 구현 계획

**Spec:** `AI_Docs/2026-09-29-untranslated-and-id-locale-refresh-design.md`

## 전역 제약

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/`에만 작성한다.
- Unity batchmode를 사용하지 않는다.
- 현재 체크아웃과 작업 공간을 사용하며 커밋·Push·PR을 수행하지 않는다.
- 전투 상태와 멀티플레이 동기화 데이터는 변경하지 않는다.

## Task 1: 데이터 및 키 커버리지 실패 테스트

- Event canonical 키, 활성 GameData 영어 셀, 명시적 런타임 키, String Table 동기화 검사를 추가한다.
- 현재 누락 상태에서 기대한 이유로 실패하는지 확인한다.

## Task 2: 워크북과 String Table 보완

- Event 선택지 canonical 키를 완성한다.
- `S_Core_53`~`S_Core_84` 이름·효과 영어 번역을 추가한다.
- 실제 사용하는 누락 UI·이벤트·몬스터 행동 키를 추가한다.
- Unity String Table을 워크북과 다시 동기화한다.

## Task 3: 공통 키와 동적 소유권 정리

- 구형 `relic.rarity.*` 조회를 `RelicRarityUtility`로 통일한다.
- 캐릭터 이름·주사위·보상 동적 TMP의 정적/자동 작성자 충돌을 제거한다.
- 소유권 회귀 테스트를 통과시킨다.

## Task 4: 열린 ID 기반 UI 로케일 갱신

- 테이블 준비 이후 공통 로케일 갱신 신호를 제공한다.
- 보상 슬롯·보상 상세과 주요 기억/유물 ID 기반 presenter가 현재 ID로 다시 표시하도록 연결한다.
- 주사위 버튼과 성공·실패 결과를 키 및 현재 상태 기반으로 갱신한다.
- 로케일 변경 회귀 테스트를 통과시킨다.

## Task 5: 전체 검증

- 관련 EditMode 테스트를 Unity Test Runner에서 실행 가능한 상태로 확인한다.
- 런타임/Editor C# 어셈블리를 빌드한다.
- 워크북·Shared Data·각 locale table 정합성을 독립 검사한다.
- Unity 에디터에서 실제 언어 전환이 필요한 항목은 수동 검증 항목으로 기록한다.
# 후속 작업 추가

1. 로비 호버 안정 키 9개와 각 지원 언어 번역을 Excel 및 Unity String Table에 동기화한다.
2. 호버 공용 TMP와 유물 상점 동적 TMP의 작성자 충돌을 제거한다.
3. 주사위 상태 전환과 버튼 라벨 갱신 순서를 수정한다.
4. 로케일 준비 이벤트를 정적 갱신 이후에 구독자별로 안전하게 배포한다.
5. 소스 회귀 테스트, 데이터 정합성 검사, 런타임·에디터 어셈블리 컴파일을 수행한다.
