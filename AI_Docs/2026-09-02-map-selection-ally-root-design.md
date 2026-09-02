# 맵 선택 단계 Ally Root 표시 설계

## 문제

`Map_26`은 휴식방에서 Ally Root를 숨겨야 하지만, `MapRoomController.RefreshNow()`가 맵 선택 패널이 열린 경우에도 동일한 Map ID 예외를 적용해 캐릭터를 숨긴다.

## 설계

- `MapRoomController.RefreshForMapSelection()`을 추가한다.
- 맵 선택 경로에서는 Map ID 예외를 무시하고 Ally Root를 활성화하며 캐릭터 프리팹을 배치한다.
- `RefreshNow()`는 맵 패널 활성 여부를 기준으로 일반 새로고침과 맵 선택 새로고침을 선택한다.
- `BattleSceneController`가 맵 선택으로 전환하기 직전에도 명시적으로 맵 선택 새로고침을 호출한다.
- 일반 `RefreshForMap(Map_26)`는 유지하여 휴식방 내에서는 Ally Root를 계속 숨긴다.

## 영향

- 맵 선택 연출과 휴식방 표시만 변경한다.
- 전투 상태, 맵 생성 및 멀티플레이 동기화에는 영향이 없다.
