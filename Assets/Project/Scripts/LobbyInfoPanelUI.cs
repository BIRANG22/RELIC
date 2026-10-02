using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lobby Info_Panel의 CharacterSelect와 유물/연성제 선택 장착을 관리합니다.
///
/// 동작:
/// 1. Info_Panel/Relic 또는 Compound 목록의 슬롯을 클릭해 아이템을 선택합니다.
/// 2. CharacterSelect/CharBtn_0~4의 Relic/Back 또는 Compound/Back에 마우스를 올리면
///    장착 가능한 경우 Back이 #3C4476으로 표시됩니다.
/// 3. Back을 클릭하면 해당 CharBtn의 CharacterId를 기준으로 선택 아이템을 바로 장착합니다.
///
/// 기존 드래그 앤 드롭은 LobbyEquipPanelUI가 계속 담당하며 이 스크립트는 클릭 장착만 보강합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyInfoPanelUI : MonoBehaviour
{
    private const string InfoPanelName = "Info_Panel";
    private static readonly Color EquipTargetHoverColor = new Color32(0x3C, 0x44, 0x76, 0xFF);

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Character Select")]
    [Tooltip("Info_Panel/CharacterSelect. 비워두면 이름으로 자동 연결합니다.")]
    [SerializeField] private Transform characterSelectRoot;

    [Header("Inventory")]
    [Tooltip("Info_Panel/Relic. 비워두면 이름으로 자동 연결합니다.")]
    [SerializeField] private Transform relicInventoryRoot;
    [Tooltip("Info_Panel/Compound. 비워두면 이름으로 자동 연결합니다.")]
    [SerializeField] private Transform compoundInventoryRoot;

    private CharPick characterPicker;
    private BattleBagItemSlotUI selectedInventorySlot;
    private string selectedItemId;
    private bool selectedItemIsCompound;

    private readonly List<BattleBagItemSlotUI> relicInventorySlots = new();
    private readonly List<BattleBagItemSlotUI> compoundInventorySlots = new();

    private void Awake()
    {
        ResolveReferences();
        EnsureCharacterSelectActive();
        BindInventorySelectionTargets();
        BindCharacterEquipmentTargets();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureCharacterSelectActive();
        RefreshCharacterData();
    }

    private void Start()
    {
        // LobbyEquipPanelUI가 동적으로 생성/갱신한 슬롯까지 한 번 더 연결합니다.
        BindInventorySelectionTargets();
        BindCharacterEquipmentTargets();
    }

    /// <summary>
    /// 현재 PartyRuntimeStore 상태를 CharacterSelect에 다시 반영하고,
    /// 동적으로 생성된 유물/연성제 슬롯 및 CharBtn 장착 타깃을 다시 연결합니다.
    /// </summary>
    public void RefreshCharacterData()
    {
        ResolveReferences();
        EnsureCharacterSelectActive();
        characterPicker?.RefreshFromPartyRuntime();
        BindInventorySelectionTargets();
        BindCharacterEquipmentTargets();
        ValidateSelectedInventorySlot();
    }

    public static void RefreshAll()
    {
        LobbyInfoPanelUI[] panels = FindObjectsByType<LobbyInfoPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].RefreshCharacterData();
        }
    }

    private void ResolveReferences()
    {
        GameObject root = ResolvePanelRoot();
        if (root == null)
            return;

        Transform rootTransform = root.transform;

        if (characterSelectRoot == null || !characterSelectRoot.IsChildOf(rootTransform))
            characterSelectRoot = rootTransform.Find("CharacterSelect") ?? FindChildRecursive(rootTransform, "CharacterSelect");

        if (relicInventoryRoot == null || !relicInventoryRoot.IsChildOf(rootTransform))
            relicInventoryRoot = rootTransform.Find("Relic");

        if (compoundInventoryRoot == null || !compoundInventoryRoot.IsChildOf(rootTransform))
            compoundInventoryRoot = rootTransform.Find("Compound");

        if (characterSelectRoot != null)
        {
            characterPicker = characterSelectRoot.GetComponent<CharPick>()
                ?? characterSelectRoot.GetComponentInChildren<CharPick>(true);
        }
    }

    private void EnsureCharacterSelectActive()
    {
        if (characterSelectRoot != null && !characterSelectRoot.gameObject.activeSelf)
            characterSelectRoot.gameObject.SetActive(true);
    }

    private void BindInventorySelectionTargets()
    {
        relicInventorySlots.Clear();
        compoundInventorySlots.Clear();

        CollectAndBindInventorySlots(relicInventoryRoot, false, relicInventorySlots);
        CollectAndBindInventorySlots(compoundInventoryRoot, true, compoundInventorySlots);
    }

    private void CollectAndBindInventorySlots(
        Transform inventoryRoot,
        bool isCompound,
        List<BattleBagItemSlotUI> destination)
    {
        if (inventoryRoot == null)
            return;

        Transform content = FindChildRecursive(inventoryRoot, "Content");
        if (content == null)
            return;

        BattleBagItemSlotUI[] slots = content.GetComponentsInChildren<BattleBagItemSlotUI>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            BattleBagItemSlotUI slot = slots[i];
            if (slot == null)
                continue;

            destination.Add(slot);

            LobbyInfoInventorySelectionRelay relay =
                slot.GetComponent<LobbyInfoInventorySelectionRelay>();
            if (relay == null)
                relay = slot.gameObject.AddComponent<LobbyInfoInventorySelectionRelay>();

            relay.Configure(this, slot, isCompound);
        }
    }

    private void BindCharacterEquipmentTargets()
    {
        if (characterSelectRoot == null)
            return;

        CharBtn[] buttons = characterSelectRoot.GetComponentsInChildren<CharBtn>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            CharBtn charButton = buttons[i];
            if (charButton == null)
                continue;

            BindCharacterEquipmentTarget(charButton, "Relic", false);
            BindCharacterEquipmentTarget(charButton, "Compound", true);
        }
    }

    private void BindCharacterEquipmentTarget(CharBtn charButton, string rootName, bool isCompound)
    {
        Transform itemRoot = charButton.transform.Find(rootName)
            ?? FindChildRecursive(charButton.transform, rootName);
        if (itemRoot == null)
            return;

        Transform back = itemRoot.Find("Back") ?? FindChildRecursive(itemRoot, "Back");
        if (back == null)
            return;

        Image backImage = back.GetComponent<Image>();
        if (backImage == null)
            backImage = back.GetComponentInChildren<Image>(true);

        if (backImage != null)
            backImage.raycastTarget = true;

        // Back은 물론, 실제 장착 아이콘이 Raycast Target을 가지고 있어도
        // 같은 장착/해제 입력으로 처리합니다. Icon은 Back의 자식이 아니라
        // 형제 오브젝트이므로 Back에만 이벤트를 붙이면 Icon 클릭이 전달되지 않습니다.
        BindEquipmentPointerRelay(back, charButton, isCompound, backImage);

        Transform icon = itemRoot.Find("Icon") ?? FindChildRecursive(itemRoot, "Icon");
        if (icon != null)
            BindEquipmentPointerRelay(icon, charButton, isCompound, backImage);

        // 계층에 장착 이미지를 Relic/Compound 이름으로 별도 배치한 경우에도
        // 그 이미지가 Raycast를 받으면 동일하게 클릭 해제할 수 있도록 연결합니다.
        Transform itemImage = itemRoot.Find(rootName);
        if (itemImage != null && itemImage != itemRoot)
            BindEquipmentPointerRelay(itemImage, charButton, isCompound, backImage);
    }

    private void BindEquipmentPointerRelay(
        Transform target,
        CharBtn charButton,
        bool isCompound,
        Image backImage)
    {
        if (target == null)
            return;

        Image targetImage = target.GetComponent<Image>();
        if (targetImage != null)
            targetImage.raycastTarget = true;

        LobbyInfoEquipmentTargetRelay relay = target.GetComponent<LobbyInfoEquipmentTargetRelay>();
        if (relay == null)
            relay = target.gameObject.AddComponent<LobbyInfoEquipmentTargetRelay>();

        relay.Configure(this, charButton, isCompound, backImage);
    }

    internal void SelectInventorySlot(BattleBagItemSlotUI slot, bool isCompound)
    {
        if (slot == null || !slot.HasItem || string.IsNullOrWhiteSpace(slot.ItemId))
            return;

        string itemId = slot.ItemId.Trim();
        if (!IsMatchingItemType(itemId, isCompound))
            return;

        if (selectedInventorySlot != null && selectedInventorySlot != slot)
            selectedInventorySlot.SetSelected(false);

        selectedInventorySlot = slot;
        selectedItemId = itemId;
        selectedItemIsCompound = isCompound;
        selectedInventorySlot.SetSelected(true);

        // 반대 타입 목록에서 남아 있던 선택 표시를 제거합니다.
        List<BattleBagItemSlotUI> otherSlots = isCompound ? relicInventorySlots : compoundInventorySlots;
        for (int i = 0; i < otherSlots.Count; i++)
        {
            if (otherSlots[i] != null)
                otherSlots[i].SetSelected(false);
        }
    }

    internal bool CanUseEquipmentTarget(CharBtn charButton, bool isCompound)
    {
        if (!TryGetCharacterRuntime(charButton, out CharacterRuntimeData runtime))
            return false;

        // 인벤토리 아이템이 선택되어 있으면 같은 종류의 장착 대상만 활성화합니다.
        if (!string.IsNullOrWhiteSpace(selectedItemId))
        {
            return selectedItemIsCompound == isCompound &&
                   IsMatchingItemType(selectedItemId, isCompound);
        }

        // 선택 중인 아이템이 없을 때는 현재 장착물이 있는 Back만 활성화합니다.
        // 이 상태에서 클릭하면 해당 장비를 해제합니다.
        return HasEquippedItem(runtime, isCompound);
    }

    internal bool HandleEquipmentTargetClick(CharBtn charButton, bool isCompound)
    {
        if (!TryGetCharacterRuntime(charButton, out CharacterRuntimeData runtime))
            return false;

        string characterId = charButton.CharacterId;
        bool changed;

        if (!string.IsNullOrWhiteSpace(selectedItemId))
        {
            // 아이템을 선택한 상태에서는 장착을 우선합니다.
            if (selectedItemIsCompound != isCompound ||
                !IsMatchingItemType(selectedItemId, isCompound))
            {
                return false;
            }

            string itemId = selectedItemId;
            changed = isCompound
                ? EquipCompound(characterId, itemId)
                : EquipRelic(characterId, itemId);

            if (!changed)
                return false;

            ClearInventorySelection();
        }
        else
        {
            // 아무 아이템도 선택하지 않은 상태에서 이미 장착된 Back을 클릭하면 해제합니다.
            changed = isCompound
                ? UnequipCompound(characterId, runtime)
                : UnequipRelic(characterId, runtime);

            if (!changed)
                return false;
        }

        RefreshAfterEquipmentChange();
        return true;
    }

    // 기존 외부 호출과의 호환을 위해 유지합니다.
    internal bool TryEquipSelectedToCharacter(CharBtn charButton, bool isCompound)
    {
        if (string.IsNullOrWhiteSpace(selectedItemId))
            return false;

        return HandleEquipmentTargetClick(charButton, isCompound);
    }

    private bool TryGetCharacterRuntime(CharBtn charButton, out CharacterRuntimeData runtime)
    {
        runtime = null;

        if (charButton == null || string.IsNullOrWhiteSpace(charButton.CharacterId))
            return false;

        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CharacterRuntimeStore == null)
            return false;

        return dataManager.CharacterRuntimeStore.TryGet(charButton.CharacterId, out runtime);
    }

    private static bool HasEquippedItem(CharacterRuntimeData runtime, bool isCompound)
    {
        if (runtime == null)
            return false;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        int slotIndex = isCompound
            ? ActiveRelicRuntimeUtility.ActiveRelicSlotIndex
            : 1;

        return runtime.EquippedRelicIds != null &&
               slotIndex >= 0 &&
               slotIndex < runtime.EquippedRelicIds.Length &&
               !string.IsNullOrWhiteSpace(runtime.EquippedRelicIds[slotIndex]);
    }

    private void RefreshAfterEquipmentChange()
    {
        LobbyEquipPanelUI.RefreshAllCharacterData();
        RelicEquipPanelUI.RefreshAll();
        RefreshCharacterData();

        if (SaveSystem.Instance != null)
            SaveSystem.Instance.SaveCurrentProgress();
    }

    private bool EquipRelic(string characterId, string relicId)
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CharacterRuntimeStore == null ||
            dataManager.RelicDatabase == null ||
            string.IsNullOrWhiteSpace(characterId) ||
            string.IsNullOrWhiteSpace(relicId))
        {
            return false;
        }

        if (!dataManager.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtime))
            return false;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);

        int targetSlot = FindFirstEmptyRelicSlot(runtime);
        if (targetSlot < 0)
            return false;

        LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
        if (lobby?.OwnedRelicIds == null)
            return false;

        RelicEquipService service = new RelicEquipService(
            dataManager.CharacterRuntimeStore,
            lobby.OwnedRelicIds,
            dataManager.RelicDatabase);

        return service.EquipRelic(characterId, targetSlot, relicId);
    }

    private bool EquipCompound(string characterId, string compoundId)
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CharacterRuntimeStore == null ||
            dataManager.CompoundDatabase == null ||
            string.IsNullOrWhiteSpace(characterId) ||
            string.IsNullOrWhiteSpace(compoundId))
        {
            return false;
        }

        if (!dataManager.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtime))
            return false;

        if (!dataManager.CompoundDatabase.TryGet(compoundId, out CompoundData compound))
            return false;

        LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
        if (lobby?.StoredCompoundIds == null)
            return false;

        int storedIndex = FindStoredItemIndex(lobby.StoredCompoundIds, compoundId);
        if (storedIndex < 0)
            return false;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        int activeSlot = ActiveRelicRuntimeUtility.ActiveRelicSlotIndex;
        string previousCompoundId = runtime.EquippedRelicIds[activeSlot];

        lobby.StoredCompoundIds.RemoveAt(storedIndex);

        if (!string.IsNullOrWhiteSpace(previousCompoundId))
            lobby.StoredCompoundIds.Add(previousCompoundId.Trim());

        runtime.EquippedRelicIds[activeSlot] = compoundId.Trim();
        ActiveRelicRuntimeUtility.ResetUses(runtime, compound);
        return true;
    }

    private bool UnequipRelic(string characterId, CharacterRuntimeData runtime)
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CharacterRuntimeStore == null ||
            dataManager.RelicDatabase == null ||
            runtime == null ||
            string.IsNullOrWhiteSpace(characterId))
        {
            return false;
        }

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        const int runtimeRelicSlotIndex = 1;
        if (runtime.EquippedRelicIds == null ||
            runtimeRelicSlotIndex >= runtime.EquippedRelicIds.Length ||
            string.IsNullOrWhiteSpace(runtime.EquippedRelicIds[runtimeRelicSlotIndex]))
        {
            return false;
        }

        LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
        if (lobby?.OwnedRelicIds == null)
            return false;

        RelicEquipService service = new RelicEquipService(
            dataManager.CharacterRuntimeStore,
            lobby.OwnedRelicIds,
            dataManager.RelicDatabase);

        return service.UnequipRelic(characterId, runtimeRelicSlotIndex);
    }

    private bool UnequipCompound(string characterId, CharacterRuntimeData runtime)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(characterId))
            return false;

        DataManager dataManager = DataManager.Instance;
        LobbyRuntimeData lobby = dataManager?.LobbyRuntimeStore?.GetOrCreate();
        if (lobby == null)
            return false;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        int activeSlot = ActiveRelicRuntimeUtility.ActiveRelicSlotIndex;
        if (runtime.EquippedRelicIds == null ||
            activeSlot < 0 ||
            activeSlot >= runtime.EquippedRelicIds.Length)
        {
            return false;
        }

        string compoundId = runtime.EquippedRelicIds[activeSlot]?.Trim();
        if (string.IsNullOrWhiteSpace(compoundId))
            return false;

        lobby.StoredCompoundIds ??= new List<string>();
        lobby.StoredCompoundIds.Add(compoundId);
        runtime.EquippedRelicIds[activeSlot] = null;
        return true;
    }

    private static int FindFirstEmptyRelicSlot(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return -1;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);

        // 0번은 Compound(Active Relic) 전용 슬롯이므로 일반 유물은 1번부터 찾습니다.
        for (int i = 1; i < runtime.EquippedRelicIds.Length; i++)
        {
            if (BattleErosionEffectService.IsRelicSlotLocked(i))
                continue;

            if (string.IsNullOrWhiteSpace(runtime.EquippedRelicIds[i]))
                return i;
        }

        return -1;
    }

    private static int FindStoredItemIndex(IList<string> ids, string targetId)
    {
        if (ids == null || string.IsNullOrWhiteSpace(targetId))
            return -1;

        string normalized = targetId.Trim();
        for (int i = 0; i < ids.Count; i++)
        {
            if (string.Equals(ids[i]?.Trim(), normalized, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private bool IsMatchingItemType(string itemId, bool isCompound)
    {
        if (string.IsNullOrWhiteSpace(itemId) || DataManager.Instance == null)
            return false;

        string normalized = itemId.Trim();

        if (isCompound)
        {
            return DataManager.Instance.CompoundDatabase != null &&
                   DataManager.Instance.CompoundDatabase.TryGet(normalized, out _);
        }

        return DataManager.Instance.RelicDatabase != null &&
               DataManager.Instance.RelicDatabase.TryGet(normalized, out RelicData relic) &&
               !ActiveRelicEffectResolver.IsActiveRelic(relic);
    }

    private void ValidateSelectedInventorySlot()
    {
        if (selectedInventorySlot == null ||
            !selectedInventorySlot.HasItem ||
            string.IsNullOrWhiteSpace(selectedInventorySlot.ItemId) ||
            !string.Equals(selectedInventorySlot.ItemId.Trim(), selectedItemId, StringComparison.Ordinal))
        {
            ClearInventorySelection();
            return;
        }

        selectedInventorySlot.SetSelected(true);
    }

    private void ClearInventorySelection()
    {
        if (selectedInventorySlot != null)
            selectedInventorySlot.SetSelected(false);

        selectedInventorySlot = null;
        selectedItemId = null;
        selectedItemIsCompound = false;
    }

    private GameObject ResolvePanelRoot()
    {
        if (panelRoot != null && string.Equals(panelRoot.name, InfoPanelName, StringComparison.OrdinalIgnoreCase))
            return panelRoot;

        if (string.Equals(gameObject.name, InfoPanelName, StringComparison.OrdinalIgnoreCase))
        {
            panelRoot = gameObject;
            return panelRoot;
        }

        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, InfoPanelName);
            if (found == null)
                continue;

            panelRoot = found.gameObject;
            return panelRoot;
        }

        return panelRoot;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (string.Equals(root.name, targetName, StringComparison.OrdinalIgnoreCase))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }
}

/// <summary>
/// Info_Panel의 유물/연성제 인벤토리 슬롯 클릭을 LobbyInfoPanelUI에 전달합니다.
/// BattleBagItemSlotUI의 기존 클릭/드래그 로직과 함께 동작합니다.
/// </summary>
public sealed class LobbyInfoInventorySelectionRelay : MonoBehaviour, IPointerClickHandler
{
    private LobbyInfoPanelUI owner;
    private BattleBagItemSlotUI slot;
    private bool isCompound;

    public void Configure(LobbyInfoPanelUI newOwner, BattleBagItemSlotUI newSlot, bool compound)
    {
        owner = newOwner;
        slot = newSlot;
        isCompound = compound;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.SelectInventorySlot(slot, isCompound);
    }
}

/// <summary>
/// CharacterSelect/CharBtn_x의 Relic/Compound Back 및 장착 Icon의 호버/클릭을 처리합니다.
/// </summary>
public sealed class LobbyInfoEquipmentTargetRelay : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    private LobbyInfoPanelUI owner;
    private CharBtn charButton;
    private bool isCompound;
    private Image backImage;
    private Color normalColor = Color.white;
    private bool hasNormalColor;

    public void Configure(
        LobbyInfoPanelUI newOwner,
        CharBtn newCharButton,
        bool compound,
        Image targetBackImage)
    {
        owner = newOwner;
        charButton = newCharButton;
        isCompound = compound;
        backImage = targetBackImage;

        if (backImage != null && !hasNormalColor)
        {
            normalColor = backImage.color;
            hasNormalColor = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (backImage == null || owner == null || !owner.CanUseEquipmentTarget(charButton, isCompound))
            return;

        Color hover = EquipHoverColorWithAlpha(backImage.color.a);
        backImage.color = hover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreColor();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        if (owner == null || !owner.CanUseEquipmentTarget(charButton, isCompound))
            return;

        owner.HandleEquipmentTargetClick(charButton, isCompound);
        RestoreColor();
    }

    private void RestoreColor()
    {
        if (backImage != null && hasNormalColor)
            backImage.color = normalColor;
    }

    private static Color EquipHoverColorWithAlpha(float alpha)
    {
        Color color = new Color32(0x3C, 0x44, 0x76, 0xFF);
        color.a = alpha;
        return color;
    }
}
