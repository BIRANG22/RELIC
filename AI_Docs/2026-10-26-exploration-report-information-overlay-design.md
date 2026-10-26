# ExplorationReport 정보 아이콘 마스크 분리 및 정보창 배치 설계

## 의도

`ExplorationReportInformationIcon`이 초상 `RectMask2D`의 영향을 받지 않게 하고, 호버·클릭 정보창을 아이콘 기준 중앙 아래가 아니라 오른쪽 아래에 표시한다.

## 현재 구조와 원인

- 세 정보 아이콘은 각각 `ExplorationReportPortraitViewport`의 자식이며 Viewport에는 `RectMask2D`가 있다.
- 따라서 아이콘과 자식 글리프가 초상 마스크의 클리핑 대상이 된다.
- 정보창 배치는 아이콘 하단 중앙점을 기준으로 계산하고 정보창 피벗을 그대로 적용하므로 중앙 아래에 표시된다.

## 선택한 설계

- 각 Row의 기존 Viewport 자리에 `ExplorationReportPortraitContainer`를 두고, 마스크 Viewport와 정보 아이콘을 Container의 형제로 둔다.
- Container만 Row의 `VerticalLayoutGroup` 정렬 대상이 되므로, 레이아웃 재계산 때 Viewport와 아이콘이 항상 함께 이동한다.
- 아이콘의 화면상 위치와 크기(초상 좌하단, `40x40`)는 유지한다.
- `StatisticsOverlayAnchor`는 아이콘 자식으로 유지해 호버·클릭 기준 참조가 끊기지 않게 한다.
- 정보창은 아이콘 우하단을 기준으로 수평·수직 12px 간격을 두고, 정보창 좌상단이 오른쪽 아래에 오도록 배치한다.
- 정보창이 패널 경계를 벗어날 때는 기존처럼 패널 내부로 보정한다.

## 고려한 대안

- `Image.maskable`만 끄는 방법은 아이콘 자식 글리프까지 별도로 관리해야 하고 마스크 계층 의존성이 남아 제외한다.
- 아이콘을 Row 직계 자식으로 두고 `LayoutElement.ignoreLayout`을 적용하는 방법은 런타임 레이아웃 재계산 때 Viewport와 상대 위치가 어긋날 수 있어 제외한다.

## 범위와 제약

- `Battle.unity`의 세 초상 Container, 정보 아이콘 및 Viewport 계층·RectTransform을 변경한다.
- `ExplorationResultPanelUI`의 정보창 위치 계산을 우하단 기준으로 변경한다.
- 관련 EditMode 테스트를 갱신한다.
- 복구용 `Battle_CurrentRecovery_2026-10-26.unity`는 수정하지 않는다.
- Unity 에디터 직접 제어 및 BatchMode 테스트는 수행하지 않는다.
- 전투 데이터와 결과 계산은 변경하지 않는다.
