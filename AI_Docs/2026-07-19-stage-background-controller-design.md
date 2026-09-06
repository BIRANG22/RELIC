# Stage Background Controller Design

## 목표

배틀 배경 선택 책임을 `BattleSceneController`에서 분리하고, 현재 맵의 1-based 행 범위에 따라 `Stage_01` 아래에 배경 프리팹을 하나만 표시한다.

## 배경 규칙

- 1~3행: `St1_00`
- 4~7행: `St1_01`
- 8~10행: `St1_02`
- `Boss` 배경은 이번 변경 범위에서 제외한다.
- 맵 데이터의 `GeneratedMapNodeData.LayerIndex`는 0-based이므로 컨트롤러에 전달할 때 1을 더한다.

## 구조

`StageBackgroundController`는 직렬화된 범위 목록을 가진다. 각 항목은 시작 행, 종료 행, 배경 프리팹을 보유한다. `ShowForLayer(int layerIndex)`는 행을 계산하고 일치하는 프리팹을 선택해 스폰 루트 아래에 생성한다. 이미 같은 프리팹이 표시 중이면 재생성하지 않으며, 다른 배경으로 바뀔 때만 기존 인스턴스를 제거한다.

`BattleSceneController`는 방별 배경 오브젝트를 직접 찾거나 활성화하지 않는다. Start, Battle, Rest 방을 열기 직전에 대상 방 아래의 `StageBackgroundController`를 찾아 `ShowForLayer(nodeData.LayerIndex)`를 호출하는 연결 책임만 가진다. 세 방은 같은 배경 프리팹과 행 범위를 사용하며, 각 `Stage_01` 오브젝트에는 설정만 저장한다. 향후 10행 전용 배경은 범위 설정 변경만으로 추가할 수 있다.

`StageBackgroundController` 자체에는 `Stage_01`이나 특정 방 이름을 넣지 않는다. 따라서 추후 `Stage_02`, `Stage_03`에도 같은 컴포넌트를 붙이고 각 스테이지용 범위와 프리팹만 연결하여 재사용한다.

## 오류 처리

- 일치하는 범위가 없으면 기존 인스턴스를 제거하고 경고한다.
- 프리팹이 비어 있는 범위는 선택하지 않고 경고한다.
- 범위의 시작 행이 종료 행보다 크면 해당 항목을 무시한다.
- 여러 범위가 겹치면 목록에서 먼저 일치한 항목을 사용한다.

## 검증

EditMode 테스트로 행 범위 선택, 동일 프리팹 재사용, 범위 변경 시 교체, 미일치 시 제거를 확인한다. Unity 에디터가 열려 있으므로 batchmode 테스트는 실행하지 않고 프로젝트 컴파일과 정적 검사를 수행한다.

## 멀티플레이 경계

배경은 `LayerIndex`라는 안정적인 스냅샷 값만 소비하고 전투 상태를 변경하지 않는다. 전투 결과 계산, UI, VFX와 독립된 프레젠테이션 컴포넌트이므로 동기화 로직을 추가하지 않는다.
