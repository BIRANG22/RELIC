# DebugBattle 스킬 시전 테스트와 VFX 검색 최적화 설계

## 배경

DebugBattle에서 스킬 애니메이션과 스킬 고유 VFX를 확인하려면 VFX 프리팹만 직접 실행하는 방식으로는 부족하다. 실제 배틀과 같은 결과를 보려면 플레이어 스킬 커맨드를 예약하고 `BattleTurnExecutor`가 기존 전투 실행 루틴을 처리해야 한다.

또한 `Test_VFX` 워크벤치의 프리팹 검색은 입력 중에도 전체 프리팹 목록을 매 GUI 갱신마다 필터링하고 버튼으로 그려 검색 글자가 늦게 반영된다.

## 권장 구조

1. `TestVfxWorkbench` 검색 최적화
   - 프리팹 발견 목록은 기존처럼 수동 Refresh 또는 Awake에서만 갱신한다.
   - 검색어가 바뀐 경우에만 필터 결과 인덱스를 다시 만든다.
   - 화면에 그리는 검색 결과 수를 제한하고 전체 매칭 수를 표시한다.
   - 프리팹 로드는 목록 갱신 시에만 수행하고, 검색 입력 중에는 문자열 비교만 수행한다.

2. DebugBattle 실제 스킬 시전 테스트
   - `BattleEffectDebugWindow`에 스킬 시전 테스트 섹션을 추가한다.
   - 선택된 파티 캐릭터를 우선 사용하되, 자동 스킬 소유자 선택 옵션이 켜져 있으면 해당 SkillId를 장착한 파티 캐릭터를 찾아 `PlayerReservedCommand`를 만든다.
   - 선택형 스킬은 디버그 허수아비 그리드를 우선 타겟으로 삼는다. 디버그 반복 검증을 위해 강제 타겟 옵션을 제공하고, 옵션이 꺼져 있으면 유효한 선택 그리드 안에서만 타겟을 고른다.
   - 방향형/전체형/즉시형은 기존 `BattleRangeCalculator`와 `BattleTimelineController`의 preview API를 사용해 커맨드를 채운다.
   - 예약 성공 시 `BattleTurnExecutor.ExecuteTurn()`을 호출해 실제 전투 애니메이션, 스킬 VFX, 피격 이펙트 흐름을 그대로 태운다.

## 멀티플레이 경계

- 디버그 도구는 전투 결과를 직접 계산하지 않고 기존 `PlayerReservedCommand -> BattleTimelineController -> BattleTurnExecutor` 흐름만 사용한다.
- 새 헬퍼는 DebugBattle 테스트 편의용이며, 실전 네트워크 동기화 구조나 전투 핵심 판정 로직에는 새 의존성을 추가하지 않는다.

## 검증 계획

- EditMode 테스트로 VFX 검색 필터 캐시 동작과 결과 제한을 검증한다.
- EditMode 테스트로 DebugBattle 스킬 커맨드 생성이 `S_Ability_11` 같은 Selection 스킬의 타겟/범위를 채우는지 검증한다.
- Unity batchmode 테스트는 프로젝트 규칙상 실행하지 않고, MSBuild 컴파일과 `git diff --check`로 정적 검증한다.
