# 배틀 씬 진입 기본 BGM 시점 수정 설계

## 조사 결과

- `SoundDatabase`에는 `Stage1Common` 클립이 연결되어 있고 Battle 씬의 `BattleBgmDirector` 방별 매핑도 존재한다.
- `BattleState.Enter`는 씬 로드가 끝난 뒤 `HasUnclearedCurrentNode`를 계산한다.
- 신규 탐사도 Battle 씬 `Start`에서 첫 노드를 설정하므로, 로드 완료 시점에는 미완료 현재 노드가 생겨 이어하기로 오인된다.
- 이 때문에 `Stage1Common` 요청이 생략되고 실제 방 선택 이벤트가 발생할 때까지 Title BGM이 유지된다.

## 권장 설계

- 씬 로드 전에 미완료 현재 노드 여부를 캡처한다.
- 신규 진입이면 씬 로드 직후 `Stage1Common`을 재생한다.
- 이어하기이면 기본 BGM 요청을 생략하고 `BattleRoomSelected -> BattleBgmDirector` 흐름에 맡긴다.
- 씬 로드 후 변경된 노드 상태는 신규/이어하기 판정에 사용하지 않는다.

## 검증

- 로드 전 미완료 노드가 없고 로드 후 생긴 경우 기본 BGM을 재생한다.
- 로드 전부터 미완료 노드가 있던 경우 기본 BGM을 재생하지 않는다.
- 런타임 및 Editor 어셈블리를 컴파일한다.

## 멀티플레이 경계

BGM 요청 시점만 변경하는 로컬 presentation 수정이다. 전투 상태, 랜덤, Command 및 네트워크 snapshot에는 영향이 없다.
