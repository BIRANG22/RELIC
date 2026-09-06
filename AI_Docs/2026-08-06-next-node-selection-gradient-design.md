# 다음 노드 선택 배경 그라데이션 설계

## 목표

`NextNodeSelectionRoot`의 왼쪽은 전투 배경이 자연스럽게 보이고 오른쪽으로 갈수록 검게 어두워지는 배경을 추가한다.

## 설계

- 기존 `gradaion2.png`의 위쪽 투명→아래쪽 검정 알파 그라데이션을 재사용한다.
- `GradientBackground`를 Root의 첫 번째 자식으로 추가하고 Z축으로 +90도 회전해 왼쪽 투명→오른쪽 검정으로 변환한다.
- Root의 `VerticalLayoutGroup`에 영향을 받지 않도록 `LayoutElement.ignoreLayout`을 켠다.
- Image의 Raycast Target을 꺼 다음 노드 버튼 입력을 방해하지 않는다.
- 런타임 생성 버튼보다 앞서 그려지도록 배경을 첫 번째 자식으로 둔다.

## 멀티플레이 경계

시각적 UI 배경만 추가하며 맵 상태나 노드 선택 결과에는 영향을 주지 않는다.
