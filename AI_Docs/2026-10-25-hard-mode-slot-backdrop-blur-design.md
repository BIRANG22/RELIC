# HardModeSlot 배경 블러 설계

## 의도

기존 `HardModeSlot` 내부의 아이콘용 붉은 블러는 유지하면서, 각 `HardModeSlot` 오브젝트 자체보다 뒤에 별도의 외곽 블러를 배치한다.

## 접근 방식

- 선택: `ExplorationReportSelectedErosionSlots` 아래에 블러와 슬롯을 형제 오브젝트로 배치한다. 블러는 대응 슬롯 바로 앞 형제 순서에 두어 슬롯 전체보다 뒤에 렌더한다.
- 대안: 슬롯 자식으로 추가하면 슬롯 내부 레이어에 머물러 요청한 오브젝트 뒤 배치가 명확하지 않다.
- 대안: 공용 `StorageSlotUI.prefab`에 배경을 넣으면 다른 슬롯까지 영향을 주므로 제외한다.

## 설계

- 기존 `HardModeRedBlur` 자식과 전용 프리팹은 유지한다.
- 새 `ExplorationResultHardModeBackdropBlur.prefab`을 추가한다. 기존 글로우 Sprite를 넓게 사용하고, 붉은 반투명 색상과 `raycastTarget = false`를 적용한다.
- `ExplorationResultPanelUI`는 `hardModeBackdropBlurPrefab`과 생성된 블러 리스트를 관리한다.
- 최대 침식 슬롯 수만큼 블러를 한 번 생성하고 `HardModeSlotBackdropBlur_n`으로 이름을 지정한다.
- 각 블러는 대응 슬롯과 같은 위치·스케일을 사용하되 프리팹의 더 큰 RectTransform 크기를 유지한다.
- 형제 순서는 `BackdropBlur_n → HardModeSlot_n`이 되도록 매 바인딩 때 정리한다.
- 대응 슬롯이 비어 있으면 배경 블러도 비활성화한다.

## 검증

- EditMode 에셋/소스 테스트로 별도 프리팹, 씬 참조, 형제 배치, 위치 동기화, 활성 상태 동기화를 검증한다.
- 테스트 포함 Editor 프로젝트를 컴파일하고 `git diff --check`를 실행한다.
- Unity 에디터에서 실제 외곽 범위와 투명도를 수동 확인한다.

## 멀티플레이 경계

정산 UI의 렌더 계층만 변경한다. 전투 결과, 침식 선택 데이터 및 동기화 구조에는 영향이 없다.
