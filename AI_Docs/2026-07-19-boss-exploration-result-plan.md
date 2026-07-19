# Boss Exploration Result Implementation Plan

**Goal:** 보스 승리 시 일반 보상 대신 탐사 결과를 표시하고, 거점 도착 후 탐사 자산을 블루 더스티움으로 한 번만 정산한다.

## 데이터 흐름

1. 전투 핵심 피해/사망 처리 지점에서 `CharacterId` 기반 런 통계를 누적한다.
2. 런 시작 시 스킬 인벤토리 스냅샷을 저장해 새로 획득한 스킬을 구분한다.
3. 보스 승리 시 현재 레드 더스티움, 모든 유물, 새 스킬, 캐릭터 통계로 결과 스냅샷을 만든다.
4. Battle 씬 탐사 결과 패널의 거점 이동 버튼이 연구 대기 데이터를 Lobby 런타임에 저장하고 Lobby 상태로 전환한다.
5. Lobby 씬 연구 패널이 정산을 한 번 적용하고 확인 시 대기 데이터를 제거한다.

## 환산 정책

- 레드 더스티움: 50%, 소수점 버림
- 유물: Common 10, Uncommon 25, Rare 50, Unique 100
- 스킬: CoreCommon 10, CoreRare 25, CoreEpic 50
- 알 수 없는 등급은 0으로 계산하고 경고한다.

## 변경 단위

- `BattleRuntimeData`: 시작 스킬 ID와 캐릭터 통계 저장
- `LobbyRuntimeData`: 중복 지급 방지 상태가 포함된 대기 연구 결과 저장
- `BattleRunStatisticsService`: ID 기반 피해, 사망, 처치 누적
- `ExplorationResultBuilder`: UI와 분리된 결과 스냅샷 생성
- `ResearchConversionPolicy`: 등급별 블루 환산
- `BattleResultChecker`: 보스 승리 분기와 일반 보상 생략
- `ExplorationResultPanelUI`: Battle 씬 결과 표시 및 거점 전환
- `ResearchResultPanelUI`: Lobby 씬 도착 후 정산 표시 및 확인 처리

## 테스트

- `Assets/Tests/EditMode~/BossExplorationResultTests.cs`
- 통계 누적, 새 스킬 필터링, 등급별 환산, 중복 지급 방지를 검증한다.
- Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않고 MSBuild 컴파일과 Unity Test Runner용 테스트 생성을 수행한다.

## 멀티플레이 경계

- 통계와 결과는 Scene Object가 아니라 `CharacterId`, 자산 ID, 숫자 스냅샷으로 저장한다.
- 피해 결과 확정 후 통계를 기록하며 UI는 결과를 계산하거나 전투 상태를 변경하지 않는다.
- 정산은 명시적인 결과 스냅샷을 한 번 소비하는 방식으로 구현한다.
