# 스테이지·맵 타입 BGM 상태 ID 설계

## 조사 결과

현재 맵 원본 `GameDataRuntime.csv`의 Map_01~30은 모두 `Stage1`이다. Common 8개, Elite 5개, Boss/Start/Shop/Rest 각 1개, Special 8개, Tutorial 5개이며 Tutorial은 전투형 3개와 이벤트형 2개로 나뉜다.

## 설계

의미 없는 `Battle1~3`, `Event1~2` 상태를 `Stage1Common`, `Stage1Elite`, `Stage1Boss`, `Stage1Start`, `Stage1Shop`, `Stage1Rest`, `Stage1Special`, `Stage1TutorialBattle`, `Stage1TutorialEvent`로 교체한다. 상황 상태도 `Stage1LowHealth`, `Stage1BossIntro`, `Stage1Defeat`로 명확히 한다.

`BattleSceneController`는 선택 노드의 MapData를 조회해 Stage, Type, 전투맵 보유 여부를 BGM 디렉터에 전달한다. `BattleBgmDirector`의 인스펙터 매핑은 Stage + Type + Any/Battle/Event 변형을 키로 사용한다. 이후 Stage2가 추가되면 enum 상태와 매핑만 추가할 수 있다.

오디오 메인 클립이 없는 상태는 계속 후보에서 제외하며, 유효 후보가 없으면 현재 BGM을 유지한다.

## 멀티플레이 경계

맵 데이터와 이미 결정된 전투 결과만 읽는 로컬 프레젠테이션 변경이며 전투 상태를 변경하지 않는다.
