# Debug Battle Sandbox Design

## Goal

`DebugBattle` 씬을 배틀씬 복제본이 아니라 전투 효과 검증용 샌드박스로 정리한다.

## Scope

- `DebugBattle.unity`를 직접 수정한다.
- `Battle.unity`는 참조만 하고 수정하지 않는다.
- 기본 런타임은 아군 캐릭터 1명과 허수아비 몬스터 1명을 기준으로 한다.
- 스킬, 룬, 유물, 컴파운드 효과 검증을 위한 런타임 조작 UI를 제공한다.
- 기존 전투 결과 계산 로직은 수정하지 않는다.

## Recommended Structure

- `DebugBattlePartySetup`
  - 기본 디버그 파티 크기를 1명으로 줄인다.
  - 선택 캐릭터 ID를 받아 런타임 파티를 다시 만드는 API를 제공한다.
  - 기존 캐릭터 마스터/런타임/파티 스토어를 사용한다.

- `BattleDebugDataProvider`
  - 씬 시작 시 기본 아군 1명을 구성한다.
  - 디버그 창에서 캐릭터 교체 요청이 들어오면 같은 구성 API를 재사용한다.

- `BattleEffectDebugTool`
  - 캐릭터 교체, 룬/유물/컴파운드 장착, 리프레시/리로드를 명령형 유틸로 제공한다.
  - UI는 전투 상태를 직접 계산하지 않고 런타임 데이터와 기존 전투 서비스를 갱신한다.

- `BattleEffectDebugWindow`
  - 기존 IMGUI 창을 유지하되 크기 조절 가능하게 만든다.
  - 캐릭터, 스킬, 장비, 상태, 그리드/컴파운드 섹션을 분리한다.
  - 창의 최소 크기만 제한하고 런타임에서 자유롭게 확대/축소할 수 있게 한다.

## Data Flow

1. `DebugBattle` 시작 시 `BattleDebugDataProvider.CreateDebugData()`가 디버그 런타임을 구성한다.
2. `DebugBattlePartySetup`이 `CharacterRuntimeStore`와 `PartyRuntimeStore`에 1인 파티를 저장한다.
3. `BattleRoomLoader`가 기존 전투 로딩 경로로 아군과 몬스터를 배치한다.
4. 디버그 창에서 스킬/룬/유물/컴파운드 값을 바꾸면 런타임 데이터가 갱신되고 HUD와 전투 룸이 새로고침된다.

## Verification

- EditMode 테스트로 기본 파티 크기, 지정 캐릭터 교체, 장비 ID 분류, 창 크기 제한 로직을 검증한다.
- MSBuild로 `Assembly-CSharp`와 `Assembly-CSharp-Editor` 컴파일을 확인한다.
- Unity batchmode 테스트는 프로젝트 규칙상 실행하지 않는다.

