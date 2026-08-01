# 배양조 인벤토리 및 유물 상점 피드백 설계

## 목표

- `CultureTankPanel`을 ESC 우선 닫기 대상으로 추가한다.
- 빈 `CultureTankRow_1~3`을 선택하면 패널 내부 `inventory/item`의 8개 이미지 슬롯에 로비 가방 아이템을 표시한다.
- 내부 슬롯에서 아이템을 선택하면 기존 배양 연구 서비스를 통해 연구를 시작한다.
- 유물 상점 상품 호버 시 설명과 아이콘 확대 피드백을 제공한다.
- 씬에 지정된 `RefreshIcon` 스프라이트가 런타임 초기화로 교체되지 않게 한다.

## 배양조 데이터 흐름

`LobbyCultureTankPanelPresenter`가 선택된 `LobbyCultureTankController`와 패널 내부 슬롯 표시만 담당한다. 연구 상태 변경은 기존 `CultureTankResearchService.TryStartResearch(lobby, tankId, itemId, startedAtUtcTicks, out error)`를 사용한다.

1. 빈 배양조 행 클릭 시 해당 컨트롤러를 선택한다.
2. `LobbyRuntimeData.BagItemIds`를 읽어 `inventory/item` 아래 슬롯에 아이콘을 표시한다.
3. 슬롯 클릭 시 선택된 `TankId`와 `ItemId`로 연구 시작을 요청한다.
4. 서비스가 가방 아이템 제거와 연구 런타임 추가를 원자적으로 수행한다.
5. 성공 후 저장, 호스트 스냅샷 발행, 배양조 행과 내부 인벤토리를 갱신한다.

연구 중인 행은 기존 상태 표시를 유지하며 인벤토리 선택 모드로 진입하지 않는다. 완료 행은 기존 보상 수령 흐름을 유지한다. 호스트 권한 제한과 안정적인 `TankId`/`ItemId` 경계를 유지한다.

## 패널 내부 인벤토리

- `CultureTankPanel/inventory/item` 아래 직계 자식 8개를 씬 배치 순서대로 슬롯으로 사용한다.
- 각 슬롯에서 표시용 `Image`를 찾고 필요하면 클릭용 `Button`을 연결한다.
- `BagItemIds` 순서대로 `ItemIconDatabase` 아이콘을 지정한다.
- 아이템이 없는 슬롯은 아이콘을 비우고 입력을 비활성화한다.
- 선택 가능한 배양조가 없거나 패널이 닫히면 선택 상태를 해제한다.
- 별도 `BagPanel`은 열지 않는다.

## ESC 닫기

`LobbyMainPanelKeyboardInputController`에 `cultureTankPanel`과 `LobbyCultureTankPanelPresenter` 참조를 추가하고 씬 이름으로 자동 연결한다. 우선순위는 유물 상점, 배양조, 침식도 선택, 캐릭터 설정 순서다. 배양조는 프리젠터의 `Close()`를 호출해 내부 선택 상태도 정리한다.

## 유물 상점 호버

`LobbyRelicOfferButtonUI`가 포인터 진입/이탈 이벤트를 받고 현재 상품의 유물 ID를 프리젠터에 전달한다. 호버 중 `RelicIcon`을 초기 크기의 1.12배로 확대하고 이탈, 판매 완료, 빈 슬롯, 비활성화 시 원래 크기로 복원한다.

`LobbyRelicShopPresenter`는 `RelicDatabase`에서 `RelicData.Name`과 `EffectDesc`를 읽어 씬에 이미 존재하는 `relic_name`과 `relic_effect`에 표시한다. `relic_info` 이미지는 항상 활성 상태를 유지하고, 호버가 끝나면 두 텍스트 내용만 비운다. 런타임에는 설명 텍스트를 생성하지 않는다.

## 배양조 행 텍스트

각 `CultureTankRow_1~3`은 이름과 상태를 분리한다. 기존 `Label`에는 `배양조 1`과 같은 이름만 표시하고, 별도 `StateLabel`에는 `비어 있음`, `배양중 120s`, `완료`, `데이터 없음` 중 현재 상태만 표시한다.

패널 내부 아이템 선택은 월드 오브젝트 클릭 차단 규칙을 다시 적용하지 않는다. 프리젠터가 선택한 `TankId`와 슬롯의 `ItemId`를 전달하면 호스트 권한과 실제 가방 소유 여부는 기존 연구 서비스가 검증한다.

## 리롤 아이콘

`LobbyRelicRefreshButtonUI.Initialize`는 콜백만 연결하고 `RefreshIcon.sprite`를 변경하지 않는다. 씬에 저장된 스프라이트가 단일 원본이 되며 `LobbyRelicShopPresenter.relicRefreshIcon` 의존성을 제거한다.

## 오류 처리

- 데이터 매니저, 가방, 아이콘 또는 선택 배양조가 없으면 슬롯을 비우고 입력을 막는다.
- 연구 시작 실패 시 데이터는 바꾸지 않고 경고 UI와 로그를 남긴다.
- 유물 설명 데이터가 비어 있으면 이름은 ID로 대체하고 설명은 빈 문자열로 처리한다.

## 검증

- EditMode 테스트로 슬롯 매핑, 연구 선택 전달, ESC 닫기, 호버 확대/복원, 리롤 스프라이트 보존을 검증한다.
- Unity 에디터가 열려 있으므로 batchmode는 실행하지 않는다.
- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 MSBuild로 빌드한다.

## 멀티플레이 경계

UI는 `TankId`와 `ItemId`를 전달할 뿐 연구 결과를 계산하지 않는다. 상태 변경과 가방 차감은 기존 호스트 권한 기반 연구 서비스에서 처리하고 기존 스냅샷 발행 흐름을 유지한다.
