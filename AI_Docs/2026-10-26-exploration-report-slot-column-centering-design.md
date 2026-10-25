# ExplorationReport 슬롯 열 중앙정렬 설계

## 의도

`ExplorationReportRelicSlots`와 `ExplorationReportMemorySlots`에서 생성되는 슬롯 열을 부모 RectTransform의 가로 중앙에 정렬한다.

## 원인

- 슬롯 루트는 부모 Row 중앙에 배치되어 있다.
- 런타임 슬롯은 루트 왼쪽 기준 `x=0`부터 배치된다.
- 따라서 슬롯 전체 폭이 루트보다 작으면 열이 왼쪽으로 치우친다.

## 선택한 설계

- 슬롯 한 개의 실제 표시 폭은 `RectTransform.rect.width * localScale.x`로 계산한다.
- 한 행에 표시되는 열 수는 `min(maxCount, columns)`로 계산한다.
- 전체 폭은 `(열 수 - 1) * horizontalSpacing + 실제 슬롯 폭`이다.
- 시작 x는 `max(0, (root.rect.width - 전체 폭) * 0.5)`이다.
- 각 슬롯 x는 `시작 x + 열 인덱스 * horizontalSpacing`으로 배치한다.
- 유물과 기억 슬롯에 같은 계산을 사용한다.

## 범위

- 슬롯 가로 시작 위치만 변경한다.
- 슬롯 개수, 스케일, 간격, 세로 배치, 아이콘 데이터는 변경하지 않는다.
- 전투 결과 및 멀티플레이 상태에는 영향이 없다.
