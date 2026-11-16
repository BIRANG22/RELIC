# 이어하기 현재 방 BGM 즉시 재생 설계

## 조사 결과

`BattleSceneController`는 미완료 노드를 복원하면서 `BattleRoomSelected` 이벤트를 발생시키고, `BattleBgmDirector`는 이 이벤트로 현재 방 BGM을 선택한다. 그러나 씬 로드가 끝난 뒤 `BattleState.Enter()`가 `Stage1Common`을 무조건 요청해 이미 선택된 보스방 BGM을 기본 전투 BGM으로 덮어쓴다. 이후 방 상태가 다시 평가될 때 보스방 BGM으로 복귀하므로 기본 BGM이 잠깐 들린다.

## 권장 설계

- 신규 탐사처럼 미완료 현재 노드가 없는 진입에서는 기존 `Stage1Common`을 재생한다.
- 미완료 현재 노드를 이어하는 진입에서는 `BattleState`의 기본 BGM 요청을 생략한다.
- 이어하기 방의 실제 BGM 선택은 기존 `BattleRoomSelected -> BattleBgmDirector -> AudioManager` 흐름을 유지한다.
- 방별 매핑, 페이드, 보스 인트로 및 저체력 우선순위는 변경하지 않는다.

## 검증

- `ShouldPlayDefaultBattleBgm(false)`는 `true`여야 한다.
- `ShouldPlayDefaultBattleBgm(true)`는 `false`여야 한다.
- 런타임 및 에디터 C# 프로젝트가 컴파일되어야 한다.

## 멀티플레이 경계

BGM 선택은 동기화된 맵 런타임을 읽는 로컬 프레젠테이션이다. 전투 상태 변경, 랜덤, 네트워크 메시지는 추가하지 않는다.
