# 배틀 맵 호버 차단 해제 및 X 간격 축소 설계

## 조사 결과

- 노드의 `HoverHitArea`는 활성 상태이며 레이캐스트 대상이다.
- EventSystem과 InputSystem UI 모듈도 Battle 씬에 존재한다.
- 실제 원인은 `MapArea/ViewPort`의 마지막 자식 `Background`가 지도 전체를 덮고 `Raycast Target`이 켜져 있는 것이다. 이 그래픽이 노드보다 위에서 모든 포인터 레이캐스트를 가로챈다.
- 현재 열 간격은 `BattleMapLayoutUtility.LayerGap = 140`이다.

## 변경 설계

- `ViewPort/Background` 이미지의 `Raycast Target`만 끈다. 시각적 배치와 마스크는 유지한다.
- 노드 입력은 기존 `HoverHitArea`가 받으며, 정보 표시와 아이콘 확대 흐름은 변경하지 않는다.
- 열 간격은 140에서 100으로 줄인다.
- 콘텐츠 너비와 현재 노드 포커스는 생성 노드 좌표로 계산되므로 새로운 간격을 자동 반영한다.

## 검증

- 레이아웃 테스트의 열 간격 기대값을 100으로 변경한다.
- Battle 씬 테스트에서 ViewPort Background 이미지의 `m_RaycastTarget`이 0인지 확인한다.
- 런타임 어셈블리 컴파일과 씬 직렬화 블록을 확인한다.

## 멀티플레이 영향

UI 좌표와 포인터 입력 대상만 변경한다. 맵 그래프, 노드 ID와 진행 상태에는 영향이 없다.
