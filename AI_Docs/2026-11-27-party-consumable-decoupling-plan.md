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

- [ ] 효과/명령: BattleConsumableService, ActiveRelicService 및 의미 있는 회귀 테스트. 파티 전체 적용, 대상 ID 검증, 슬롯 단일 소비, 실패/중복 거부.
- [ ] 로비: LobbyEquipPanelUI, LobbyInfoPanelUI 및 Lobby.unity/공용 슬롯 프리팹. 반복 슬롯은 명시적 참조로 연결. 캐릭터 Compound 계층과 관련 장착 분기 제거.
- [ ] 네트워크: 두 Steam synchronizer 및 command/policy 테스트. 파티 회원 권한, 이동 대상 ID 전달, 호스트 단일 소비/스냅샷.
- [ ] 전투 UI/타겟팅: BattleConsumablePanelUI, ActiveRelicTargetingController, BattleCharacterPanelUI. 선택 캐릭터 의존 제거, 이동/교환 2단계 타겟 선택 및 취소.
- [ ] 통합: 전체 C# 컴파일, 가능한 독립 테스트, 씬/프리팹 참조와 배치 검사, diff 검사 및 독립 리뷰.

## 검증 제약

문서는 AI_Docs 내부, 테스트는 Assets/Tests/EditMode~/ 또는 PlayMode~/ 내부에만 작성. Unity 직접 제어/batchmode 금지. 실제 클릭/연출 및 멀티 실접속은 미검증으로 보고. 스냅샷/명령은 안정적 ID를 사용하고 UI는 효과를 계산하지 않는다.

## 진행 기록

- 기존 공용 4슬롯은 존재하나 로비가 partySlotIndex/characterId로 연결하고 전투 service가 ActorCharacterId를 필수로 요구하는 상태를 확인.
- Compound_01~06은 Self 효과, 07은 이동, 08은 교환, 09~15는 칸/적 지정 효과임을 런타임 CSV에서 확인.
