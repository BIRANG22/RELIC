# DebugBattle 최신 Battle 씬 동기화 설계

## 목적

`DebugBattle.unity`의 공용 전투 기능, UI, 카메라, 연출, 사운드 구성을 현재 `Battle.unity`와 동일하게 최신화한다. DebugBattle 전용 데이터 준비와 테스트 도구는 유지하여 실제 전투 경로에서 스킬, 효과, VFX를 검증할 수 있게 한다.

## 조사 결과

- `Battle.unity`는 약 4.01MB, `DebugBattle.unity`는 약 2.93MB이며 프리팹 오버라이드와 참조 수가 크게 달라졌다.
- 두 씬은 같은 핵심 루트와 `BattleSceneController`, `BattleRoomLoader`, `BattleDebugDataProvider` 구조를 공유하지만 DebugBattle에는 최신 Battle UI/연출 변경이 누락되어 있다.
- DebugBattle 전용 실행은 `DebugBattleSceneBootstrap`과 `DebugBattleSceneRunner`가 담당한다. 디버그 창, 허수아비 몬스터, 기본 파티 구성은 씬 공용 UI와 분리되어 있다.
- DebugBattle 단독 실행에는 씬에 배치된 `DataManager`와 데이터베이스 참조가 필요하다.

## 접근 방식

### 선택: Battle 씬을 기준으로 전면 동기화하고 DebugBattle 전용 DataManager만 추가

`Battle.unity` 전체를 DebugBattle 공용 구성의 기준으로 사용한다. 그 뒤 기존 DebugBattle의 `DataManager` 루트와 직렬화된 데이터베이스 참조만 보존한다. 디버그 창과 러너는 씬 이름을 기준으로 런타임에 보장되므로 별도의 오래된 UI 복사본을 유지하지 않는다.

이 방식은 현재 Battle의 모든 공용 기능과 UI를 빠짐없이 반영하며, 두 씬의 차이를 명시적인 DebugBattle 전용 요소로 제한한다.

### 제외: 변경된 UI만 선별 이식

씬 오버라이드가 대규모로 벌어진 상태라 누락 가능성이 높고 다음 Battle 변경 때 다시 불일치가 생기므로 사용하지 않는다.

### 제외: DebugBattle을 Battle 씬 로드 후 Additive 오버레이로 변경

씬 진입 구조와 Build Settings, 기존 디버그 런처 계약까지 변경해야 하므로 이번 최신화 범위를 벗어난다.

## 구현 범위

- `DebugBattle.unity`의 공용 내용은 현재 `Battle.unity`와 동일하게 맞춘다.
- 기존 DebugBattle의 `DataManager` 루트와 데이터베이스 참조를 보존한다.
- `DebugBattleSceneBootstrap`, `DebugBattleSceneRunner`, 디버그 효과 창, 허수아비 몬스터 동작은 기존 코드를 재사용한다.
- 씬 동기화 회귀 테스트는 Battle 원문에 DebugBattle 전용 DataManager 문서와 SceneRoots 항목만 허용한다.
- Battle 씬 자체와 전투 핵심 계산 로직은 변경하지 않는다.

## 검증

- 씬 YAML에서 DebugBattle이 Battle의 공용 내용을 그대로 포함하는지 검사한다.
- DebugBattle에 DataManager가 정확히 한 개 있고 필수 데이터베이스 참조가 유지되는지 검사한다.
- DebugBattle 런타임 부트스트랩과 디버그 대상 코드 참조가 유지되는지 검사한다.
- EditMode 테스트 소스와 전체 C# 프로젝트를 컴파일한다.
- Unity 에디터 화면 및 실제 플레이 동작은 프로젝트 규칙에 따라 직접 제어하지 않고 미검증 항목으로 보고한다.

## 멀티플레이 경계

씬의 공용 UI·연출·참조를 최신 Battle 구성으로 맞추는 작업이며 Command, 전투 상태 변경, 결과 이벤트, 랜덤, 네트워크 스냅샷 구조는 변경하지 않는다.
