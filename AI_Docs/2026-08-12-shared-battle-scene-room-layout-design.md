# Battle 씬 Room 공용 계층 설계

## 목표

Start, Battle, Event, Rest Room에 중복 배치된 배경·맵 비주얼·비전투 파티 표시·공통 UI를 하나의 공용 계층으로 통합한다. 각 Room에는 해당 Room의 진행 로직과 전용 연출/UI만 남긴다.

## 계층

```text
RoomRoot
├─ SharedRoomRoot                     항상 활성
│  ├─ SharedWorldRoot
│  │  ├─ StageBackground
│  │  ├─ MapVisualRoot
│  │  └─ SharedPartyPresentationRoot  Start/Event/Rest/지도 전환용
│  └─ SharedUICanvas
│     ├─ InventoryPanel
│     ├─ BagPanel
│     ├─ MapPanel
│     └─ SharedRewardPanel
└─ RoomContents
   ├─ StartRoomContent
   ├─ BattleRoomContent
   │  ├─ View/UnitRoot                실제 전투 유닛과 Grid
   │  └─ BattleHUDCanvas              전체 전투 전용
   ├─ EventRoomContent
   └─ RestRoomContent
```

## 공용화 경계

- 공용: 스테이지 배경, `MapVisualController`, 비전투 파티 표시와 앵커, Inventory, Bag, Map, 보상 표시 View.
- 전투 전용: `BattleHUDCanvas` 전체, Grid, 전투 UnitRoot, Spawner, 예약/타임라인, 전투 결과 판정과 정리.
- Start 전용: NPC 대화, 유물 선택과 획득 연출.
- Event 전용: 상자, 데이터 이벤트 선택지, 이벤트 결과 진행.
- Rest 전용: 회복·강화·상점 선택과 NPC 연출.

## 전환 책임

`BattleSceneController`가 Room 콘텐츠 하나만 활성화하고 `SharedRoomRoot`는 끄지 않는다. 컨트롤러가 공용 `StageBackgroundController`, `MapVisualController`, 파티 표시 루트를 명시적으로 참조하며 노드 진입 시 LayerIndex와 MapId를 전달한다. 비전투 Room Controller는 공용 파티 표시 앵커를 참조한다.

## 보상 UI

보상 목록 렌더링과 획득 입력은 `SharedRewardPanel` 하나가 담당한다. 패널은 보상 처리 완료 콜백만 호출하며 Room 종료를 직접 결정하지 않는다.

- Battle 완료 정책: 노드 클리어, `BattleRewardCollector` 정리, `BattleRoomCleaner`, 지도 복귀.
- Event 완료 정책: 이벤트 흐름의 다음 단계 또는 지도 복귀.

따라서 UI/VFX는 결과를 계산하지 않고 Battle/Event Controller가 전달한 결과와 완료 정책만 재생한다.

## 마이그레이션 원칙

1. 공용 루트와 명시적 참조를 먼저 도입한다.
2. 기존 Room 하위 검색은 임시 폴백으로 유지해 씬 전환 중 누락을 방지한다.
3. 씬 오브젝트 이동 후 중복 배경과 Event 전용 보상 View를 제거한다.
4. `BattleHUDCanvas`와 전투 UnitRoot는 이동하지 않는다.

## 검증

- Room 전환 시 `SharedRoomRoot`가 계속 활성 상태인지 검증한다.
- 모든 Room 진입이 동일한 배경 및 MapVisual 인스턴스를 사용하는지 검증한다.
- Start/Event/Rest에서 같은 파티 표시 루트를 사용하는지 검증한다.
- BattleHUDCanvas가 BattleRoomContent 하위에 유지되는지 씬 구조 테스트로 검증한다.
- Battle/Event 보상 종료 정책이 서로 섞이지 않는지 EditMode 테스트로 검증한다.
