# Event Room Excel Integration Design

## 배경
- Special 룸 이벤트를 맵 시트의 `EventId`와 매칭해 엑셀 데이터로 구동한다.
- 새 시트를 만들지 않고 기존 `EventMap` 데이터를 `Event` 시트로 승격한다.
- 기존 `EventMaster`, `EventChoice` 시트와 `EVENT_001` 계열 샘플 데이터는 사용하지 않는다.

## 권장 구조
- `Map.EventId`는 `Event_01` ~ `Event_09` 형식을 사용한다.
- 기존 `EVT001`, `EVT002_A`, `EVENT_001` 같은 값은 로딩 단계에서 `Event_01`, `Event_02_A` 형식으로 정규화한다.
- 맵 생성 결과 `GeneratedMapNodeData`에 `EventId`를 포함해 `BattleSceneController`가 `EventRoomController`로 전달한다.
- `EventRoomController`는 `EventId`가 있을 때만 데이터 이벤트 모드로 동작하고, 데이터가 없으면 기존 상자 이벤트 흐름을 유지한다.

## 이벤트 효과 범위
- 선택지 표시, 주사위 3개 판정, 확률 판정, 다음 이벤트 이동은 데이터 기반으로 처리한다.
- 유물 무작위 획득은 기존 `ChestRelicRewardService`를 재사용한다.
- 수치가 `TBD`이거나 별도 UI가 필요한 이벤트는 결과 문구를 표시하고, 실제 수치 변경은 데이터가 확정된 뒤 확장한다.

## 멀티플레이 경계
- 맵 생성과 이벤트 판정 랜덤은 `BattleRandom`을 사용한다.
- 이벤트 UI는 선택 결과를 표시하고, 런타임 상태 변경은 데이터 스토어에만 반영한다.
- 네트워크 프레임워크나 Scene Object 의존 전투 판정은 추가하지 않는다.
