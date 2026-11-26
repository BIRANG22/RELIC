# 공용 소모품 4슬롯 구현 계획

> **구현 규칙:** 승인된 `2026-11-26-battle-timeline-record-consumable-redesign.md`의 4단계를 현재 작업 공간에서 TDD로 순차 구현한다. 프로젝트 규칙에 따라 커밋·브랜치·worktree 작업은 수행하지 않는다.

**목표:** 캐릭터별 연성제 장착과 다회 사용을 제거하고, 탐사 파티가 동일 ID 중복을 허용하는 공용 소모품 네 개를 가져가 전투 예약 단계에서 슬롯별 한 번씩 즉시 사용하게 한다.

**아키텍처:** 로비 장착 ID, 전투 슬롯 ID/사용 여부, 보관함 수량을 분리한다. 전투 사용은 `UseConsumableCommand -> 호스트 검증/상태 변경 -> UseConsumableResult` 흐름으로 처리하고 기존 `ActiveRelicService`의 효과 판정만 재사용한다. UI는 확정된 런타임과 결과만 표시한다.

**기술:** Unity 6, C#, JsonUtility 저장/네트워크 스냅샷, 씬 YAML, NUnit EditMode 테스트

**설계:** `AI_Docs/2026-11-26-battle-timeline-record-consumable-redesign.md`

## 전역 제약

- 문서와 테스트는 각각 `AI_Docs`, `Assets/Tests/EditMode~` 아래에만 둔다.
- Unity batchmode와 에디터 직접 제어를 사용하지 않는다.
- 슬롯은 정확히 네 개이며 동일 `ItemId` 중복을 허용한다.
- 전투 상태 변경은 안정적인 ID와 명령/결과 구조를 사용한다.
- UI는 소모품 효과나 사용 성공을 계산하지 않는다.

## 검토 중점

- 구버전 캐릭터 세 명이 같은 연성제를 장착해도 수량 손실 없이 공용 슬롯/보관함으로 이전되는가.
- 동일 ID 네 개의 장착이 보유 수량 네 개일 때 허용되고 세 개일 때 거부되는가.
- 빈 슬롯, 사용 완료 슬롯, 실행 중 턴, 잘못된 `ItemId`/대상/소유자 요청이 상태를 변경하지 않는가.
- 네트워크 재접속 후 슬롯 ID와 사용 완료 상태가 호스트와 동일한가.
- 사용하지 않은 소모품과 사용한 소모품이 탐사 종료 정산에서 중복 반환되지 않는가.

---

### 작업 1: 공용 슬롯 데이터와 구버전 마이그레이션

**파일**
- 생성: `Assets/Project/Scripts/Gameplay/Data/Runtime/PartyConsumableRuntime.cs`
- 수정: `LobbyRuntimeData.cs`, `LobbyRuntimeStore.cs`, `BattleRuntimeData.cs`, `SaveSystem.cs`
- 테스트: `Assets/Tests/EditMode~/PartyConsumableRuntimeTests.cs`

**인터페이스**
- `PartyConsumableRuntime.NormalizeLobby(LobbyRuntimeData)`
- `PartyConsumableRuntime.MigrateCharacterCompounds(LobbyRuntimeData, IEnumerable<CharacterRuntimeData>)`
- `PartyConsumableRuntime.TryEquip(LobbyRuntimeData, int, string)` / `TryUnequip(...)`
- `BattleConsumableSlotRuntimeData { ItemId, IsUsed }`

- [ ] 보유 수량, 중복 장착, 정확히 네 슬롯, 구버전 이전 테스트를 먼저 실패시킨다.
- [ ] 최소 데이터/정책 구현으로 통과시킨다.
- [ ] 저장 정규화에서 이전을 한 번만 수행하고 캐릭터 슬롯 0을 비운다.

### 작업 2: 탐사 시작 전달과 종료 정산

**파일**
- 수정: `LobbyBattleRuntimeTransferService.cs`, `BattleRuntimeData.cs` 및 기존 탐사 결과 정산 서비스
- 테스트: `PartyConsumableRuntimeTests.cs`

**인터페이스**
- `PartyConsumableRuntime.CopyToBattle(LobbyRuntimeData, BattleRuntimeData)`
- `PartyConsumableRuntime.ReturnUnusedToLobby(BattleRuntimeData, LobbyRuntimeData)`

- [ ] 시작 시 ID 네 개와 `IsUsed=false`가 복사되는 실패 테스트를 작성한다.
- [ ] 사용 완료 슬롯은 반환하지 않고 미사용 슬롯만 한 번 반환하는 실패 테스트를 작성한다.
- [ ] 전달/정산 경로를 구현하고 테스트를 통과시킨다.

### 작업 3: 전투 사용 명령과 결과

**파일**
- 생성: `UseConsumableCommand.cs`, `BattleConsumableService.cs`
- 수정: `ActiveRelicService.cs`, `ActiveRelicTargetingController.cs`
- 테스트: `Assets/Tests/EditMode~/BattleConsumableServiceTests.cs`

**인터페이스**
- `UseConsumableCommand { RequestId, ActorCharacterId, SlotIndex, ItemId, TargetGridIndex }`
- `UseConsumableResult { Succeeded, RejectReason, SlotIndex, ItemId, IsUsed }`
- `BattleConsumableService.Validate(...)`와 성공 시에만 상태를 한 번 변경하는 `TryUse(...)`

- [ ] 빈/사용됨/ID 불일치/예약 단계 외/죽은 사용자 검증 테스트를 실패시킨다.
- [ ] 성공한 즉시 효과만 슬롯을 사용 완료로 만드는 테스트를 실패시킨다.
- [ ] 기존 연성제 효과 적용을 슬롯 사용 상태와 분리해 구현한다.

### 작업 4: 호스트 권위 네트워크 동기화

**파일**
- 수정: `BattleNetworkModels.cs`, `SteamBattleStateSynchronizer.cs`, `BattleNetworkSerialization.cs`
- 테스트: `BattleConsumableServiceTests.cs` 및 직렬화 테스트

**인터페이스**
- `BattleNetworkCommandType.UseConsumable`
- 스냅샷은 `BattleRuntimeData.ConsumableSlots`를 포함한다.

- [ ] 명령 직렬화와 슬롯 상태 왕복 테스트를 실패시킨다.
- [ ] 호스트만 검증/적용하고 클라이언트는 스냅샷 결과만 반영하게 한다.
- [ ] 중복 `RequestId`가 두 번 소비하지 않는지 검증한다.

### 작업 5: 로비 공용 장착 UI

**파일**
- 수정: `LobbyEquipPanelUI.cs`, `LobbyInfoPanelUI.cs`, `Assets/Project/Scenes/YDM/Lobby.unity`
- 테스트: `PartyConsumableRuntimeTests.cs` 및 씬 YAML 구조 검사

- [ ] 캐릭터 슬롯 0 기반 장착 코드를 공용 네 슬롯 정책 호출로 교체한다.
- [ ] 공용 슬롯 네 개를 탐사 준비 영역에 명시적으로 배치/연결한다.
- [ ] 보관함 실제 개수보다 많이 장착할 수 없고 중복 ID는 허용되는지 검증한다.

### 작업 6: 전투 2×2 슬롯과 Undo/Redo 재배치

**파일**
- 생성: `BattleConsumableSlotUI.cs`, `BattleConsumablePanelUI.cs`
- 수정: `BattleCharacterPanelUI.cs`, `ActiveRelicTargetingController.cs`, `Assets/Project/Scenes/YDM/Battle.unity`
- 테스트: 씬 YAML 구조 검사 및 서비스 테스트

- [ ] 기존 Compound 버튼/문구 입력을 제거한다.
- [ ] 네 슬롯은 선택 캐릭터와 무관한 전투 런타임을 표시하고 클릭 시 명령만 요청한다.
- [ ] 대상 지정 성공 결과 후에만 사용 완료 시각을 적용한다.
- [ ] Undo/Redo 버튼을 기존 Compound 위치로 옮기고 Z/X 예약 이력 동작은 보존한다.
- [ ] 기존 Undo/Redo 영역에 네 슬롯을 2×2로 명시 배치한다.

### 작업 7: 전체 검증

- [ ] 관련 EditMode 테스트 소스를 RED→GREEN 기록과 함께 컴파일한다.
- [ ] `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 컴파일을 통과시킨다.
- [ ] `git diff --check`, 씬/프리팹 GUID와 직렬화 필드, Unity 로그 컴파일 오류를 확인한다.
- [ ] 실제 에디터 화면과 Steam 재접속은 검증하지 못한 항목으로 보고한다.
