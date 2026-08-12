# MapId 기반 룸 비주얼 DB 설계

## 목표

맵 노드가 선택되어 룸이 열릴 때 `MapId`를 기준으로 해당 방에 표시할 NPC, 오브젝트, 장식 프리팹을 인스펙터에서 연결할 수 있게 한다.

## 배경

- `EventId`는 이벤트 선택지와 후속 이벤트 흐름 때문에 같은 공간에서도 여러 ID로 나뉠 수 있다.
- `BattleMapId`는 전투 배치, 몬스터, 전투 내부 데이터에 가깝다.
- `MapId`는 맵 시트의 개별 룸 항목이며, 특정 방의 공간/비주얼 표현을 연결하는 키로 가장 자연스럽다.

## 구조

- `MapVisualDatabase`
  - ScriptableObject DB.
  - `MapId`별로 하나 이상의 비주얼 프리팹 배치 정보를 가진다.
  - 동일 `MapId` 중복은 먼저 등록된 항목을 사용하고 경고만 남긴다.

- `MapVisualEntry`
  - `MapId`와 `MapVisualSpawnEntry` 목록을 가진다.

- `MapVisualSpawnEntry`
  - 생성할 프리팹, 앵커 이름, 로컬 위치/회전/스케일, 활성 여부를 가진다.
  - 앵커 이름이 비어 있거나 찾지 못하면 기본 `visualRoot` 아래에 생성한다.

- `MapVisualController`
  - 룸 오브젝트 하위에 붙는 표시 전용 컨트롤러.
  - `ApplyMapVisual(mapId)` 호출 시 기존 생성물을 정리하고 DB에서 `MapId` 항목을 찾아 생성한다.
  - `ClearVisuals()`로 생성물을 정리한다.
  - DB는 인스펙터 override를 우선 사용하고, 없으면 `DataManager.Instance.MapVisualDatabase`를 사용한다.

- `BattleSceneController`
  - Start, Battle, Boss, Rest, Special 룸을 열 때 기존 배경 적용 후 현재 노드의 `MapId`를 해당 룸의 `MapVisualController`에 전달한다.

## 범위

- 이번 작업은 표시용 비주얼만 다룬다.
- 전투 결과에 영향을 주는 장애물, 함정, 파괴 가능 오브젝트는 추후 `BattleMapId` 기반 전투 데이터로 별도 처리한다.
- `DebugBattle`, `Battletest` 씬은 수정하지 않는다.

## 멀티플레이 경계

`MapId`는 이미 런타임 맵 상태에 포함된 안정적인 ID다. 이번 작업은 그 ID를 읽어 클라이언트 표시물을 생성하는 UI/연출 계층 변경이며, 전투 판정이나 랜덤 결과에는 관여하지 않는다.
