# 파티 공용 소모품 장착 및 전체 적용 설계·구현 계획

사용자 승인: `진행해`. 현재 브랜치/작업 공간에서 완료까지 연속 진행. 커밋/Push/PR/브랜치/worktree 작업 없음.

## 승인된 동작

- 로비 CharBtn_0~5의 Compound 슬롯 및 캐릭터 클릭 장착 연결 제거.
- PlayButton 오른쪽에 공용 슬롯 4개 가로 배치. 보관함 선택 후 슬롯 장착/교체/해제. 캐릭터 교체/순서와 독립.
- 전투 공용 슬롯은 캐릭터 선택 없이 사용. 자기 강화/회복/정화 효과는 전투 파티의 생존 캐릭터 모두에게 각각 적용하고 슬롯은 한 번만 소비.
- 장판/장애물/적 약화는 지정 대상에 한 번 적용. 이동은 사용 시 아군→빈 칸, 교환은 아군→다른 아군을 선택. 장착 소유자는 없음.
- 멀티플레이는 파티 참가자의 공용 명령을 호스트가 검증·적용. 캐릭터 소유권을 소모품 권한으로 사용하지 않음. 비회원/중복 요청/이미 사용한 슬롯은 거부.
- 기존 공용 4슬롯 저장 데이터와 미사용 반환/구버전 이전을 재사용.

## 구현 경계 및 인터페이스

- `UseConsumableCommand`: RequestId, SlotIndex, ItemId, TargetGridIndex, TargetCharacterId. ActorCharacterId 제거. TargetCharacterId는 이동/교환 사용 대상일 뿐 소유자 아님.
- `ActiveRelicService.GetConsumableAvailability(string itemId)`: 캐릭터와 무관한 DB 기반 사용 유형.
- `BattleConsumableService.GetPartyMembers(PartyRuntimeStore party, CharacterRuntimeStore characters)`: 현재 파티 ID 목록으로 런타임 조회.
- `BattleConsumableService.TryUseParty(BattleRuntimeData battle, UseConsumableCommand command, IReadOnlyList<CharacterRuntimeData> party, bool isReservationPhase, ActiveRelicService effectService, GridManager gridManager = null, BattleGridEffectController gridEffects = null)`: 단일 공용 사용 진입점. 실패하면 슬롯 미소비, 전체 적용은 성공한 대상이 하나 이상일 때 한 번 소비. 효과 중복/사망/빈 대상의 기존 개별 규칙 유지.
- 로비 네트워크 요청: RequestEquipConsumable(int slotIndex, string itemId), RequestUnequipConsumable(int slotIndex). 일반 장비 명령의 캐릭터 검사는 유지.

## 작업 단위

- [x] 효과/명령: BattleConsumableService, ActiveRelicService 및 의미 있는 회귀 테스트. 파티 전체 적용, 대상 ID 검증, 슬롯 단일 소비, 실패/중복 거부.
- [x] 로비: LobbyEquipPanelUI, LobbyInfoPanelUI 및 Lobby.unity/공용 슬롯 프리팹. 반복 슬롯은 명시적 참조로 연결. 캐릭터 Compound 계층과 관련 장착 분기 제거.
- [x] 네트워크: 두 Steam synchronizer 및 command/policy 테스트. 파티 회원 권한, 이동 대상 ID 전달, 호스트 단일 소비/스냅샷.
- [x] 전투 UI/타겟팅: BattleConsumablePanelUI, ActiveRelicTargetingController, BattleCharacterPanelUI. 선택 캐릭터 의존 제거, 이동/교환 2단계 타겟 선택 및 취소.
- [x] 통합: 전체 C# 컴파일, 가능한 독립 테스트, 씬/프리팹 참조와 배치 검사, diff 검사 및 코드 경로 리뷰.

## 검증 제약

문서는 AI_Docs 내부, 테스트는 Assets/Tests/EditMode~/ 또는 PlayMode~/ 내부에만 작성. Unity 직접 제어/batchmode 금지. 실제 클릭/연출 및 멀티 실접속은 미검증으로 보고. 스냅샷/명령은 안정적 ID를 사용하고 UI는 효과를 계산하지 않는다.

## 진행 기록

- 기존 공용 4슬롯은 존재하나 로비가 partySlotIndex/characterId로 연결하고 전투 service가 ActorCharacterId를 필수로 요구하는 상태를 확인.
- Compound_01~06은 Self 효과, 07은 이동, 08은 교환, 09~15는 칸/적 지정 효과임을 런타임 CSV에서 확인.

## 구현 및 검증 결과 (2026-11-27)

- CharBtn_0~5의 Compound 하위 계층 6개(114개 YAML 오브젝트) 제거, CharBtn 직렬화 참조 해제.
- 기존 슬롯 디자인으로 LobbyConsumableSlot.prefab을 만들고 PlayButton 오른쪽에 60×60 슬롯 4개를 씬에 배치. 부모/인벤토리 패널/슬롯 참조는 직렬화 연결.
- 인벤토리에서 선택 후 공용 슬롯을 클릭하여 장착/교체. 선택 없이 장착 슬롯 클릭 또는 우클릭으로 해제. 캐릭터 클릭/드래그 장착 경로는 차단.
- Self 효과는 현재 파티의 생존 CharacterId마다 한 번 적용. 중복 ID/벤치 캐릭터 제외. 성공한 대상이 하나 이상일 때 슬롯 한 번 소비.
- 이동/교환은 아군 칸 선택 후 목적 칸 선택. 교환 목적지는 다른 생존 파티원으로 검증. 파티 위치 저장소를 서비스 인자로 전달하여 새 검증이 DataManager/Scene 검색에 의존하지 않게 함.
- 파티 상태 및 사용 명령은 BattleConsumableService에서 처리하고 UI는 결과 후 표시 갱신. 기존 이동/장판 효과 구현의 씬 의존은 유지되며 이번 작업에서 네트워크 프레임워크를 코어에 추가하지 않음.
- 로비/전투 호스트는 Steam 로비 회원을 확인하고 소모품에는 캐릭터 소유권을 요구하지 않음. 요청 ID 중복 검사 및 사용 슬롯 재소비 방지. 일반 장비의 소유권 검사 유지.
- BattleNetworkCommand.targetCharacterId 전달을 추가하고 전투 프로토콜을 4→5로 변경. 멀티 참여자는 같은 빌드 사용 필요.
- STEAMWORKS_NET 포함/제외 전체 Assembly-CSharp 컴파일 모두 성공.
- 실제 런타임 DLL을 이용한 관리형 NUnit 17개 통과: 파티 버프·마나 회복·사망 제외·중복 제외·4번 슬롯 소비·실패/잘못된 요청·벤치 제외·공용 보관함·저장 이전/반환.
- 기존 저장 이전 테스트 2개의 null 기대값을 기존 NormalizeLobby의 빈 문자열 표현에 맞게 수정. 런타임 저장 형식 변경 없음.
- 씬/프리팹 검사 통과: 4개 프리팹 인스턴스, 슬롯 인덱스/부모 참조, 정사각형 및 PlayButton 오른쪽 간격, 스크립트 GUID, 새 누락 참조/중복 YAML ID 없음.
- Unity 에디터 직접 제어 및 batchmode 미실행. 실제 클릭/드래그, 해상도별 배치, 이동·교환 연출, Steam 2인 실접속은 미검증.
- 커밋/Push/PR/브랜치/worktree 작업 없음.

### 변경 파일

- Assets/Project/Scenes/YDM/Lobby.unity
- Assets/Project/PrefabsR/LobbyConsumableSlot.prefab (+ meta)
- Assets/Project/Scripts/LobbyConsumablePanelUI.cs (+ meta)
- Assets/Project/Scripts/LobbyConsumableSlotUI.cs (+ meta)
- Assets/Project/Scripts/LobbyEquipPanelUI.cs
- Assets/Project/Scripts/LobbyInfoPanelUI.cs
- Assets/Project/Scripts/BattleCharacterPanelUI.cs
- Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Relic/BattleConsumableService.cs
- Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Relic/ActiveRelicService.cs
- Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Relic/BattleConsumablePanelUI.cs
- Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Relic/ActiveRelicTargetingController.cs
- Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/BattleNetworkModels.cs
- Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/BattleNetworkSerialization.cs
- Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/SteamBattleStateSynchronizer.cs
- Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbySharedStateSynchronizer.cs
- Assets/Tests/EditMode~/BattleConsumableServiceTests.cs
- Assets/Tests/EditMode~/PartyConsumableRuntimeTests.cs
- Assets/Tests/EditMode~/Invoke-PartyConsumableChecks.ps1
- Assets/Tests/EditMode~/Verify-PartyConsumableScene.cjs
- AI_Docs/2026-11-27-party-consumable-decoupling-plan.md
