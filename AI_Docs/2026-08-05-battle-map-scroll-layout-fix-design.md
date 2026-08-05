# 배틀 지도 스크롤·레이아웃 수정 설계

## 목표

- 지도 선택 중 `MapPanel`의 수동 Y 배치를 유지한다.
- 각 레이어의 노드를 최대 3개로 제한한다.
- 레이어 간격과 같은 레이어 내 노드 간격을 기존의 절반으로 줄인다.
- 노드 프리팹 표시 크기를 절반으로 줄인다.
- 현재 노드를 Viewport 왼쪽에 맞추고 마우스 드래그로 지도를 가로 스크롤한다.

## 조사 결과

- `MapPanel` 루트의 `BattleCharacterPanelUI`가 전투 단계 이벤트에 따라 루트 RectTransform Y를 150과 540 사이에서 변경한다.
- ScrollRect Content는 폭 420이며 실제 `NodeRoot`와 `LineRoot`는 Content의 형제라서 Content 이동이 지도에 적용되지 않는다.
- 포커스 로직은 현재 노드가 아니라 다음 선택 가능 노드들의 평균 X를 사용한다.
- 생성기의 최대 레이어 노드 수는 5이고 고정 레이어 및 fallback에도 4개 구성이 남아 있다.

## 권장 구조

- `MapPanel`에서 잘못 복사된 `BattleCharacterPanelUI`만 제거하고 기존 RectTransform 값은 유지한다.
- `Map`을 ScrollRect Content로 유지하며 `LineRoot`와 `NodeRoot`를 그 자식으로 이동한다. 선은 노드보다 먼저 렌더링한다.
- 지도 생성 후 노드 X 범위와 Viewport 폭으로 Content 폭을 계산한다. Content 높이와 Y 위치는 변경하지 않는다.
- 포커스는 `CurrentNodeIndex`의 X를 사용하고 Viewport 왼쪽에 노드 반지름과 여백을 확보한다.
- `MaxColumnCount`와 모든 고정 개수를 3 이하로 제한하고 총 노드 수 범위를 새 최대치에 맞춘다.
- `LayerGap`은 280에서 140, `RowGap`은 150에서 75, `NodePrefab`은 80×80에서 40×40으로 변경한다.

## 검증

- EditMode 테스트로 3개 제한, 절반 간격, Content 폭 계산, 현재 노드 왼쪽 포커스를 검증한다.
- 씬 구조 검사로 잘못된 컴포넌트 제거와 Content 아래 LineRoot/NodeRoot 배치를 확인한다.
- C# 프로젝트와 테스트 프로젝트를 컴파일한다.

## 멀티플레이 경계

맵 생성 개수는 seed 기반 `BattleRandom` 흐름을 유지한다. UI 포커스와 ScrollRect는 표시 전용이며 전투 결과 상태를 변경하지 않는다.
