# Debug Battle Character Panel Implementation Plan

## Tasks

1. `BattleRoomLoader`에 통합 캐릭터 패널 우선 옵션 추가
   - `disableStandaloneSkillListPanel` 직렬화 필드를 추가한다.
   - 옵션이 켜지면 `Tab`, 자동 스킬리스트 오픈, 스킬리스트 Refresh/Close 경로를 무시한다.

2. DebugBattle 전용 패널 생성기 추가
   - `DebugBattleCharacterPanelBuilder`를 추가한다.
   - `BattleHUDCanvas` 아래에 `BattleCharacterPanel` 오브젝트를 생성한다.
   - `BattleCharacterPanelUI`가 최소한 스킬 버튼, 이동 버튼, 아이템 버튼, 캐릭터/몬스터 루트, 텍스트와 아이콘을 표시할 수 있는 계층을 만든다.

3. `BattleCharacterPanelUI` 자동 바인딩 보강
   - 디버그 생성 패널의 자식 이름을 기준으로 누락된 참조를 자동 연결한다.
   - 기존 Battle 씬의 수동 연결은 유지한다.

4. `DebugBattle.unity` 연결
   - 기존 `SkillListPanel` 오브젝트는 비활성화한다.
   - `BattleRoomLoader.skillListPanel` 참조를 비우고 `disableStandaloneSkillListPanel`을 켠다.
   - 디버그 씬 부트스트랩이 패널 생성기를 보장한다.

5. 검증
   - MSBuild 런타임/에디터 어셈블리 빌드.
   - `Lobby.unity`, `Battle.unity` 무변경 확인.
   - Unity batchmode 테스트는 실행하지 않는다.

