# 결과 패널 스테이지 표시 및 계층 정리 구현 계획

1. 맵시트의 `MapData.Stage` 기준 스테이지 표시 매핑과 씬 그룹 존재를 검증하는 EditMode 테스트를 추가한다.
2. `ExplorationResultPanelUI`에 `Stage`, `DisplayName`, `PreviewSprite` 기반 스테이지 프레젠테이션 매핑과 해석 메서드를 추가한다.
3. 결과 패널이 열릴 때 현재 노드의 `MapId`로 `MapData`를 찾고, `MapData.Stage` 기준으로 이름과 프리뷰 이미지를 적용한다.
4. `Battle.unity`의 `ExplorationReportFrame` 아래에 그룹 RectTransform 3개를 추가한다.
5. 기존 자식들의 좌표를 유지한 채 부모만 역할별 그룹으로 변경한다.
6. MSBuild와 씬 YAML 중복 ID 검증을 실행한다.
