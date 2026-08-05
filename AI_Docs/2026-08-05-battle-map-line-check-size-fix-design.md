# 배틀 지도 라인·체크 이미지·씬 직렬화 수정 설계

## 목표

- 완료 노드의 `CheckAnimationImage`를 기존 96×96의 0.4배인 38.4×38.4로 표시한다.
- 가로형 라인 이미지를 연결 방향으로 자연스럽게 늘린다.
- 같은 레이어의 노드 Y 간격을 40으로 줄인다.
- Battle 씬을 열 때 `MapPanel`의 누락 컴포넌트 자동 Fix 오류가 발생하지 않게 한다.

## 설계

- `NodePrefab`의 `checkImageSize`를 38.4×38.4로 변경한다.
- `MapLineView.Setup`은 Rect 크기를 `(연결 거리, 두께)`로 설정하고 회전은 연결 벡터의 각도를 그대로 사용한다.
- `BattleMapLayoutUtility.RowGap`을 75에서 40으로 변경한다.
- 현재 고아 상태인 `BattleCharacterPanelUI` 직렬화 블록을 `MapPanel` 컴포넌트 목록에 다시 연결하되 비활성 상태를 유지한다. 이 컴포넌트는 실행되지 않아 MapPanel Y를 변경하지 않으며, Unity 직렬화 참조 무결성도 회복한다.

## 검증

- 실제 `MapLineView`에 수평·대각 연결을 적용해 크기와 회전을 검증한다.
- NodePrefab 인스턴스의 자동 생성 CheckAnimationImage 크기를 확인한다.
- 씬에서 MapPanel 컴포넌트 참조와 비활성 직렬화 블록이 일치하는지 검사한다.
- 프로덕션과 EditMode 테스트 코드를 컴파일한다.

## 멀티플레이 영향

모든 변경은 지도 표시용 UI이며 맵 진행 상태와 전투 결과를 변경하지 않는다.
