# Battle MapPanel2 승격 설계

## 목표

`Battle.unity`의 사용자 제작 `MapPanel2`를 최종 `MapPanel` 루트로 승격한다. 기존 `MapPanel`의 지도 자식과 `BattleMapPanel` 컴포넌트를 새 루트로 옮기고 기존 루트를 제거한다. `DebugBattle.unity`는 변경하지 않는다.

## 이전 규칙

- `MapPanel2`의 기존 자식과 레이아웃은 보존한다.
- 기존 `MapPanel`의 여섯 자식을 `MapPanel2`의 기존 네 자식 뒤에 추가한다.
- 기존 `BattleMapPanel` 컴포넌트는 동일 fileID를 유지한 채 새 루트 소유로 변경한다.
-旧 루트의 비활성 Image와 CanvasRenderer는 제거한다.
-旧 `MapPanel` GameObject와 RectTransform fileID 참조를 새 루트 fileID로 모두 교체한다.
- Canvas 직계 자식 목록에서는旧 RectTransform을 제거한다.
- 새 루트 이름은 `MapPanel`, 레이어는 UI 레이어 5로 통일한다.

## 다음 노드 선택 루트

- 새 `MapPanel` 내부에 `NextNodeSelectionRoot`를 명시적으로 둔다.
- 이 루트가 `BattleNextNodeSelectionPanel`을 소유한다.
- `BattleMapPanel.nextNodeSelectionPanel`은 이 컴포넌트를 직렬화 참조한다.
- 선택 버튼 세 개는 런타임에 반드시 이 루트의 자식으로만 생성한다.
- 루트가 있지만 버튼이 없는 경우 버튼을 채우고, 루트가 누락된 경우에만 새 루트를 fallback 생성한다.

## 검증

- Battle 씬에 `MapPanel`이 정확히 하나 존재한다.
- `MapPanel2`와旧 루트 fileID가 남지 않는다.
-旧 지도 자식과 사용자 제작 자식이 모두 새 루트 아래에 존재한다.
- `NextNodeSelectionRoot`가 새 패널의 자식이고 `BattleMapPanel`에 연결된다.
- DebugBattle 씬은 변경하지 않는다.
