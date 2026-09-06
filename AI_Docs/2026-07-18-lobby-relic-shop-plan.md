# 로비 유물 상점 구현 계획

> **실행 작업 규칙:** 각 작업은 테스트 우선으로 진행한다. 테스트는 `Assets/Tests/EditMode~/`에만 작성하고 Unity 에디터가 열린 상태에서는 batchmode 테스트를 실행하지 않는다. 커밋과 PR은 사용자에게 별도 허락을 받기 전까지 수행하지 않는다.

**목표:** 영구 저장되는 BlueDustium과 로비 전체 InventoryPanel을 추가하고, Position 상태에서 서로 다른 액티브 유물 최대 3개를 구매해 배틀 시작 데이터로 전달한다.

**아키텍처:** `LobbyRuntimeData`와 `LobbyRuntimeStore`가 로비 영구 데이터를 소유한다. UI는 구매 Command를 `LobbyRelicPurchaseService`에 전달하고 결과 이벤트만 반영한다. 공용 InventoryPanel은 데이터 공급자 인터페이스를 통해 로비와 배틀 저장소 중 명시적으로 연결된 한쪽만 사용한다.

**기술 스택:** Unity 6, C#, uGUI, TextMeshPro, JsonUtility 기반 기존 SaveSystem, NUnit EditMode 테스트.

## 공통 제약

- BlueDustium은 최초 저장 데이터가 없을 때만 999로 초기화한다.
- BlueDustium은 `BattleRuntimeData`에 복사하지 않는다.
- 유물 가격은 Common 100, Uncommon 200, Rare 300, Unique 500이다.
- 이미 보유한 유물과 같은 진열 내 중복 유물은 후보에서 제외한다.
- 랜덤 결과는 결과 기록 또는 seed 주입이 가능한 인터페이스를 사용한다.
- UI, VFX, 카메라는 구매 결과를 계산하지 않는다.
- 구매 흐름은 Command → 상태 변경 → 결과 이벤트 순서를 따른다.
- 별도 `BagPanel` UI는 만들지 않는다.
- 문서는 `AI_Docs` 밖에 만들지 않는다.

---

### 작업 1: 로비 영구 런타임과 저장 연결

**파일:**

- 생성: `Assets/Project/Scripts/Gameplay/Data/Runtime/LobbyRuntimeData.cs`
- 생성: `Assets/Project/Scripts/Gameplay/Data/Runtime/LobbyRuntimeStore.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Data/DataManager.cs`
- 수정: `Assets/Project/Scripts/Core/SaveSystem.cs`
- 테스트: `Assets/Tests/EditMode~/LobbyRuntimePersistenceTests.cs`

**제공 인터페이스:**

- `LobbyRuntimeData LobbyRuntimeStore.GetOrCreate()`
- `LobbyRuntimeData LobbyRuntimeStore.Get()`
- `void LobbyRuntimeStore.Set(LobbyRuntimeData data)`
- `LobbyRuntimeData DataManager.LobbyRuntimeStore`

- [ ] **1.1 실패 테스트 작성**

다음 동작을 각각 테스트한다.

```csharp
[Test]
public void GetOrCreate_FirstCreation_StartsWith999BlueDustium()
{
    var store = new LobbyRuntimeStore();
    Assert.That(store.GetOrCreate().BlueDustium, Is.EqualTo(999));
}

[TestCase(0)]
[TestCase(321)]
public void Set_ExistingBalance_IsNotReinitialized(int savedBalance)
{
    var store = new LobbyRuntimeStore();
    store.Set(new LobbyRuntimeData { BlueDustium = savedBalance });
    Assert.That(store.GetOrCreate().BlueDustium, Is.EqualTo(savedBalance));
}

[Test]
public void SaveSnapshot_RoundTripsLobbyRuntime()
{
    var lobby = new LobbyRuntimeData
    {
        BlueDustium = 777,
        OwnedRelicIds = new List<string> { "R_Active_1" },
        SkillInventoryIds = new List<string> { "S_Test" }
    };

    GameSaveData save = SaveSystem.CreateSaveDataSnapshotForTests(lobby);
    string json = JsonUtility.ToJson(save);
    GameSaveData restored = JsonUtility.FromJson<GameSaveData>(json);

    Assert.That(restored.Lobby.BlueDustium, Is.EqualTo(777));
    Assert.That(restored.Lobby.OwnedRelicIds, Is.EquivalentTo(lobby.OwnedRelicIds));
    Assert.That(restored.Lobby.SkillInventoryIds, Is.EquivalentTo(lobby.SkillInventoryIds));
}
```

- [ ] **1.2 RED 확인**

Unity Test Runner에서 `LobbyRuntimePersistenceTests`를 실행해 타입 또는 저장 필드 부재로 실패하는지 확인한다. 에디터 자동화가 불가능하면 테스트 소스의 필수 타입 참조와 생성 전 프로덕션 소스 부재를 정적 검사해 RED 증거를 남긴다.

- [ ] **1.3 최소 구현**

`LobbyRuntimeData`는 다음 필드를 소유한다.

```csharp
[Serializable]
public sealed class LobbyRuntimeData
{
    public int BlueDustium = LobbyRuntimeStore.StartingBlueDustium;
    public List<string> OwnedRelicIds = new();
    public List<string> SkillInventoryIds = new();
    public List<string> BagItemIds = new();
    public List<LobbyCharacterLoadoutData> CharacterLoadouts = new();
}

[Serializable]
public sealed class LobbyCharacterLoadoutData
{
    public string CharacterId;
    public string[] EquippedRelicIds = new string[5];
    public string[] EquippedSkillIds = new string[4];
}
```

`LobbyRuntimeStore.GetOrCreate()`에서 인스턴스가 없을 때만 999를 넣는다. `Set()`과 SaveSystem 정규화는 null 목록만 복구하고 저장된 잔액을 덮어쓰지 않는다. `GameSaveData`에 `Lobby` 필드를 추가하고 생성·적용·정규화 흐름 모두에 연결한다.

- [ ] **1.4 GREEN 확인**

테스트를 다시 실행하고 Runtime/Editor C# 프로젝트 빌드가 성공하는지 확인한다.

---

### 작업 2: 유물 가격, 후보 선택, 구매 Command 서비스

**파일:**

- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicPricePolicy.cs`
- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/ILobbyRelicShopRandom.cs`
- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicOfferService.cs`
- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicPurchaseService.cs`
- 테스트: `Assets/Tests/EditMode~/LobbyRelicShopServiceTests.cs`

**제공 인터페이스:**

```csharp
public readonly struct LobbyRelicPurchaseCommand
{
    public LobbyRelicPurchaseCommand(string relicId) => RelicId = relicId;
    public string RelicId { get; }
}

public readonly struct LobbyRelicPurchaseResult
{
    public LobbyRelicPurchaseResult(bool succeeded, string relicId, int price, LobbyRelicPurchaseFailure failure);
    public bool Succeeded { get; }
    public string RelicId { get; }
    public int Price { get; }
    public LobbyRelicPurchaseFailure Failure { get; }
}
```

- [ ] **2.1 가격 및 후보 실패 테스트 작성**

Common/Uncommon/Rare/Unique 가격, 액티브 타입만 포함, 보유 ID 제외, 후보 중복 없음, 후보 부족 시 결과 개수 감소를 검증한다. 테스트 랜덤은 고정 인덱스 배열을 반환하는 가짜 구현으로 결과를 재현한다.

- [ ] **2.2 구매 실패 테스트 작성**

```csharp
[Test]
public void Purchase_ValidOffer_DeductsCurrencyAndAddsOwnedRelic()
{
    LobbyRuntimeData runtime = CreateRuntime(999);
    LobbyRelicPurchaseResult result = service.Execute(
        new LobbyRelicPurchaseCommand("R_Active_Common"), runtime);

    Assert.Multiple(() =>
    {
        Assert.That(result.Succeeded, Is.True);
        Assert.That(runtime.BlueDustium, Is.EqualTo(899));
        Assert.That(runtime.OwnedRelicIds, Does.Contain("R_Active_Common"));
    });
}

[Test]
public void Purchase_InsufficientBalance_DoesNotMutateRuntime()
{
    LobbyRuntimeData runtime = CreateRuntime(99);
    LobbyRelicPurchaseResult result = service.Execute(
        new LobbyRelicPurchaseCommand("R_Active_Common"), runtime);

    Assert.That(result.Failure, Is.EqualTo(LobbyRelicPurchaseFailure.InsufficientBlueDustium));
    Assert.That(runtime.BlueDustium, Is.EqualTo(99));
    Assert.That(runtime.OwnedRelicIds, Is.Empty);
}
```

중복 구매와 알 수 없는 레어도도 상태 불변을 검증한다.

- [ ] **2.3 RED 확인**

서비스 타입 부재와 예상 가격 결과 부재로 테스트가 실패하는지 확인한다.

- [ ] **2.4 최소 구현**

`LobbyRelicPricePolicy.TryGetPrice(string rarity, out int price)`가 `RelicRarityUtility`의 파싱 결과를 고정 가격으로 매핑한다. `LobbyRelicOfferService`는 `RelicDatabase.GetAll()`을 필터한 뒤 주입된 랜덤으로 최대 3개를 비복원 추출한다. 구매 서비스는 한 메서드에서 모든 검증을 끝낸 뒤에만 잔액과 목록을 변경하고 결과를 반환한다.

- [ ] **2.5 GREEN 확인**

서비스 테스트와 C# 빌드를 다시 실행한다.

---

### 작업 3: PositionPanel 유물 버튼과 월드 앵커 추적

**파일:**

- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/WorldAnchorCanvasFollower.cs`
- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicOfferButtonUI.cs`
- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs`
- 생성: `Assets/Project/Prefabs/UI/Lobby/LobbyRelicOfferButton.prefab`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyViewStateController.cs`
- 수정: `Assets/Project/Scenes/YDM/Lobby.unity`
- 테스트: `Assets/Tests/EditMode~/LobbyRelicShopPresenterTests.cs`

**제공 인터페이스:**

- `void LobbyRelicShopPresenter.RefreshOffers()`
- `void LobbyRelicOfferButtonUI.Bind(LobbyRelicOffer offer, Action<string> purchaseRequested)`
- `void LobbyRelicOfferButtonUI.ShowSold()`
- `void LobbyRelicOfferButtonUI.ShowEmpty()`

- [ ] **3.1 상태 및 좌표 변환 실패 테스트 작성**

`LobbyViewStateController`가 Lobby/CharacterSelection에서 `PositionPanel`을 끄고 Position에서만 켜는지 검증한다. 별도 테스트는 알려진 월드 좌표를 Camera와 Canvas 좌표로 변환했을 때 버튼 RectTransform이 예상 로컬 좌표에 놓이는지 검증한다.

- [ ] **3.2 Presenter 실패 테스트 작성**

서로 다른 3개 offer가 3개 버튼에 연결되는지, 구매 성공 후 해당 버튼만 `Sold`가 되는지, 잔액 부족 시 버튼이 판매 완료로 바뀌지 않는지 검증한다.

- [ ] **3.3 RED 확인**

새 UI 타입과 PositionPanel 직렬화 필드 부재로 실패하는지 확인한다.

- [ ] **3.4 최소 구현 및 씬 배치**

Canvas 아래에 `PositionPanel`을 full-stretch RectTransform으로 추가한다. `StartRelicSpawnRoot`의 자식 Transform 3개를 Presenter에 순서대로 연결한다. 각 버튼에 `WorldAnchorCanvasFollower`, Image, TMP 가격, Button을 구성하고 `RelicIconDatabase`에서 아이콘을 조회한다. 카메라 뒤로 이동하거나 화면 밖이면 CanvasGroup으로 숨기고 클릭을 막는다.

`LobbyViewStateController`에는 다음 제어를 추가한다.

```csharp
[SerializeField] private GameObject positionPanel;

// ApplyState
SetActive(positionPanel, isPosition);
```

기존 `positionPanel` 필드가 이미 있으면 새 필드를 만들지 않고 씬 참조만 연결한다.

- [ ] **3.5 GREEN 확인**

정적 씬 검사로 PositionPanel 참조, 버튼 3개, 월드 앵커 3개 연결을 확인하고 C# 빌드를 실행한다.

---

### 작업 4: BlueDustium HUD

**파일:**

- 생성: `Assets/Project/Scripts/UI/LobbyBlueDustiumHudUI.cs`
- 생성: `Assets/Project/Prefabs/UI/Lobby/LobbyBlueDustiumHud.prefab`
- 수정: `Assets/Project/Scenes/YDM/Lobby.unity`
- 테스트: `Assets/Tests/EditMode~/LobbyBlueDustiumHudTests.cs`

**제공 인터페이스:**

- `void LobbyBlueDustiumHudUI.Refresh()`
- `void LobbyBlueDustiumHudUI.SetValueImmediate(int value)`

- [ ] **4.1 실패 테스트 작성**

999가 `999`로 표시되는지, 음수 입력이 `0`으로 표시되는지, 구매 성공 결과 이후 최신 잔액으로 갱신되는지 검증한다.

- [ ] **4.2 RED 확인**

HUD 타입 부재로 실패하는지 확인한다.

- [ ] **4.3 최소 구현 및 씬 배치**

배틀의 `BattleGoldHudUI` 배치와 숫자 갱신 표현을 참고하되 로비 저장소만 읽는 작은 전용 컴포넌트로 만든다. 제공 이미지의 좌측 재화 아이콘/숫자 그룹과 같은 위치 규칙으로 Canvas에 배치한다. 기존 BlueDustium Sprite가 없으면 직렬화된 `currencyIcon`을 비워 두되 레이아웃은 유지하고 경고를 한 번만 기록한다.

- [ ] **4.4 GREEN 확인**

HUD 테스트, 씬 참조 정적 검사, C# 빌드를 실행한다.

---

### 작업 5: InventoryPanel 데이터 공급 경계와 공용 Prefab

**파일:**

- 생성: `Assets/Project/Scripts/Gameplay/Inventory/IInventoryRuntimeContext.cs`
- 생성: `Assets/Project/Scripts/Gameplay/Inventory/BattleInventoryRuntimeContext.cs`
- 생성: `Assets/Project/Scripts/Gameplay/Inventory/LobbyInventoryRuntimeContext.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/SkillInventoryPanelUI.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/RelicEquipPanelUI.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/EquippedRelicSlotUI.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/info/EquippedSkillPanelUI.cs`
- 생성: `Assets/Project/Prefabs/UI/Common/InventoryPanel.prefab`
- 수정: `Assets/Project/Scenes/YDM/Battle.unity`
- 수정: `Assets/Project/Scenes/YDM/Lobby.unity`
- 테스트: `Assets/Tests/EditMode~/InventoryRuntimeContextTests.cs`

**제공 인터페이스:**

```csharp
public interface IInventoryRuntimeContext
{
    IReadOnlyList<string> OwnedRelicIds { get; }
    IReadOnlyList<string> SkillInventoryIds { get; }
    bool IsEditLocked { get; }
    bool TryEquipRelic(string characterId, int slotIndex, string relicId);
    bool TryUnequipRelic(string characterId, int slotIndex);
    bool TryEquipSkill(string characterId, int slotIndex, string skillId);
    void Save();
}
```

- [ ] **5.1 공급자 실패 테스트 작성**

로비 공급자가 `LobbyRuntimeData`만 변경하고 `BattleRuntimeData`를 건드리지 않는지 검증한다. 배틀 공급자는 기존 장착 서비스와 편집 잠금 규칙을 유지하는지 검증한다.

- [ ] **5.2 패널 회귀 실패 테스트 작성**

가짜 `IInventoryRuntimeContext`를 연결한 패널이 보유 유물과 스킬을 표시하고 장착 요청을 해당 공급자에 전달하는지 검증한다.

- [ ] **5.3 RED 확인**

패널이 여전히 `BattleRuntimeStore`를 직접 참조하므로 테스트가 실패하는지 확인한다.

- [ ] **5.4 최소 리팩터링**

패널에서 보유 목록과 장착 변경에 필요한 직접 저장소 접근만 컨텍스트로 이동한다. 툴팁, 아이콘, 선택 애니메이션, 레이아웃 코드는 그대로 둔다. 로비 컨텍스트는 편집 허용, 배틀 컨텍스트는 기존 `lockEditInBattleRoom` 동작을 유지한다.

- [ ] **5.5 공용 Prefab 추출**

Battle씬의 현재 `InventoryPanel` 전체 계층을 `Assets/Project/Prefabs/UI/Common/InventoryPanel.prefab`으로 추출한다. Battle씬 인스턴스에는 `BattleInventoryRuntimeContext`, Lobby씬 인스턴스에는 `LobbyInventoryRuntimeContext`를 연결한다. 두 씬에서 캐릭터 장착, 스킬 인벤토리, 유물 인벤토리 계층 및 외형이 동일한지 Prefab override를 검사한다.

- [ ] **5.6 GREEN 및 회귀 확인**

컨텍스트 테스트와 C# 빌드를 실행한다. Battle씬 직렬화에서 기존 버튼, 닫기 동작, Tooltip 참조가 끊기지 않았는지 정적 검사한다.

---

### 작업 6: 배틀 시작 데이터 전달

**파일:**

- 생성: `Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyBattleRuntimeTransferService.cs`
- 수정: `Assets/Project/Scripts/UI/Lobby/BattlePlayButton.cs`
- 테스트: `Assets/Tests/EditMode~/LobbyBattleRuntimeTransferServiceTests.cs`

**제공 인터페이스:**

```csharp
public sealed class LobbyBattleRuntimeTransferService
{
    public LobbyBattleRuntimeTransferResult Transfer(
        LobbyRuntimeData lobby,
        BattleRuntimeData battle,
        CharacterRuntimeStore characters);
}
```

- [ ] **6.1 실패 테스트 작성**

```csharp
[Test]
public void Transfer_CopiesCombatInventoryWithoutSharingLists()
{
    LobbyRuntimeData lobby = CreateLobbyData();
    BattleRuntimeData battle = new();

    LobbyBattleRuntimeTransferResult result = service.Transfer(lobby, battle, characters);

    Assert.That(result.Succeeded, Is.True);
    Assert.That(battle.OwnedRelicIds, Is.EquivalentTo(lobby.OwnedRelicIds));
    Assert.That(battle.SkillInventoryIds, Is.EquivalentTo(lobby.SkillInventoryIds));
    Assert.That(battle.OwnedRelicIds, Is.Not.SameAs(lobby.OwnedRelicIds));
}
```

BlueDustium이 배틀 데이터에 존재하지 않고 전달 후에도 로비 잔액이 그대로인지, 캐릭터 ID별 장착 상태가 올바르게 복사되는지도 검증한다.

- [ ] **6.2 RED 확인**

전달 서비스 부재로 실패하는지 확인한다.

- [ ] **6.3 최소 구현 및 버튼 연결**

`BattlePlayButton`의 기존 `CommitRuntimeStateContributorsForBattleStart()` 다음, `CaptureLobbyLoadoutSnapshot()` 이전에 전달 서비스를 호출한다. 전달 실패 시 씬 전환을 중단하고 이유를 로그/사용자 경고로 표시한다. 목록은 새 List/배열로 복사하고 저장 객체를 공유하지 않는다.

- [ ] **6.4 GREEN 확인**

전달 서비스 테스트와 `BattlePlayButton` 회귀 빌드를 실행한다.

---

### 작업 7: 통합 검증과 수동 확인

**파일:**

- 수정 없음. 발견된 요청 범위 내 결함만 해당 작업 파일에서 수정한다.

- [ ] **7.1 전체 정적 검증**

다음을 확인한다.

- Lobby씬에 `PositionPanel`, 유물 버튼 3개, InventoryPanel, BlueDustium HUD가 존재한다.
- `StartRelicSpawnRoot`의 자식 3개가 follower에 각각 한 번 연결된다.
- `LobbyViewStateController.positionPanel`이 연결된다.
- 로비 InventoryPanel에는 `LobbyInventoryRuntimeContext`, 배틀 InventoryPanel에는 `BattleInventoryRuntimeContext`가 연결된다.
- Unity YAML fileID 중복과 Missing Script가 없다.

- [ ] **7.2 빌드 검증**

다음을 순서대로 실행한다.

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp.csproj /t:Build /p:RestorePackages=false /v:minimal
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' .\Assembly-CSharp-Editor.csproj /t:Build /p:RestorePackages=false /v:minimal
git diff --check
```

기존 경고와 새 경고를 구분해 보고한다.

- [ ] **7.3 Unity 에디터 수동 확인 안내**

에디터에서 다음 시나리오를 확인한다.

1. 새 저장 생성 시 BlueDustium 999 표시.
2. Position 진입 시 유물 버튼 3개 표시, 카메라 드래그 시 월드 앵커 추적.
3. 유물 구매 시 정확한 가격 차감, 판매 완료 표시, InventoryPanel 유물 목록 갱신.
4. 잔액 부족 시 데이터 불변.
5. InventoryPanel에서 유물과 스킬 장착 가능.
6. 저장 후 로비 재진입 시 잔액과 인벤토리 유지.
7. 배틀 시작 후 Battle InventoryPanel에 로비 보유 및 장착 결과 표시.
8. 배틀 재화 Remnant와 BlueDustium 값이 서로 영향을 주지 않음.

## 완료 조건

- 모든 새 EditMode 테스트가 통과하거나, 에디터가 열린 환경에서 실행할 수 없는 경우 그 사실과 대체 검증 결과가 명시된다.
- Runtime/Editor C# 빌드가 오류 없이 완료된다.
- 로비와 배틀 InventoryPanel 외형 및 기존 배틀 동작 회귀가 없다.
- 멀티플레이 경계에 영향을 주는 전투 핵심 변경은 없으며, 전달 로직은 ID 기반 복사로 제한된다.
