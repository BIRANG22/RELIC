# ExplorationReport 캐릭터 행 압축 및 정보 아이콘 배치 설계

## 의도

`ExplorationReportInformationIcon`을 캐릭터 초상 좌하단에 배치하고, 캐릭터 결과 행의 과도한 세로 여백을 줄여 현재 높이의 약 2/3로 압축한다.

## 현재 구조와 원인

- `ExplorationReportInformationIcon`은 `ExplorationReportRow_0~2`의 직접 자식이어서 Row의 `VerticalLayoutGroup` 정렬 대상이다.
- 아이콘이 40px 높이의 독립 행을 차지하므로 초상과 나머지 정보 사이에 불필요한 세로 공간이 생긴다.
- 각 Row의 현재 크기는 `310x460`이며, Row 프레임은 Row 전체에 Stretch되어 있다.
- 각 `ExplorationReportPortraitViewport`는 `310x90`이고 `RectMask2D`로 초상을 자른다.

## 선택한 설계

- 각 정보 아이콘을 대응하는 `ExplorationReportPortraitViewport`의 자식으로 이동한다.
- 아이콘은 Viewport 좌하단 기준 `40x40`, 안쪽 여백 6px로 배치한다.
- Viewport의 마스크 안에 아이콘을 두어 초상 영역 밖으로 돌출되지 않게 한다.
- 기존 `informationIcon`, `informationHoverTarget`, `informationOwner` 참조는 유지한다.
- 아이콘이 Row의 레이아웃 행에서 제거되므로 Portrait, 캐릭터 정보, 유물 슬롯, 기억 슬롯, 경험치 영역의 기존 크기는 유지한다.
- `ExplorationReportRow_0~2` 높이를 `460`에서 `310`으로 줄여 약 2/3 높이로 압축한다.
- Row 프레임은 기존 Stretch 설정을 유지하여 축소된 Row에 맞춰진다.

## 고려한 대안

- Row 아래에 그대로 두고 `LayoutElement.ignoreLayout`을 적용하는 방법은 Portrait 위치가 바뀔 때 아이콘이 함께 이동하지 않아 제외한다.
- Viewport와 별도의 오버레이 앵커를 Row에 추가하는 방법은 불필요한 오브젝트와 참조가 늘어나 제외한다.

## 범위

- `Battle.unity`의 세 캐릭터 결과 행 UI 계층과 RectTransform만 변경한다.
- 경험치 계산, 슬롯 데이터, 통계 툴팁 동작은 변경하지 않는다.
- Unity 에디터 직접 제어 및 BatchMode 검증은 수행하지 않는다.
