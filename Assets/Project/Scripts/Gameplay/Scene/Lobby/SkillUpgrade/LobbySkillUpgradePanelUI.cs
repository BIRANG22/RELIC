using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbySkillUpgradePanelUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SkillUpgradeIconItem iconPrefab;
    [SerializeField] private Image selectedSkillIconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text upgradedNameText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text upgradedEffectText;
    [SerializeField] private TMP_Text priceText;

    [Header("Layout")]
    [SerializeField] private Vector2 fallbackIconSize = new(80f, 80f);
    [SerializeField] private Vector2 iconSpacing = new(15f, 15f);
    [SerializeField] private RectOffset iconPadding = new();
    [SerializeField] private TextAnchor iconAlignment = TextAnchor.UpperLeft;

    private readonly List<SkillUpgradeIconItem> spawnedItems = new();
    private readonly LobbySkillUpgradeSelection selection = new();
    private Sprite defaultSelectedSprite;
    private bool defaultSelectedEnabled;
    private Color defaultSelectedColor = Color.white;
    private bool cachedSelectedDefaults;

    private void Awake()
    {
        ConfigureContentLayout();
        BindPanelButtons();
    }

    private void OnEnable()
    {
        ConfigureContentLayout();
        BindPanelButtons();
    }

    public void Open()
    {
        (panelRoot != null ? panelRoot : gameObject).SetActive(true);
        GetComponent<SkillUpgradePanelContextSelector>()?.RefreshContext();
        ClearSelection();
        Refresh();
    }

    public void ActivateForContext()
    {
        enabled = true;
        BindPanelButtons();
    }

    public void Close()
    {
        ClearItems();
        ClearSelection();
        (panelRoot != null ? panelRoot : gameObject).SetActive(false);
    }

    public void TuneSelectedSkill()
    {
        Debug.Log($"[LobbySkillUpgradePanelUI] 강화 버튼 입력. 선택 여부: {selection.HasSelection}");

        if (!CanLocalPlayerMutateHostOnlyState())
        {
            BattleWarningUI.ShowMessage("Only the host can upgrade in multiplayer lobby.");
            return;
        }

        if (!selection.HasSelection)
        {
            ShowFailure(LobbySkillUpgradeFailure.InvalidRequest);
            return;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogError("[LobbySkillUpgradePanelUI] DataManager.Instance가 없어 강화를 실행할 수 없습니다.");
            return;
        }

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore?.GetOrCreate();
        var service = new LobbySkillUpgradeService(DataManager.Instance.CharacterRuntimeStore);
        LobbySkillUpgradeResult result = selection.Execute(lobby, service);

        if (!result.Succeeded)
        {
            Debug.LogWarning($"[LobbySkillUpgradePanelUI] 강화 실패: {result.Failure}, 가격: {result.Price}");
            ShowFailure(result.Failure);
            RefreshPrice();
            return;
        }

        Debug.Log($"[LobbySkillUpgradePanelUI] 강화 성공. 소모: {result.Price}, 잔액: {lobby.BlueDustium}");
        LobbyBlueDustiumHudUI.RefreshAll();
        EquippedSkillPanelUI.RefreshAll();
        SkillInventoryPanelUI.RefreshAll();
        ClearSelection();
        Refresh();
        PublishHostSnapshotAfterLocalMutation();
    }

    private void Refresh()
    {
        ClearItems();
        ClearInfo();
        ConfigureContentLayout();
        RefreshPrice();

        DataManager manager = DataManager.Instance;
        if (manager == null)
            return;

        PartyRuntimeStore party = manager.PartyRuntimeStore;
        if (party != null)
        {
            for (int i = 0; i < party.MaxPartyCountValue; i++)
            {
                string characterId = party.GetCharacterId(i);
                if (!string.IsNullOrWhiteSpace(characterId) &&
                    manager.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData character))
                {
                    SpawnCharacterSkills(character);
                }
            }
        }

        LobbyRuntimeData lobby = manager.LobbyRuntimeStore?.GetOrCreate();
        if (lobby?.SkillInventoryIds != null)
        {
            for (int i = 0; i < lobby.SkillInventoryIds.Count; i++)
                SpawnItem(null, lobby.SkillInventoryIds[i], SkillSlotType.Inventory, i);
        }

        if (contentRoot is RectTransform rect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void SpawnCharacterSkills(CharacterRuntimeData character)
    {
        if (character == null)
            return;

        SpawnItem(character.CharacterId, character.PassiveSkillId, SkillSlotType.Passive, -1);
        SpawnItem(character.CharacterId, character.UniqueSkillId, SkillSlotType.Unique, -1);
        SpawnItem(character.CharacterId, character.AbilitySkillId, SkillSlotType.Ability, -1);

        if (character.EquippedSkillIds == null)
            return;

        for (int i = 2; i < character.EquippedSkillIds.Length; i++)
            SpawnItem(character.CharacterId, character.EquippedSkillIds[i], SkillSlotType.Equipped, i);
    }

    private void SpawnItem(string characterId, string skillId, SkillSlotType slotType, int slotIndex)
    {
        if (iconPrefab == null || contentRoot == null || !TryGetUpgradeId(skillId, out string upgradeId))
            return;

        SkillUpgradeIconItem item = Instantiate(iconPrefab, contentRoot);
        PrepareItemLayout(item);
        item.Initialize(characterId, skillId, upgradeId, slotType, slotIndex,
            OnItemClicked, ShowSkillInfo, _ => { });
        spawnedItems.Add(item);
    }

    private bool TryGetUpgradeId(string skillId, out string upgradeId)
    {
        upgradeId = null;
        DataManager manager = DataManager.Instance;
        if (manager?.SkillDatabase == null || string.IsNullOrWhiteSpace(skillId) ||
            SkillRarityUtility.IsUpgradeSkillVariant(skillId) ||
            !manager.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData skill) ||
            !SkillRarityUtility.CanUpgrade(skill) ||
            !SkillRarityUtility.TryGetPairedVariantId(skillId, out upgradeId))
        {
            return false;
        }

        return manager.SkillDatabase.TryGet(upgradeId, out _);
    }

    private void OnItemClicked(SkillUpgradeRequest request, Sprite icon)
    {
        selection.Select(request);
        Debug.Log($"[LobbySkillUpgradePanelUI] 스킬 클릭 선택: {request.CurrentSkillId} -> {request.UpgradeSkillId}");
        CacheSelectedDefaults();
        Image selectedImage = ResolveSelectedSkillIconImage();
        if (selectedImage != null)
        {
            selectedImage.sprite = icon;
            selectedImage.enabled = icon != null;
            selectedImage.preserveAspect = true;
        }
        ShowSkillInfo(request);
    }

    private void ShowSkillInfo(SkillUpgradeRequest request)
    {
        // 이 패널은 호버 시 상세 정보가 표시되지 않으므로 미리보기 요청과
        // 실제 강화 요청을 동일하게 유지한다.
        selection.Select(request);

        DataManager manager = DataManager.Instance;
        if (manager?.SkillDatabase == null ||
            !manager.SkillDatabase.TryGet(request.CurrentSkillId, out SkillMasterData current) ||
            !manager.SkillDatabase.TryGet(request.UpgradeSkillId, out SkillMasterData upgraded))
            return;

        CharacterRuntimeData character = null;
        if (!string.IsNullOrWhiteSpace(request.CharacterId))
            manager.CharacterRuntimeStore.TryGet(request.CharacterId, out character);

        SetText(nameText, string.IsNullOrWhiteSpace(current.Name) ? current.SkillId : GameDataLocalization.SkillName(current));
        SetText(upgradedNameText, string.IsNullOrWhiteSpace(upgraded.Name) ? upgraded.SkillId : GameDataLocalization.SkillName(upgraded));
        SetText(effectText, SkillTooltipFormatter.BuildSkillDescription(current, character));
        SetText(upgradedEffectText, SkillTooltipFormatter.BuildSkillDescription(upgraded, character));
    }

    private void RefreshPrice()
    {
        int count = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate()?.LobbySkillUpgradeCount ?? 0;
        SetText(priceText, $"{LobbySkillUpgradePricePolicy.GetPrice(count)}");
    }

    private static void ShowFailure(LobbySkillUpgradeFailure failure)
    {
        string message = failure == LobbySkillUpgradeFailure.InsufficientBlueDustium
            ? "BlueDustium이 부족합니다."
            : "스킬을 강화할 수 없습니다.";
        BattleWarningUI.ShowMessage(message);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target == null)
            return;
        target.gameObject.SetActive(true);
        target.text = value ?? string.Empty;
    }

    private void ClearInfo()
    {
        SetText(nameText, string.Empty);
        SetText(upgradedNameText, string.Empty);
        SetText(effectText, string.Empty);
        SetText(upgradedEffectText, string.Empty);
    }

    private void ClearSelection()
    {
        selection.Clear();
        CacheSelectedDefaults();
        Image selectedImage = ResolveSelectedSkillIconImage();
        if (selectedImage == null)
            return;
        selectedImage.sprite = defaultSelectedSprite;
        selectedImage.enabled = defaultSelectedEnabled;
        selectedImage.color = defaultSelectedColor;
    }

    private void CacheSelectedDefaults()
    {
        Image selectedImage = ResolveSelectedSkillIconImage();
        if (cachedSelectedDefaults || selectedImage == null)
            return;
        defaultSelectedSprite = selectedImage.sprite;
        defaultSelectedEnabled = selectedImage.enabled;
        defaultSelectedColor = selectedImage.color;
        cachedSelectedDefaults = true;
    }

    private Image ResolveSelectedSkillIconImage()
    {
        if (selectedSkillIconImage != null)
            return selectedSkillIconImage;

        Transform root = panelRoot != null ? panelRoot.transform : transform;
        Transform wheel = FindDescendant(root, "Wheel");
        Transform image = wheel != null ? FindDescendant(wheel, "Image") : null;
        selectedSkillIconImage = image != null ? image.GetComponent<Image>() : null;
        return selectedSkillIconImage;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (string.Equals(child.name, objectName, StringComparison.OrdinalIgnoreCase))
                return child;
            Transform nested = FindDescendant(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void ClearItems()
    {
        foreach (SkillUpgradeIconItem item in spawnedItems)
            if (item != null)
                Destroy(item.gameObject);
        spawnedItems.Clear();
    }

    private void ConfigureContentLayout()
    {
        if (contentRoot == null)
            return;
        iconPadding ??= new RectOffset();
        Vector2 size = ResolveIconSize();
        GridLayoutGroup grid = contentRoot.GetComponent<GridLayoutGroup>() ?? contentRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.childAlignment = iconAlignment;
        grid.cellSize = size;
        grid.spacing = iconSpacing;
        grid.padding = iconPadding;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        float width = contentRoot is RectTransform rect ? Mathf.Max(rect.rect.width, rect.sizeDelta.x) : size.x;
        grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt((width + iconSpacing.x) / (size.x + iconSpacing.x)));
    }

    private Vector2 ResolveIconSize()
    {
        RectTransform rect = iconPrefab != null ? iconPrefab.GetComponent<RectTransform>() : null;
        Vector2 size = rect != null && rect.sizeDelta.x > 0f && rect.sizeDelta.y > 0f ? rect.sizeDelta : fallbackIconSize;
        return new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
    }

    private void PrepareItemLayout(SkillUpgradeIconItem item)
    {
        RectTransform rect = item != null ? item.GetComponent<RectTransform>() : null;
        if (rect != null)
        {
            rect.sizeDelta = ResolveIconSize();
            rect.localScale = Vector3.one;
        }
    }

    private static bool CanLocalPlayerMutateHostOnlyState()
    {
        SteamLobbySharedStateSynchronizer synchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        return synchronizer == null ||
               synchronizer.CanLocalPlayerMutateHostOnlyState();
    }

    private static void PublishHostSnapshotAfterLocalMutation()
    {
        SteamLobbySharedStateSynchronizer.Instance
            ?.PublishHostSnapshotAfterLocalMutation();
    }

    private void BindPanelButtons()
    {
        Transform tuningTransform = FindDescendant(transform, "TuningButton");
        Transform fallbackUpgradeTransform = FindDescendant(transform, "UpgradeButton");
        Transform upgradeTransform = tuningTransform != null ? tuningTransform : fallbackUpgradeTransform;
        Button upgradeButton = upgradeTransform != null ? upgradeTransform.GetComponent<Button>() : null;
        if (upgradeButton != null)
        {
            upgradeButton.onClick = new Button.ButtonClickedEvent();
            upgradeButton.onClick.AddListener(TuneSelectedSkill);
        }

        if (tuningTransform != null && fallbackUpgradeTransform != null)
            fallbackUpgradeTransform.gameObject.SetActive(false);

        Transform closeTransform = FindDescendant(transform, "Cancel") ?? FindDescendant(transform, "CloseButton");
        Button closeButton = closeTransform != null ? closeTransform.GetComponent<Button>() : null;
        if (closeButton != null)
        {
            closeButton.onClick = new Button.ButtonClickedEvent();
            closeButton.onClick.AddListener(Close);
        }
    }
}
