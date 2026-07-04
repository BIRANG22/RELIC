using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Relic.Gameplay.Data;

public enum SkillSlotType
{
    Passive,
    Unique,
    Ability,
    Equipped,
    Inventory
}

public struct SkillUpgradeRequest
{
    public string CharacterId;
    public string CurrentSkillId;
    public string UpgradeSkillId;
    public SkillSlotType SlotType;
    public int SlotIndex;
}
public class SkillUpgradePanel : MonoBehaviour
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

    [Header("Upgrade Complete Animation")]
    [SerializeField] private Transform gearTransform;
    [SerializeField] private float gearRotateAnglePerStep = -60f;
    [SerializeField] private int gearRotateStepCount = 2;
    [SerializeField] private float gearRotateTickDuration = 0.12f;
    [SerializeField] private float gearRotateTickInterval = 1f;
    [SerializeField] private float closeDelayAfterUpgradeComplete = 0.5f;
    [SerializeField] private string upgradeCompleteMessageFormat = "{0}으로 강화되었습니다.";
    [SerializeField] private Color upgradedSkillIconColor = new Color32(0x7E, 0x93, 0xEC, 0xFF);

    [Header("Layout")]
    [SerializeField] private Vector2 fallbackIconSize = new(80f, 80f);
    [SerializeField] private Vector2 iconSpacing = new(15f, 15f);
    [SerializeField] private RectOffset iconPadding = new();
    [SerializeField] private TextAnchor iconAlignment = TextAnchor.UpperLeft;

    private readonly List<SkillUpgradeIconItem> spawnedItems = new();
    private bool hasUpgradedThisRestRoom;
    private bool hasSelectedUpgradeRequest;
    private SkillUpgradeRequest selectedUpgradeRequest;
    private bool hasCachedSelectedIconDefault;
    private Sprite selectedIconDefaultSprite;
    private bool selectedIconDefaultEnabled;
    private Color selectedIconDefaultColor = Color.white;
    private Coroutine upgradeCompleteCoroutine;

    public bool HasUpgradedThisRestRoom => hasUpgradedThisRestRoom;

    private void Awake()
    {
        ConfigureContentLayout();
    }

    private void OnEnable()
    {
        ConfigureContentLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled)
            ConfigureContentLayout();
    }

    public void ResetRestRoomUpgradeLimit()
    {
        hasUpgradedThisRestRoom = false;
        ClearSelectedUpgradeSelection();
    }

    public void Open()
    {
        if (hasUpgradedThisRestRoom)
        {
            Close();
            return;
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        ClearSelectedUpgradeSelection();
        ClearSkillInfoTexts();
        ConfigureContentLayout();
        Refresh();
    }

    public void Close()
    {
        Clear();
        ClearSelectedUpgradeSelection();
        ClearSkillInfoTexts();

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public bool TuneSelectedSkill()
    {
        if (hasUpgradedThisRestRoom || !hasSelectedUpgradeRequest)
            return false;

        return ApplySkillUpgrade(selectedUpgradeRequest);
    }

    private void Refresh()
    {
        Clear();
        ConfigureContentLayout();

        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore == null)
            return;

        for (int partyIndex = 0; partyIndex < partyStore.MaxPartyCountValue; partyIndex++)
        {
            string characterId = partyStore.GetCharacterId(partyIndex);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                    characterId,
                    out CharacterRuntimeData characterRuntime))
            {
                continue;
            }

            SpawnCharacterSkillItems(characterRuntime);
        }

        SpawnInventorySkillItems();
        RebuildContentLayout();
    }

    private void SpawnInventorySkillItems()
    {
        if (DataManager.Instance == null)
            return;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        if (runtime.SkillInventoryIds == null)
            return;

        for (int i = 0; i < runtime.SkillInventoryIds.Count; i++)
            SpawnInventorySkillItem(runtime.SkillInventoryIds[i], i);
    }

    private void SpawnInventorySkillItem(string skillId, int inventoryIndex)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (iconPrefab == null || contentRoot == null)
            return;

        if (!TryGetSkillUpgradeId(skillId, out string upgradeSkillId))
            return;

        SkillUpgradeIconItem item = Instantiate(iconPrefab, contentRoot);
        PrepareSpawnedItemLayout(item);
        item.Initialize(
            null,
            skillId,
            upgradeSkillId,
            SkillSlotType.Inventory,
            inventoryIndex,
            OnSkillItemClicked,
            ShowUpgradeSkillInfo,
            OnSkillItemHoverExit
        );

        spawnedItems.Add(item);
    }

    private void SpawnCharacterSkillItems(CharacterRuntimeData characterRuntime)
    {
        if (characterRuntime == null)
            return;

        SpawnSkillItem(characterRuntime, characterRuntime.PassiveSkillId, SkillSlotType.Passive, -1);
        SpawnSkillItem(characterRuntime, characterRuntime.UniqueSkillId, SkillSlotType.Unique, -1);
        SpawnSkillItem(characterRuntime, characterRuntime.AbilitySkillId, SkillSlotType.Ability, -1);

        if (characterRuntime.EquippedSkillIds == null)
            return;

        for (int i = 2; i < characterRuntime.EquippedSkillIds.Length; i++)
        {
            SpawnSkillItem(characterRuntime, characterRuntime.EquippedSkillIds[i], SkillSlotType.Equipped, i);
        }
    }

    private void SpawnSkillItem(
        CharacterRuntimeData characterRuntime,
        string skillId,
        SkillSlotType slotType,
        int slotIndex)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (iconPrefab == null || contentRoot == null)
            return;

        if (!DataManager.Instance.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData currentSkill) ||
            !SkillRarityUtility.CanUpgrade(currentSkill))
        {
            return;
        }

        if (!TryGetSkillUpgradeId(skillId, out string upgradeSkillId))
            return;

        SkillUpgradeIconItem item = Instantiate(iconPrefab, contentRoot);
        PrepareSpawnedItemLayout(item);
        item.Initialize(
            characterRuntime.CharacterId,
            skillId,
            upgradeSkillId,
            slotType,
            slotIndex,
            OnSkillItemClicked,
            ShowUpgradeSkillInfo,
            OnSkillItemHoverExit
        );

        spawnedItems.Add(item);
    }

    private void OnSkillItemClicked(SkillUpgradeRequest request, Sprite selectedIcon)
    {
        if (hasUpgradedThisRestRoom)
            return;

        SelectSkillForUpgrade(request, selectedIcon);
        ShowUpgradeSkillInfo(request);
    }

    private void SelectSkillForUpgrade(SkillUpgradeRequest request, Sprite selectedIcon)
    {
        selectedUpgradeRequest = request;
        hasSelectedUpgradeRequest = true;
        SetSelectedSkillIcon(selectedIcon != null ? selectedIcon : ResolveSkillIcon(request.CurrentSkillId));
    }

    private bool ApplySkillUpgrade(SkillUpgradeRequest request)
    {
        if (DataManager.Instance == null)
            return false;

        if (hasUpgradedThisRestRoom)
            return false;

        if (request.SlotType == SkillSlotType.Inventory)
        {
            return UpgradeInventorySkill(request);
        }

        if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                request.CharacterId,
                out CharacterRuntimeData characterRuntime))
        {
            return false;
        }

        switch (request.SlotType)
        {
            case SkillSlotType.Passive:
                characterRuntime.PassiveSkillId = request.UpgradeSkillId;
                break;

            case SkillSlotType.Unique:
                characterRuntime.UniqueSkillId = request.UpgradeSkillId;
                break;

            case SkillSlotType.Ability:
                characterRuntime.AbilitySkillId = request.UpgradeSkillId;
                break;

            case SkillSlotType.Equipped:
                if (characterRuntime.EquippedSkillIds == null)
                    return false;

                if (request.SlotIndex < 0 || request.SlotIndex >= characterRuntime.EquippedSkillIds.Length)
                    return false;

                characterRuntime.EquippedSkillIds[request.SlotIndex] = request.UpgradeSkillId;
                break;

            default:
                return false;
        }

        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(characterRuntime);
        EquippedSkillPanelUI.RefreshAll();
        SkillInventoryPanelUI.RefreshAll();

        Debug.Log(
            $"[SkillUpgradePanel] Skill upgraded / Character:{request.CharacterId} / " +
            $"{request.CurrentSkillId} -> {request.UpgradeSkillId}"
        );

        CompleteUpgrade();
        return true;
    }

    private bool UpgradeInventorySkill(SkillUpgradeRequest request)
    {
        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        if (runtime.SkillInventoryIds == null)
            return false;

        if (request.SlotIndex < 0 || request.SlotIndex >= runtime.SkillInventoryIds.Count)
            return false;

        runtime.SkillInventoryIds[request.SlotIndex] = request.UpgradeSkillId;
        DataManager.Instance.BattleRuntimeStore.Set(runtime);
        SkillInventoryPanelUI.RefreshAll();

        Debug.Log(
            $"[SkillUpgradePanel] Inventory skill upgraded / " +
            $"{request.CurrentSkillId} -> {request.UpgradeSkillId}"
        );

        CompleteUpgrade();
        return true;
    }

    private void CompleteUpgrade()
    {
        hasUpgradedThisRestRoom = true;

        if (!isActiveAndEnabled)
        {
            Close();
            return;
        }

        if (upgradeCompleteCoroutine != null)
            StopCoroutine(upgradeCompleteCoroutine);

        upgradeCompleteCoroutine = StartCoroutine(PlayUpgradeCompleteSequence());
    }

    private IEnumerator PlayUpgradeCompleteSequence()
    {
        string upgradedSkillName = BuildUpgradeCompleteSkillName(selectedUpgradeRequest);

        yield return PlayGearRotateAnimation();

        TintUpgradedSkillItem(selectedUpgradeRequest);
        TintSelectedWheelIconAsUpgraded();

        float safeCloseDelay = Mathf.Max(0f, closeDelayAfterUpgradeComplete);
        if (safeCloseDelay > 0f)
            yield return new WaitForSecondsRealtime(safeCloseDelay);

        upgradeCompleteCoroutine = null;
        Close();
        ShowUpgradeCompleteWarning(upgradedSkillName);
    }

    private IEnumerator PlayGearRotateAnimation()
    {
        Transform gear = ResolveGearTransform();

        if (gear == null)
            yield break;

        int safeCount = Mathf.Max(0, gearRotateStepCount);
        float safeInterval = Mathf.Max(0f, gearRotateTickInterval);

        for (int i = 0; i < safeCount; i++)
        {
            yield return RotateGearOneTick(gear, gearRotateAnglePerStep);

            if (i < safeCount - 1 && safeInterval > 0f)
                yield return new WaitForSecondsRealtime(safeInterval);
        }
    }

    private IEnumerator RotateGearOneTick(Transform gear, float deltaZ)
    {
        if (gear == null)
            yield break;

        Vector3 startEuler = gear.localEulerAngles;
        float startZ = NormalizeAngle(startEuler.z);
        float targetZ = startZ + deltaZ;
        float safeDuration = Mathf.Max(0.01f, gearRotateTickDuration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float z = Mathf.Lerp(startZ, targetZ, t);
            gear.localRotation = Quaternion.Euler(startEuler.x, startEuler.y, z);
            yield return null;
        }

        gear.localRotation = Quaternion.Euler(startEuler.x, startEuler.y, targetZ);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle < -180f)
            angle += 360f;

        return angle;
    }

    private void TintUpgradedSkillItem(SkillUpgradeRequest request)
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            SkillUpgradeIconItem item = spawnedItems[i];

            if (item != null && item.Matches(request))
            {
                item.SetIconColor(upgradedSkillIconColor);
                return;
            }
        }
    }

    private void TintSelectedWheelIconAsUpgraded()
    {
        Image targetImage = ResolveSelectedSkillIconImage();

        if (targetImage == null)
            return;

        CacheSelectedSkillIconDefault(targetImage);
        targetImage.color = upgradedSkillIconColor;
    }

    private Transform ResolveGearTransform()
    {
        if (gearTransform != null)
            return gearTransform;

        Transform root = panelRoot != null ? panelRoot.transform : transform;
        Transform found = FindChildByName(root, "Gear");

        if (found != null)
            gearTransform = found;

        return gearTransform;
    }


    private void ShowUpgradeSkillInfo(SkillUpgradeRequest request)
    {
        if (!TryBuildUpgradeSkillInfo(
                request,
                out string currentName,
                out string upgradedName,
                out string currentEffect,
                out string upgradeEffect))
        {
            return;
        }

        TMP_Text resolvedNameText = ResolveNameText();
        TMP_Text resolvedUpgradedNameText = ResolveUpgradedNameText();
        TMP_Text resolvedEffectText = ResolveEffectText();
        TMP_Text resolvedUpgradedEffectText = ResolveUpgradedEffectText();

        if (resolvedNameText != null)
        {
            resolvedNameText.gameObject.SetActive(true);
            resolvedNameText.text = currentName;
        }

        if (resolvedUpgradedNameText != null)
        {
            resolvedUpgradedNameText.gameObject.SetActive(true);
            resolvedUpgradedNameText.text = upgradedName;
        }

        if (resolvedEffectText != null)
        {
            resolvedEffectText.gameObject.SetActive(true);
            resolvedEffectText.text = currentEffect;
        }

        if (resolvedUpgradedEffectText != null)
        {
            resolvedUpgradedEffectText.gameObject.SetActive(true);
            resolvedUpgradedEffectText.text = upgradeEffect;
        }
    }

    private void OnSkillItemHoverExit(SkillUpgradeRequest request)
    {
        // 강화 패널에서는 팝업 프리팹을 닫는 대신, 마지막으로 본 스킬 정보를 텍스트 영역에 유지합니다.
    }

    private bool TryBuildUpgradeSkillInfo(
        SkillUpgradeRequest request,
        out string currentName,
        out string upgradedName,
        out string currentEffect,
        out string upgradeEffect)
    {
        currentName = string.Empty;
        upgradedName = string.Empty;
        currentEffect = string.Empty;
        upgradeEffect = string.Empty;

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
            return false;

        if (!DataManager.Instance.SkillDatabase.TryGet(request.CurrentSkillId, out SkillMasterData currentSkill))
            return false;

        if (!DataManager.Instance.SkillDatabase.TryGet(request.UpgradeSkillId, out SkillMasterData upgradeSkill))
            return false;

        CharacterRuntimeData runtime = ResolveCharacterRuntime(request.CharacterId);
        currentName = GetSkillName(currentSkill);
        upgradedName = GetSkillName(upgradeSkill);
        currentEffect = SkillTooltipFormatter.BuildSkillDescription(currentSkill, runtime);
        upgradeEffect = SkillTooltipFormatter.BuildSkillDescription(upgradeSkill, runtime);

        return true;
    }

    private CharacterRuntimeData ResolveCharacterRuntime(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        if (DataManager.Instance == null || DataManager.Instance.CharacterRuntimeStore == null)
            return null;

        return DataManager.Instance.CharacterRuntimeStore.TryGet(
            characterId,
            out CharacterRuntimeData runtime)
            ? runtime
            : null;
    }

    private string GetSkillName(SkillMasterData skillData)
    {
        if (skillData == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(skillData.Name))
            return skillData.Name;

        return skillData.SkillId;
    }

    private string BuildUpgradeCompleteSkillName(SkillUpgradeRequest request)
    {
        if (DataManager.Instance != null &&
            DataManager.Instance.SkillDatabase != null &&
            DataManager.Instance.SkillDatabase.TryGet(request.UpgradeSkillId, out SkillMasterData upgradedSkill))
        {
            string upgradedName = GetSkillName(upgradedSkill);
            if (!string.IsNullOrWhiteSpace(upgradedName))
                return upgradedName;
        }

        if (!string.IsNullOrWhiteSpace(request.UpgradeSkillId))
            return request.UpgradeSkillId;

        return "스킬";
    }

    private void ShowUpgradeCompleteWarning(string upgradedSkillName)
    {
        string safeName = string.IsNullOrWhiteSpace(upgradedSkillName) ? "스킬" : upgradedSkillName;
        string format = string.IsNullOrWhiteSpace(upgradeCompleteMessageFormat)
            ? "{0}으로 강화되었습니다."
            : upgradeCompleteMessageFormat;

        BattleWarningUI.ShowMessage(string.Format(format, safeName));
    }

    private void ClearSkillInfoTexts()
    {
        TMP_Text resolvedNameText = ResolveNameText();
        TMP_Text resolvedUpgradedNameText = ResolveUpgradedNameText();
        TMP_Text resolvedEffectText = ResolveEffectText();
        TMP_Text resolvedUpgradedEffectText = ResolveUpgradedEffectText();

        if (resolvedNameText != null)
        {
            resolvedNameText.text = string.Empty;
            resolvedNameText.gameObject.SetActive(false);
        }

        if (resolvedUpgradedNameText != null)
        {
            resolvedUpgradedNameText.text = string.Empty;
            resolvedUpgradedNameText.gameObject.SetActive(false);
        }

        if (resolvedEffectText != null)
        {
            resolvedEffectText.text = string.Empty;
            resolvedEffectText.gameObject.SetActive(false);
        }

        if (resolvedUpgradedEffectText != null)
        {
            resolvedUpgradedEffectText.text = string.Empty;
            resolvedUpgradedEffectText.gameObject.SetActive(false);
        }
    }

    private TMP_Text ResolveNameText()
    {
        if (nameText != null)
            return nameText;

        Transform root = panelRoot != null ? panelRoot.transform : transform;
        Transform found = FindChildByName(root, "NameText") ??
                          FindChildByName(root, "CurrentNameText");

        if (found != null)
            nameText = found.GetComponent<TMP_Text>();

        return nameText;
    }

    private TMP_Text ResolveUpgradedNameText()
    {
        if (upgradedNameText != null)
            return upgradedNameText;

        Transform root = panelRoot != null ? panelRoot.transform : transform;
        Transform found = FindChildByName(root, "UpgradedNameText") ??
                          FindChildByName(root, "UpgradeNameText") ??
                          FindChildByName(root, "AfterNameText");

        if (found != null)
            upgradedNameText = found.GetComponent<TMP_Text>();

        return upgradedNameText;
    }

    private TMP_Text ResolveEffectText()
    {
        if (effectText != null)
            return effectText;

        Transform root = panelRoot != null ? panelRoot.transform : transform;
        Transform found = FindChildByName(root, "EffectText") ??
                          FindChildByName(root, "CurrentEffectText");

        if (found != null)
            effectText = found.GetComponent<TMP_Text>();

        return effectText;
    }

    private TMP_Text ResolveUpgradedEffectText()
    {
        if (upgradedEffectText != null)
            return upgradedEffectText;

        Transform root = panelRoot != null ? panelRoot.transform : transform;
        Transform found = FindChildByName(root, "UpgradedEffectText") ??
                          FindChildByName(root, "UpgradeEffectText") ??
                          FindChildByName(root, "AfterEffectText");

        if (found != null)
            upgradedEffectText = found.GetComponent<TMP_Text>();

        return upgradedEffectText;
    }

    private void SetSelectedSkillIcon(Sprite selectedIcon)
    {
        Image targetImage = ResolveSelectedSkillIconImage();

        if (targetImage == null)
            return;

        CacheSelectedSkillIconDefault(targetImage);
        targetImage.sprite = selectedIcon;
        targetImage.enabled = selectedIcon != null;
        targetImage.color = selectedIconDefaultColor;
        targetImage.preserveAspect = true;
    }

    private void ClearSelectedUpgradeSelection()
    {
        hasSelectedUpgradeRequest = false;
        selectedUpgradeRequest = default;

        Image targetImage = ResolveSelectedSkillIconImage();

        if (targetImage == null)
            return;

        CacheSelectedSkillIconDefault(targetImage);
        targetImage.sprite = selectedIconDefaultSprite;
        targetImage.enabled = selectedIconDefaultEnabled;
        targetImage.color = selectedIconDefaultColor;
    }

    private Image ResolveSelectedSkillIconImage()
    {
        if (selectedSkillIconImage != null)
            return selectedSkillIconImage;

        Transform root = panelRoot != null ? panelRoot.transform : transform;
        Transform wheel = root.Find("Wheel") ?? FindChildByName(root, "Wheel");
        Transform image = wheel != null
            ? wheel.Find("Image") ?? FindChildByName(wheel, "Image")
            : null;

        if (image == null)
            return null;

        selectedSkillIconImage = image.GetComponent<Image>();
        return selectedSkillIconImage;
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nested = FindChildByName(child, childName);

            if (nested != null)
                return nested;
        }

        return null;
    }

    private void CacheSelectedSkillIconDefault(Image targetImage)
    {
        if (hasCachedSelectedIconDefault || targetImage == null)
            return;

        selectedIconDefaultSprite = targetImage.sprite;
        selectedIconDefaultEnabled = targetImage.enabled;
        selectedIconDefaultColor = targetImage.color;
        hasCachedSelectedIconDefault = true;
    }

    private Sprite ResolveSkillIcon(string skillId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.SkillDatabase != null &&
            DataManager.Instance.SkillDatabase.TryGet(skillId, out SkillMasterData skillData) &&
            skillData.Icon != null)
        {
            return skillData.Icon;
        }

        if (DataManager.Instance.SkillIconDatabase != null &&
            DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out Sprite databaseIcon))
        {
            return databaseIcon;
        }

        return null;
    }

    private void Clear()
    {
        if (upgradeCompleteCoroutine != null)
        {
            StopCoroutine(upgradeCompleteCoroutine);
            upgradeCompleteCoroutine = null;
        }

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }

    private void ConfigureContentLayout()
    {
        if (contentRoot == null)
            return;

        iconPadding ??= new RectOffset();

        Vector2 iconSize = ResolveIconSize();
        GridLayoutGroup grid = contentRoot.GetComponent<GridLayoutGroup>();

        if (grid == null)
            grid = contentRoot.gameObject.AddComponent<GridLayoutGroup>();

        DisableCompetingLayoutGroups(grid);

        grid.enabled = true;
        grid.childAlignment = iconAlignment;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.cellSize = iconSize;
        grid.spacing = iconSpacing;
        grid.padding = iconPadding;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = CalculateColumnCount(iconSize);

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();

        if (fitter == null)
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void DisableCompetingLayoutGroups(GridLayoutGroup activeGrid)
    {
        LayoutGroup[] layoutGroups = contentRoot.GetComponents<LayoutGroup>();

        for (int i = 0; i < layoutGroups.Length; i++)
        {
            LayoutGroup layoutGroup = layoutGroups[i];

            if (layoutGroup == null || layoutGroup == activeGrid)
                continue;

            layoutGroup.enabled = false;
        }
    }

    private int CalculateColumnCount(Vector2 iconSize)
    {
        RectTransform rectTransform = contentRoot as RectTransform;
        float width = rectTransform != null ? rectTransform.rect.width : 0f;

        if (width <= 0f && rectTransform != null)
            width = rectTransform.sizeDelta.x;

        if (width <= 0f)
            return 1;

        float availableWidth = Mathf.Max(0f, width - iconPadding.left - iconPadding.right);
        float cellWidth = Mathf.Max(1f, iconSize.x);
        float spacing = Mathf.Max(0f, iconSpacing.x);

        return Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing) / (cellWidth + spacing)));
    }

    private Vector2 ResolveIconSize()
    {
        Vector2 size = fallbackIconSize;

        if (iconPrefab == null)
            return ClampIconSize(size);

        LayoutElement prefabLayout = iconPrefab.GetComponent<LayoutElement>();

        if (prefabLayout != null &&
            prefabLayout.preferredWidth > 0f &&
            prefabLayout.preferredHeight > 0f)
        {
            return ClampIconSize(new Vector2(prefabLayout.preferredWidth, prefabLayout.preferredHeight));
        }

        RectTransform prefabRect = iconPrefab.GetComponent<RectTransform>();

        if (prefabRect == null)
            return ClampIconSize(size);

        Vector2 rectSize = prefabRect.rect.size;

        if (rectSize.x > 0f && rectSize.y > 0f)
            return ClampIconSize(rectSize);

        if (prefabRect.sizeDelta.x > 0f && prefabRect.sizeDelta.y > 0f)
            return ClampIconSize(prefabRect.sizeDelta);

        return ClampIconSize(size);
    }

    private Vector2 ClampIconSize(Vector2 size)
    {
        return new Vector2(
            Mathf.Max(1f, size.x),
            Mathf.Max(1f, size.y)
        );
    }

    private void PrepareSpawnedItemLayout(SkillUpgradeIconItem item)
    {
        if (item == null)
            return;

        Vector2 iconSize = ResolveIconSize();
        RectTransform rectTransform = item.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = iconSize;
            rectTransform.localScale = Vector3.one;
        }

        LayoutElement layoutElement = item.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = item.gameObject.AddComponent<LayoutElement>();

        layoutElement.ignoreLayout = false;
        layoutElement.minWidth = iconSize.x;
        layoutElement.minHeight = iconSize.y;
        layoutElement.preferredWidth = iconSize.x;
        layoutElement.preferredHeight = iconSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private void RebuildContentLayout()
    {
        if (contentRoot is RectTransform rectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private bool TryGetSkillUpgradeId(string skillId, out string upgradeSkillId)
    {
        upgradeSkillId = null;

        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        int lastUnderscoreIndex = skillId.LastIndexOf('_');

        if (lastUnderscoreIndex < 0)
            return false;

        string prefix = skillId.Substring(0, lastUnderscoreIndex + 1);
        string numberText = skillId.Substring(lastUnderscoreIndex + 1);

        if (!int.TryParse(numberText, out int number))
            return false;

        // 이미 강화된 스킬이면 제외
        if (number % 2 == 0)
            return false;

        int upgradeNumber = number + 1;
        string upgradedNumberText = upgradeNumber.ToString(new string('0', numberText.Length));
        upgradeSkillId = prefix + upgradedNumberText;

        if (DataManager.Instance == null)
            return false;

        if (!DataManager.Instance.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData currentSkill) ||
            !SkillRarityUtility.CanUpgrade(currentSkill))
        {
            return false;
        }

        return DataManager.Instance.SkillDatabase.TryGet(upgradeSkillId, out _);
    }
}
