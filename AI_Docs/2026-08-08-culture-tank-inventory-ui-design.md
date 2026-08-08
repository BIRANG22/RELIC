# CultureTankPanel 소유 아이템 UI 설계

## 목표

CultureTankPanel 내부에 로비 BagPanel과 같은 형태의 8칸 아이템 슬롯을 배치한다. 사용자는 빈 CultureTankRow_1~3을 먼저 선택한 뒤 소유 아이템 슬롯을 눌러 해당 재료를 투입한다.

## UI 범위

- BagPanel의 슬롯 배경, 아이콘, 선택 강조 형태를 재사용한다.
- 슬롯은 CultureTankPanel/Inventory/SlotRoot 아래에 8개 배치한다.
- BattleBagItemSlotUI를 슬롯 표현과 클릭 처리에 재사용한다.
- 툴팁, 상세 정보 패널, 버리기 버튼 및 BattleBagPanelUI는 포함하지 않는다.

## 상호작용

- 빈 배양조 행을 클릭하면 해당 행이 선택 강조된다.
- 선택된 행이 없으면 아이템 슬롯은 클릭할 수 없다.
- 행 선택 후 소유 아이템을 클릭하면 CultureTankResearchService.TryPlaceIngredient로 투입한다.
- 투입 성공 시 아이템은 가방에서 제거되고 행 아이콘에 표시되며 행 선택은 해제된다.
- 채워진 행을 클릭하면 기존 동작대로 재료를 가방에 반환한다.
- 완성 결과물이 남아 있거나 로컬 사용자가 상태를 변경할 권한이 없으면 재료 선택을 막는다.

## 책임 분리

- CultureTankResearchService: 가방과 배양조 재료 상태 변경을 담당한다.
- LobbyCultureTankPanelPresenter: 행 선택, 슬롯 표시, 클릭 콜백과 갱신을 담당한다.
- BattleBagItemSlotUI: 슬롯의 아이콘과 강조 표시만 담당하고 상태 계산은 하지 않는다.

## 검증

- 로비 씬 PreviewScene에서 CultureTankPanel 내부 슬롯 8개와 컴포넌트 배치를 확인한다.
- CultureTankPanel 내부에 툴팁, 버리기 버튼, BattleBagPanelUI가 없는지 확인한다.
- 행 선택·변경 권한·완성 결과 상태에 따른 아이템 선택 가능 조건을 단위 테스트한다.
- 런타임 및 에디터 C# 프로젝트를 빌드한다.
