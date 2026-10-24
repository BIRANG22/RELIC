# 탐사 보고 패널 Scene View 피킹 크래시 조사 결과

## 조사 결과

- Unity 크래시 로그는 `ExtendVisibleAsChild -> PickObjects -> GetClosestPickingID`에서 종료된다.
- 따라서 런타임 탐사 결과 계산이나 포인터 콜백이 아니라 Scene View가 UI 계층의 표시·피킹 대상을 순회하는 과정의 네이티브 크래시다.
- 패널 자식을 하나씩 활성화하고 클릭하면 재현되지 않는다.
- Hierarchy에서 `ExplorationResultPanel`을 검색한 상태로 부모를 한 번에 활성화하고 Scene View의 같은 영역을 반복 클릭하면 재현된다.
- 반복 클릭은 겹친 오브젝트를 순환 선택하는 `GetAllOverlapping/GetClosestPickingID` 경로를 사용하며 크래시 스택과 일치한다.
- 씬 전체 5,578개 YAML 문서를 검사한 결과 중복 문서 ID, 중복 자식, 다중 부모, 잘못된 클래스 ID, NaN/Infinity는 없었다.
- Canvas와 GraphicRaycaster를 제거한 뒤에도 동일하게 재현되어 패널 구성 요소가 직접 원인은 아니다.
- 프로젝트의 GPU Resident Drawer는 비활성화 상태다.
- 현재 Unity 6000.0.68f1 이후 6000.0.69f1에는 큰 RectTransform bounds 관련 uGUI 네이티브 크래시 수정이 포함되어 있다.

## 결론 및 안전한 대응

1. Hierarchy 검색 필터가 활성화된 동안 Scene View에서 같은 위치를 반복 클릭하지 않는다.
2. Scene View에서 패널 자식을 선택해야 할 때는 검색어를 먼저 지운다.
3. 협업 프로젝트의 Unity 버전은 개인이 변경하지 않는다.
4. Unity 패치 버전 변경은 팀 합의 후 별도 브랜치와 CI에서 검증한다.
5. 원인이 아니었던 Canvas/정렬 변경은 원래 상태로 복구한다.

## 현재 적용 범위

- 프로젝트 Unity 버전은 6000.0.68f1로 유지한다.
- 런타임 코드, 씬 구조 및 전투 로직은 변경하지 않는다.
- 조사 결과와 에디터 작업 우회 절차만 문서화한다.

## 멀티플레이 경계

에디터의 Scene View 선택 동작에만 해당한다. 전투 상태 및 `Command -> State Change -> Result/Event` 흐름에는 영향이 없다.
