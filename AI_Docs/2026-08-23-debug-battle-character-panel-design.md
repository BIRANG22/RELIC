# Debug Battle Character Panel Design

## Goal

`DebugBattle` 씬에서도 `Battle` 씬처럼 `BattleCharacterPanelUI` 중심으로 캐릭터, 스킬, 룬, 유물, 몬스터 정보를 확인하고 조작할 수 있게 한다.

## Problem

- `Battle` 씬에는 `BattleCharacterPanel`이 존재하고 `BattleRoomLoader`가 이 패널을 바인딩한다.
- `DebugBattle` 씬에는 `SkillListPanel` 중심의 구식 테스트 UI가 남아 있어 통합 정보 확인이 어렵다.
- `BattleCharacterPanel`은 현재 프리팹이 아니라 `Battle.unity` 내부 오브젝트다.

## Approach

- `Battle.unity`는 수정하지 않는다.
- `DebugBattle`에는 런타임에서 통합 패널을 생성하는 디버그 전용 부트스트랩을 둔다.
- 생성된 패널에는 실제 전투에서 쓰는 `BattleCharacterPanelUI`를 붙인다.
- `BattleRoomLoader`에는 별도 `SkillListPanel` 자동 사용을 끄는 옵션을 추가해 DebugBattle에서 구식 스킬리스트가 다시 열리지 않게 한다.
- 패널 생성은 디버그 씬 전용이며 전투 결과 계산 로직은 변경하지 않는다.

