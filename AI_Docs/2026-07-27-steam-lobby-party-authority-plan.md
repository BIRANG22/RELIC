# Steam 로비 파티 권한 동기화 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:test-driven-development` for each implementation task and `superpowers:verification-before-completion` before completion. Do not create commits or PRs without explicit user approval.

**Goal:** 호스트가 확정하는 3슬롯 파티 상태를 모든 Steam 로비 참가자에게 동기화하고, 참가 순서에 따른 슬롯 소유권과 캐릭터 중복 금지를 보장한다.

**Architecture:** Unity 및 Steam API와 무관한 `LobbyPartyAuthorityState`가 슬롯 소유권, 참가 순서, 캐릭터와 Revision을 계산한다. `SteamLobbyPartySynchronizer`가 Steam 로비 채팅 명령과 호스트 소유 로비 스냅샷을 중계하며, UI는 이 동기화 계층을 통해서만 네트워크 파티 변경을 요청한다. 기존 `PartyRuntimeStore`에는 확정된 캐릭터 구성만 적용하여 전투·저장·싱글플레이 경계를 유지한다.

**Tech Stack:** Unity 6, C# 9, Steamworks.NET, Unity `JsonUtility`, NUnit EditMode tests

## 전역 제약

- 문서 파일은 `AI_Docs/` 내부에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 작성하여 빌드 대상에서 제외한다.
- Unity 에디터가 열려 있으므로 Unity batchmode 테스트는 실행하지 않는다.
- 코드 수정 시작 전 사용자 허락을 다시 확인한다.
- 커밋과 PR은 사용자에게 별도 허락을 받은 뒤에만 실행한다.
- 전투 핵심 로직과 Steam/UI 코드를 섞지 않는다.
- 파티 변경은 `Command -> State Change -> Snapshot/Event` 흐름을 따른다.
- Steam 로비 밖에서는 기존 싱글플레이 파티 편집 동작을 유지한다.
- 호스트가 나가면 호스트 마이그레이션 없이 로비를 종료하고 로컬 편집 상태로 복귀한다.

---

## 파일 구조

### 새로 생성

- `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyPartyModels.cs`
  - 명령, 슬롯, 스냅샷, 명령 처리 결과 타입
- `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyPartyAuthorityState.cs`
  - 참가 순서, 슬롯 재배치, 권한·중복·Revision 검증
- `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyPartySerialization.cs`
  - Steam 채팅 명령과 로비 데이터 스냅샷 직렬화
- `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyPartySynchronizer.cs`
  - Steam 콜백, 호스트 명령 처리, 스냅샷 발행·적용
- `Assets/Tests/EditMode~/LobbyPartyAuthorityStateTests.cs`
  - 순수 권한 상태와 재배치 테스트
- `Assets/Tests/EditMode~/LobbyPartySerializationTests.cs`
  - 직렬화 왕복 및 손상 데이터 거부 테스트
- `Assets/Tests/EditMode~/SteamLobbyPartyUiBoundaryTests.cs`
  - UI가 네트워크 로비에서 저장소를 직접 변경하지 않는지 확인하는 경계 테스트

### 수정

- `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyInviteController.cs`
  - 기존 멤버별 캐릭터 덮어쓰기를 제거하고 동기화기에 로비 생명주기 전달
- `Assets/Project/Scripts/Gameplay/Scene/Lobby/CharPick.cs`
  - 네트워크 로비에서는 단일 소유 슬롯에 대한 명령만 요청
- `Assets/Project/Scripts/Gameplay/Scene/Lobby/CharBtn.cs`
  - 직접 선택 경로도 네트워크 명령 경계를 통과
- `Assets/Project/Scripts/UI/Lobby/PartySlotButton.cs`
  - 소유하지 않은 슬롯 선택 차단
- `Assets/Project/Scripts/Gameplay/Scene/Lobby/PartySlot.cs`
  - 슬롯 소유 여부에 따른 상호작용 표시 갱신 훅

---

### Task 1: 순수 파티 모델과 최초 호스트 상태

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyPartyModels.cs`
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyPartyAuthorityState.cs`
- Test: `Assets/Tests/EditMode~/LobbyPartyAuthorityStateTests.cs`

**Interfaces:**
- Produces: `LobbyPartyAuthorityState.CreateHost(ulong hostSteamId, IReadOnlyList<string> characterIds)`
- Produces: `LobbyPartySnapshot CreateSnapshot()`
- Produces: `ulong HostSteamId`, `long Revision`, `IReadOnlyList<ulong> OrderedClientSteamIds`
- Produces: `LobbyPartySlotState GetSlot(int slotIndex)`

- [ ] **Step 1: 최초 호스트 상태의 실패 테스트 작성**

```csharp
[Test]
public void CreateHost_AssignsAllThreeSlotsToHostAndPreservesCharacters()
{
    LobbyPartyAuthorityState state = LobbyPartyAuthorityState.CreateHost(
        100UL,
        new[] { "Character_A", "Character_B", "Character_C" });

    Assert.That(state.GetSlot(0).OwnerSteamId, Is.EqualTo(100UL));
    Assert.That(state.GetSlot(1).OwnerSteamId, Is.EqualTo(100UL));
    Assert.That(state.GetSlot(2).OwnerSteamId, Is.EqualTo(100UL));
    Assert.That(state.GetSlot(0).CharacterId, Is.EqualTo("Character_A"));
    Assert.That(state.GetSlot(1).CharacterId, Is.EqualTo("Character_B"));
    Assert.That(state.GetSlot(2).CharacterId, Is.EqualTo("Character_C"));
    Assert.That(state.Revision, Is.EqualTo(1));
}
```

- [ ] **Step 2: Unity Test Runner에서 테스트가 컴파일 실패하는지 확인**

Run: Unity Test Runner에서 `LobbyPartyAuthorityStateTests.CreateHost_AssignsAllThreeSlotsToHostAndPreservesCharacters`

Expected: `LobbyPartyAuthorityState` 타입이 없어 컴파일 실패

- [ ] **Step 3: 모델 타입과 최소 호스트 생성 구현**

```csharp
public sealed class LobbyPartySlotState
{
    public int SlotIndex { get; }
    public ulong OwnerSteamId { get; }
    public string CharacterId { get; }
}

public sealed class LobbyPartySnapshot
{
    public ulong HostSteamId { get; }
    public long Revision { get; }
    public IReadOnlyList<ulong> OrderedClientSteamIds { get; }
    public IReadOnlyList<LobbyPartySlotState> Slots { get; }
}

public sealed class LobbyPartyAuthorityState
{
    public const int SlotCount = 3;

    public static LobbyPartyAuthorityState CreateHost(
        ulong hostSteamId,
        IReadOnlyList<string> characterIds);

    public LobbyPartySlotState GetSlot(int slotIndex);
    public LobbyPartySnapshot CreateSnapshot();
}
```

구현 시 입력 캐릭터가 3개보다 적으면 나머지는 빈 문자열로 채우고, 외부에는 변경 가능한 내부 배열을 노출하지 않는다.

- [ ] **Step 4: 해당 테스트 통과 확인**

Run: Unity Test Runner에서 `LobbyPartyAuthorityStateTests`

Expected: 최초 상태 테스트 PASS

- [ ] **Step 5: MSBuild 컴파일 확인**

Run:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal
```

Expected: `0 Error(s)`

---

### Task 2: 참가·퇴장 재배치 규칙

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyPartyAuthorityState.cs`
- Test: `Assets/Tests/EditMode~/LobbyPartyAuthorityStateTests.cs`

**Interfaces:**
- Consumes: Task 1의 `LobbyPartyAuthorityState`
- Produces: `LobbyPartyMembershipResult AddClient(ulong clientSteamId)`
- Produces: `LobbyPartyMembershipResult RemoveClient(ulong clientSteamId)`
- Produces: `bool ContainsMember(ulong steamId)`
- Produces: `LobbyPartyMembershipResult { bool Changed; LobbyPartySnapshot Snapshot; }`

- [ ] **Step 1: 두 번째·세 번째 참가 테스트 작성**

```csharp
[Test]
public void AddFirstClient_TransfersSlotTwoOwnershipWithoutMovingCharacter()
{
    LobbyPartyAuthorityState state = CreateABC();

    state.AddClient(200UL);

    AssertSlot(state, 0, 100UL, "A");
    AssertSlot(state, 1, 100UL, "B");
    AssertSlot(state, 2, 200UL, "C");
}

[Test]
public void AddSecondClient_SwapsSlotsOneAndTwoSoFirstClientKeepsCharacter()
{
    LobbyPartyAuthorityState state = CreateABC();
    state.AddClient(200UL);

    state.AddClient(300UL);

    AssertSlot(state, 0, 100UL, "A");
    AssertSlot(state, 1, 200UL, "C");
    AssertSlot(state, 2, 300UL, "B");
}
```

테스트 이름의 `SlotTwo`는 0 기반 인덱스 2, 즉 화면의 3번 슬롯을 의미하도록 주석을 붙인다.

- [ ] **Step 2: 참가 테스트 실패 확인**

Run: Unity Test Runner에서 위 두 테스트

Expected: `AddClient`가 없어 FAIL

- [ ] **Step 3: 참가 재배치 최소 구현**

```csharp
public LobbyPartyMembershipResult AddClient(ulong clientSteamId)
{
    // 중복 멤버와 3인 초과는 Changed=false
    // 첫 클라이언트는 슬롯 2 소유권만 이전
    // 둘째 클라이언트는 슬롯 1/2 캐릭터 교환 후 소유권 배정
    // 실제 변경 한 번당 Revision 1 증가
}
```

- [ ] **Step 4: 퇴장 경우 세 가지와 반복 콜백 테스트 작성**

```csharp
[Test] public void RemoveFirstClientFromThree_ReturnsSlotOneToHostWithoutMovingCharacters();
[Test] public void RemoveSecondClientFromThree_SwapsCharactersAndMovesFirstClientToSlotTwo();
[Test] public void RemoveOnlyClient_ReturnsSlotTwoToHostWithoutMovingCharacter();
[Test] public void RepeatingSameMembershipChange_DoesNotChangeRevisionOrSwapAgain();
```

각 테스트는 슬롯 0~2의 `OwnerSteamId`, `CharacterId`, 변경 전후 `Revision`을 모두 검증한다.

- [ ] **Step 5: 퇴장 테스트 실패 확인 후 최소 구현**

Run: Unity Test Runner에서 `LobbyPartyAuthorityStateTests`

Expected before implementation: 퇴장 테스트 FAIL  
Expected after implementation: 모든 참가·퇴장 테스트 PASS

- [ ] **Step 6: 전체 순수 상태 테스트 통과 확인**

Run: Unity Test Runner에서 `LobbyPartyAuthorityStateTests`

Expected: PASS

---

### Task 3: 캐릭터 변경 명령 검증

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyPartyModels.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyPartyAuthorityState.cs`
- Test: `Assets/Tests/EditMode~/LobbyPartyAuthorityStateTests.cs`

**Interfaces:**
- Produces: `LobbyPartyCharacterChangeCommand`
- Produces: `LobbyPartyCommandResult TryChangeCharacter(LobbyPartyCharacterChangeCommand command, Func<string, bool> isValidCharacterId)`
- Produces: `LobbyPartyCommandRejectReason` values `None`, `UnknownMember`, `InvalidSlot`, `NotSlotOwner`, `StaleRevision`, `InvalidCharacter`, `DuplicateCharacter`

- [ ] **Step 1: 승인과 거부 조건 테스트 작성**

```csharp
[Test] public void ChangeCharacter_BySlotOwner_UpdatesCharacterAndRevision();
[Test] public void ChangeCharacter_ByDifferentMember_IsRejected();
[Test] public void ChangeCharacter_ToCharacterUsedByOtherSlot_IsRejected();
[Test] public void ChangeCharacter_WithStaleRevision_IsRejected();
[Test] public void ChangeCharacter_WithUnknownCharacter_IsRejected();
```

거부 테스트마다 슬롯 데이터와 Revision이 변경되지 않았는지 함께 검증한다.

- [ ] **Step 2: 테스트 실패 확인**

Run: Unity Test Runner에서 캐릭터 명령 테스트

Expected: 명령 및 결과 타입이 없어 FAIL

- [ ] **Step 3: 명령 모델과 단일 검증 진입점 구현**

```csharp
public sealed class LobbyPartyCharacterChangeCommand
{
    public string RequestId { get; }
    public ulong RequesterSteamId { get; }
    public int SlotIndex { get; }
    public string RequestedCharacterId { get; }
    public long KnownRevision { get; }
}

public LobbyPartyCommandResult TryChangeCharacter(
    LobbyPartyCharacterChangeCommand command,
    Func<string, bool> isValidCharacterId);
```

검증 순서는 멤버 → 슬롯 범위 → 소유권 → Revision → 캐릭터 유효성 → 다른 슬롯 중복으로 고정한다. 승인할 때만 상태 변경과 Revision 증가를 수행한다.

- [ ] **Step 4: 캐릭터 명령 테스트 통과 확인**

Run: Unity Test Runner에서 `LobbyPartyAuthorityStateTests`

Expected: PASS

---

### Task 4: 명령·스냅샷 직렬화와 검증

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/LobbyPartySerialization.cs`
- Test: `Assets/Tests/EditMode~/LobbyPartySerializationTests.cs`

**Interfaces:**
- Consumes: `LobbyPartyCharacterChangeCommand`, `LobbyPartySnapshot`
- Produces: `string SerializeCommand(LobbyPartyCharacterChangeCommand command)`
- Produces: `bool TryDeserializeCommand(string payload, out LobbyPartyCharacterChangeCommand command)`
- Produces: `string SerializeSnapshot(LobbyPartySnapshot snapshot)`
- Produces: `bool TryDeserializeSnapshot(string payload, out LobbyPartySnapshot snapshot)`
- Produces: `bool ValidateSnapshot(LobbyPartySnapshot snapshot, ulong expectedHostSteamId, IReadOnlyCollection<ulong> currentMembers)`

- [ ] **Step 1: 왕복 및 손상 데이터 테스트 작성**

```csharp
[Test] public void Command_RoundTripsAllFields();
[Test] public void Snapshot_RoundTripsHostRevisionOrderOwnersAndCharacters();
[Test] public void Deserialize_RejectsMalformedPayload();
[Test] public void ValidateSnapshot_RejectsDuplicateCharacters();
[Test] public void ValidateSnapshot_RejectsUnknownSlotOwner();
[Test] public void ValidateSnapshot_RejectsWrongHost();
```

- [ ] **Step 2: 테스트 실패 확인**

Run: Unity Test Runner에서 `LobbyPartySerializationTests`

Expected: `LobbyPartySerialization`이 없어 FAIL

- [ ] **Step 3: `JsonUtility`용 DTO와 변환 구현**

```csharp
[Serializable]
private sealed class SnapshotDto
{
    public string hostSteamId;
    public long revision;
    public string[] orderedClientSteamIds;
    public SlotDto[] slots;
}
```

Steam ID는 JSON 숫자 정밀도나 Unity 직렬화 차이를 피하도록 10진수 문자열로 저장하고 `ulong.TryParse`로 복원한다. 슬롯은 정확히 3개이며 인덱스 0, 1, 2가 한 번씩 존재해야 한다.

- [ ] **Step 4: 직렬화 테스트 통과 확인**

Run: Unity Test Runner에서 `LobbyPartySerializationTests`

Expected: PASS

---

### Task 5: Steam 호스트 동기화 어댑터

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyPartySynchronizer.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/SteamLobby/SteamLobbyInviteController.cs`
- Test: `Assets/Tests/EditMode~/SteamLobbyPartyUiBoundaryTests.cs`

**Interfaces:**
- Consumes: Tasks 1~4의 상태, 명령, 스냅샷, 직렬화
- Produces: `SteamLobbyPartySynchronizer.Instance`
- Produces: `bool IsNetworkPartyActive`
- Produces: `long AppliedRevision`
- Produces: `bool CanLocalPlayerEditSlot(int slotIndex)`
- Produces: `bool IsCharacterUsedByOtherSlot(string characterId, int slotIndex)`
- Produces: `bool RequestCharacterChange(int slotIndex, string characterId)`
- Produces: `void EnterLobby(ulong lobbyId, ulong localSteamId, ulong ownerSteamId)`
- Produces: `void HandleLobbyMembershipChanged()`
- Produces: `void LeaveLobby(bool preserveCharacters)`

- [ ] **Step 1: 기존 잘못된 덮어쓰기 경계 테스트 작성**

```csharp
[Test]
public void InviteController_DoesNotMirrorEachMemberIntoPartyByMemberArrayIndex()
{
    string source = File.ReadAllText(InviteControllerPath);
    Assert.That(source, Does.Not.Contain("RefreshMembersIntoPartyRuntime"));
    Assert.That(source, Does.Not.Contain("partySlot\" + i"));
}
```

추가로 새 동기화기가 `RequestCharacterChange`, `CanLocalPlayerEditSlot`, `SetLobbyData`, `SendLobbyChatMsg` 경계를 갖는지 소스 검사를 추가한다. Steam SDK를 요구하는 직접 실행 테스트는 이 단계에 넣지 않는다.

- [ ] **Step 2: 경계 테스트 실패 확인**

Run: Unity Test Runner에서 `SteamLobbyPartyUiBoundaryTests`

Expected: 기존 `RefreshMembersIntoPartyRuntime`와 멤버별 덮어쓰기 코드 때문에 FAIL

- [ ] **Step 3: 동기화기 로비 생명주기 구현**

`SteamLobbyPartySynchronizer`는 `#if STEAMWORKS_NET` 내부에서 다음 콜백을 소유한다.

```csharp
Callback<LobbyChatMsg_t>
Callback<LobbyChatUpdate_t>
Callback<LobbyDataUpdate_t>
```

호스트는 `EnterLobby` 시 현재 `PartyRuntimeStore` 캐릭터로 `LobbyPartyAuthorityState.CreateHost`를 만들고 최초 스냅샷을 `SteamMatchmaking.SetLobbyData`의 단일 키 `relic.party.snapshot.v1`에 기록한다.

클라이언트는 `EnterLobby` 시 자체 파티를 발행하지 않고 호스트 스냅샷을 요청·적용한다. 명령 채팅 메시지는 고정 접두사 `RELIC_PARTY_CMD_V1:` 뒤에 직렬화된 JSON을 붙여 구분한다.

- [ ] **Step 4: 호스트 명령 처리와 스냅샷 적용 구현**

```csharp
public bool RequestCharacterChange(int slotIndex, string characterId)
{
    // 로컬 소유권 및 중복 선검사
    // 호스트면 TryChangeCharacter 후 즉시 PublishSnapshot
    // 클라이언트면 SendLobbyChatMsg
    // 성공 전 PartyRuntimeStore 선반영 금지
}
```

`LobbyChatMsg_t` 처리 시 `SteamMatchmaking.GetLobbyChatEntry`가 반환한 실제 발신자 Steam ID를 사용한다. JSON 내부 `RequesterSteamId`와 실제 발신자가 다르면 거부한다.

스냅샷 적용은 호스트 ID, 멤버, 구조, 중복, Revision을 검증한 뒤 세 슬롯을 한 번에 `PartyRuntimeStore`에 반영한다. 기본 배치 그리드는 기존 규칙인 `6 + slotIndex`를 사용하되, 캐릭터 슬롯 교환 시에도 중복 그리드가 생기지 않게 전체 슬롯을 비운 후 순서대로 적용한다.

- [ ] **Step 5: InviteController의 기존 멤버 메타데이터 동기화 교체**

다음을 제거한다.

```text
SyncLocalMemberDataIfNeeded
ResolveLocalMemberSlotIndex
ResolveLocalCharacterId
ResolvePartySlotCharacterId
RefreshMembersIntoPartyRuntime
lastSyncedCharacterIds
nextMemberSyncTime
memberDataSyncInterval
```

`OnLobbyCreated`, `OnLobbyEntered`, `OnLobbyChatUpdated`, `OnLobbyDataUpdated`에서는 동기화기에 현재 로비와 멤버 변경을 전달한다. 상태 패널은 동기화기의 확정 슬롯 소유자와 캐릭터를 읽어 표시한다.

- [ ] **Step 6: 호스트 종료와 로컬 복귀 구현**

클라이언트가 현재 호스트의 퇴장 또는 로비 소유자 변경을 감지하면:

```text
네트워크 파티 비활성화
SteamMatchmaking.LeaveLobby
현재 PartyRuntimeStore 캐릭터 유지
모든 슬롯 로컬 편집 가능 상태
UI 전체 새로고침
```

새 호스트 승격은 수행하지 않는다.

- [ ] **Step 7: 경계 테스트 및 컴파일 확인**

Run: Unity Test Runner에서 `SteamLobbyPartyUiBoundaryTests`

Expected: PASS

Run:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal
```

Expected: `0 Error(s)`

---

### Task 6: 슬롯 UI 권한 차단

**Files:**
- Modify: `Assets/Project/Scripts/UI/Lobby/PartySlotButton.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/PartySlot.cs`
- Test: `Assets/Tests/EditMode~/SteamLobbyPartyUiBoundaryTests.cs`

**Interfaces:**
- Consumes: `SteamLobbyPartySynchronizer.CanLocalPlayerEditSlot(int)`
- Produces: 권한 없는 슬롯이 `CharacterSelectionState`를 변경하지 않는 동작
- Produces: `PartySlot.RefreshOwnershipVisual()`

- [ ] **Step 1: 슬롯 클릭 경계 테스트 작성**

```csharp
[Test]
public void PartySlotButton_ChecksNetworkOwnershipBeforeSelectingSlot()
{
    string source = File.ReadAllText(PartySlotButtonPath);
    Assert.That(source, Does.Contain("CanLocalPlayerEditSlot"));
    Assert.That(source, Does.Contain("return;"));
}
```

- [ ] **Step 2: 테스트 실패 확인**

Run: Unity Test Runner에서 해당 경계 테스트

Expected: 소유권 검사 코드가 없어 FAIL

- [ ] **Step 3: 슬롯 선택 권한 검사 구현**

```csharp
int resolvedSlotIndex = ResolveSlotIndex();
SteamLobbyPartySynchronizer sync = SteamLobbyPartySynchronizer.Instance;

if (sync != null &&
    sync.IsNetworkPartyActive &&
    !sync.CanLocalPlayerEditSlot(resolvedSlotIndex))
{
    return;
}
```

권한을 확인한 뒤에만 클릭 효과음과 `SelectPartySlot`을 실행하여, 잠긴 슬롯 클릭이 정상 선택처럼 들리지 않게 한다.

- [ ] **Step 4: 슬롯 잠금 표시 구현**

`PartySlot`은 확정 상태 갱신 때 `RefreshOwnershipVisual()`을 호출한다. 기존 프리팹 구조를 강제로 변경하지 않도록 `CanvasGroup`이 이미 있으면 `interactable`과 `alpha`를 갱신하고, 없으면 버튼 입력 차단만 적용한다. 새 UI 에셋이나 자동 프리팹 변경은 이번 범위에 포함하지 않는다.

- [ ] **Step 5: 경계 테스트 통과 확인**

Run: Unity Test Runner에서 `SteamLobbyPartyUiBoundaryTests`

Expected: PASS

---

### Task 7: 캐릭터 UI를 명령 경계에 연결

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/CharPick.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/CharBtn.cs`
- Test: `Assets/Tests/EditMode~/SteamLobbyPartyUiBoundaryTests.cs`

**Interfaces:**
- Consumes: `SteamLobbyPartySynchronizer.RequestCharacterChange`
- Consumes: `CanLocalPlayerEditSlot`, `IsCharacterUsedByOtherSlot`
- Produces: 네트워크 로비에서는 로컬 저장소를 선반영하지 않는 캐릭터 선택

- [ ] **Step 1: 두 캐릭터 선택 경로의 경계 테스트 작성**

```csharp
[Test] public void CharPick_UsesPartySynchronizerWhileNetworkPartyIsActive();
[Test] public void CharBtn_DirectPathUsesPartySynchronizerWhileNetworkPartyIsActive();
[Test] public void NetworkSelection_DoesNotClearDuplicateCharacterFromAnotherSlotLocally();
```

소스 검사에서는 네트워크 분기 안에서 `RequestCharacterChange`를 호출하고, 기존의 “중복 슬롯을 로컬에서 ClearSlot” 하는 경로에 진입하지 않는지 확인한다.

- [ ] **Step 2: 테스트 실패 확인**

Run: Unity Test Runner에서 위 세 테스트

Expected: 동기화기 호출이 없어 FAIL

- [ ] **Step 3: `CharPick` 네트워크 선택 구현**

네트워크 파티가 활성화된 경우 전체 `pendingCharacterIds`를 재적용하지 않는다. 현재 `CharacterSelectionState.CurrentPartySlotIndex`가 로컬 소유 슬롯인지 확인하고 다음만 호출한다.

```csharp
sync.RequestCharacterChange(selectedSlotIndex, btn.CharacterId);
```

요청 직후 마커와 `PartyRuntimeStore`를 변경하지 않는다. 호스트 스냅샷 적용 이벤트가 도착하면 `ResetPendingSelectionFromRuntime`, 파티 슬롯, 캐릭터 마커를 새로고침한다.

- [ ] **Step 4: `CharBtn` 직접 선택 경로 구현**

`SaveCharacterToSelectedPartySlot` 시작부에서 네트워크 활성 상태를 검사한다. 활성 상태면 `RequestCharacterChange`만 호출하고 반환한다. 기존 싱글플레이 경로의 `ClearSlot`, `SetCharacter`, 기본 그리드 지정은 그대로 유지한다.

- [ ] **Step 5: 사용 중 캐릭터 선택 차단 표시**

버튼의 기존 잠금 표시 API를 재사용할 수 있는지 확인한다. 다른 슬롯이 사용하는 캐릭터는 파티 선택 동작만 거부하되 캐릭터 정보 열람은 유지한다. 기존 영구 해금 상태인 `IsLocked`를 네트워크 점유 상태로 덮어쓰지 말고 별도 판단 함수로 처리한다.

- [ ] **Step 6: UI 경계 테스트와 컴파일 확인**

Run: Unity Test Runner에서 `SteamLobbyPartyUiBoundaryTests`

Expected: PASS

Run:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal
```

Expected: `0 Error(s)`

---

### Task 8: 통합 검증과 수동 Steam 테스트

**Files:**
- Modify only if verification exposes an in-scope defect
- Test: `Assets/Tests/EditMode~/LobbyPartyAuthorityStateTests.cs`
- Test: `Assets/Tests/EditMode~/LobbyPartySerializationTests.cs`
- Test: `Assets/Tests/EditMode~/SteamLobbyPartyUiBoundaryTests.cs`

**Interfaces:**
- Consumes: Tasks 1~7 전체
- Produces: 컴파일 성공과 1·2·3인 수동 검증 결과

- [ ] **Step 1: 변경 파일 정적 검사**

Run:

```powershell
rg -n "RefreshMembersIntoPartyRuntime|partySlot[0-9]|SetLobbyMemberData.*characterId" Assets\Project\Scripts\Gameplay\Scene\Lobby\SteamLobby
```

Expected: 제거 대상인 이전 전체 덮어쓰기 경로가 검색되지 않음

- [ ] **Step 2: 전체 C# 컴파일**

Run:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal
```

Expected: `0 Error(s)`

- [ ] **Step 3: Unity Test Runner 수동 실행**

Run: Unity 에디터의 Test Runner에서 다음 세 테스트 파일 실행

```text
LobbyPartyAuthorityStateTests
LobbyPartySerializationTests
SteamLobbyPartyUiBoundaryTests
```

Expected: 전체 PASS

- [ ] **Step 4: 2계정 빌드 수동 테스트**

```text
호스트가 A/B/C 선택
클라1 참가
양쪽 모두 [호스트:A, 호스트:B, 클라1:C] 확인
호스트의 3번 슬롯 입력 차단 확인
클라1의 1·2번 슬롯 입력 차단 확인
클라1이 C를 다른 유효 캐릭터로 변경하고 양쪽 동기화 확인
호스트가 클라1 캐릭터를 선택할 수 없는지 확인
클라1 퇴장 후 호스트가 세 슬롯을 다시 편집 가능한지 확인
```

- [ ] **Step 5: 3계정 빌드 수동 테스트**

```text
2인 상태에서 클라2 참가
클라1 캐릭터가 3번에서 2번으로 유지 이동하는지 확인
클라2가 기존 호스트 2번 캐릭터와 함께 3번을 받는지 확인
클라1 퇴장 시 2번만 호스트 소유로 바뀌는지 확인
다시 3인 구성 후 클라2 퇴장 시 클라1 캐릭터가 2번에서 3번으로 유지 이동하는지 확인
두 클라이언트가 동시에 같은 캐릭터를 요청해 하나만 확정되는지 확인
```

- [ ] **Step 6: 호스트 종료 수동 테스트**

```text
클라이언트가 참가한 상태에서 호스트 EXE 종료
클라이언트가 Steam 로비를 종료하는지 확인
마지막 캐릭터 구성은 화면에 유지되는지 확인
세 슬롯 모두 로컬 편집 가능 상태로 돌아오는지 확인
```

- [ ] **Step 7: 변경 범위 검토**

Run:

```powershell
git diff --check
git status --short
```

Expected: 공백 오류 없음, 계획에 명시된 파일 외 예상하지 않은 변경 없음

- [ ] **Step 8: 커밋 승인 요청**

테스트 결과와 변경 파일을 사용자에게 보고한다. 사용자가 명시적으로 승인한 경우에만 스테이징과 커밋을 진행한다.

