# DebugBattle 스폰 및 스킬 VFX 테스트 설계

## 배경

Battle 씬 전환 흐름이 변경되면서 DebugBattle 씬을 단독 실행했을 때 캐릭터와 허수아비가 스폰되지 않는 문제가 발생했다. DebugBattle은 원래 이펙트 확인용 씬이지만, 이제 스킬 ID에 연결된 애니메이션과 VFX도 실제 전투 사용 경로로 확인할 수 있어야 한다.

## 조사 결과

- `DataManager` 오브젝트는 `Bootstrap.unity`에만 있고 `DebugBattle.unity`에는 없었다.
- `BattleRoomLoader`는 `DataManager.Instance`가 준비될 때까지 대기하므로, `DebugBattle` 씬 단독 실행에서는 전투 로드가 진행되지 않을 수 있다.
- `DebugBattle.unity`의 `BattleDebugDataProvider`는 연결되어 있지만 `debugCharacterIds`가 비어 있어, 파티가 비어 있을 때 호출되어도 캐릭터 런타임을 만들지 못한다.
- `DebugBattlePartySetup`은 기본 제공 캐릭터 3명을 자동 편성하는 유틸로 이미 존재한다.
- `S_Ability_11`은 데이터와 `SkillVfxDatabase` 매핑이 존재하지만, 기본 런타임 장착 스킬에는 들어가지 않아 스킬 목록에서 바로 테스트하기 어렵다.
- `BattleRoomLoader`에는 선택 캐릭터 스킬 목록 자동 오픈 메서드가 있지만, 입력 준비 후 호출 지점이 빠져 있었다.

## 권장 설계

- `DebugBattle` 씬에 `DataManager`를 추가해 씬 단독 실행에서도 데이터베이스와 프리팹 DB를 사용할 수 있게 한다.
- `BattleDebugDataProvider`는 명시 캐릭터 ID가 없으면 `DebugBattlePartySetup`으로 기본 파티를 생성한다.
- `DebugBattlePartySetup`은 `Char_03` 런타임이 있으면 테스트 스킬 `S_Ability_11`을 빈 장착 슬롯에 보장한다.
- `TitleDebugBattleLauncher`와 `DebugBattleSceneRunner` 모두 디버그 파티와 테스트 스킬을 보장해, 타이틀 진입과 씬 직접 실행의 결과를 맞춘다.
- 허수아비는 기존 `BattleRoomLoader.ConfigureDebugTargetMonster("Mon_02", 23)` 경로를 유지한다.
- 입력 준비 후 기존 스킬 리스트 UI를 열어, 스킬 선택, 그리드 선택, 타임라인 예약, 실행, 애니메이션/VFX 재생까지 실제 전투 흐름으로 테스트한다.

## 멀티플레이 경계

이번 변경은 DebugBattle 전용 데이터 준비와 UI 편의 동작이다. 전투 결과 계산, 네트워크 동기화 모델, 스킬 판정 로직은 변경하지 않는다.
