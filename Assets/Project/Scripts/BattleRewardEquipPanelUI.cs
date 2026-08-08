using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class BattleRewardEquipPanelUI : MonoBehaviour
{
    private const int CharacterCount = 3;
    private const int VisibleRelicSlotCount = 6;
    private const int VisibleSkillSlotCount = 3;

    // skill1은 캐릭터 전용 스킬(EquippedSkillIds[1]) 표시용이고,
    // 실제 획득 기억은 현재 자유 슬롯인 skill2/skill3(EquippedSkillIds[2]/[3])에 새길 수 있습니다.
    private static readonly int[] RuntimeSkillSlotIndices = { 1, 2, 3 };

    [Header("Item")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemRarityText;
    [SerializeField] private TMP_Text itemEffectText;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private GameObject relicDeleteTextRoot;
    [SerializeField] private GameObject skillDeleteTextRoot;
    [SerializeField] private TMP_Text extractionValueText;

    [Header("Selection Colors")]
    [SerializeField] private Color normalCharacterColor = new Color32(0x4E, 0x4E, 0x4E, 0xFF);
    [SerializeField] private Color selectedCharacterColor = new Color32(0x3C, 0x44, 0x76, 0xFF);
    [SerializeField] private Color selectedSkillColor = new Color32(0x57, 0x6F, 0xAF, 0xFF);

    [Header("Extraction")]
    [SerializeField, Range(0f, 1f)] private float relicExtractionRate = 0.30f;

    [Header("Close Preview")]
    [SerializeField, Min(0f)] private float closeDelayAfterApply = 0.75f;

    [Header("Sorting")]
    [SerializeField, Min(1)] private int sortingOrderOffset = 20;

    [Header("Auto Bind")]
    [SerializeField] private bool autoBindHierarchy = true;

    private Canvas sortingCanvas;

    private readonly CharacterView[] characterViews = new CharacterView[CharacterCount];

    private BattleRewardData currentReward;
    private Action resolvedCallback;
    private int selectedCharacterIndex = -1;
    private int selectedSkillViewIndex = -1;
    private int selectedRelicRuntimeSlotIndex = -1;
    private bool isResolving;

    private void Awake()
    {
        ResolveReferencesIfNeeded();
        RegisterButtonEvents();
    }

    private void OnEnable()
    {
        ResolveReferencesIfNeeded();
        RegisterButtonEvents();
        RefreshSelectionVisuals();
        RefreshConfirmButton();
    }

    private void Start()
    {
        if (currentReward == null && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (relicExtractionRate < 0f)
            relicExtractionRate = 0f;
        else if (relicExtractionRate > 1f)
            relicExtractionRate = 1f;
    }
#endif

    public static bool TryOpenRelicReward(string relicId, Action resolvedCallback = null)
    {
        return TryOpenExternalReward(BattleRewardType.Relic, relicId, resolvedCallback);
    }

    public static bool TryOpenSkillReward(string skillId, Action resolvedCallback = null)
    {
        return TryOpenExternalReward(BattleRewardType.Skill, skillId, resolvedCallback);
    }

    private static bool TryOpenExternalReward(BattleRewardType type, string rewardId, Action resolvedCallback)
    {
        if (string.IsNullOrWhiteSpace(rewardId) || DataManager.Instance == null)
            return false;

        BattleRewardEquipPanelUI panel = UnityEngine.Object.FindFirstObjectByType<BattleRewardEquipPanelUI>(FindObjectsInactive.Include);
        if (panel == null || panel.currentReward != null)
            return false;

        rewardId = rewardId.Trim();
        BattleRewardData reward = new BattleRewardData
        {
            Type = type,
            RewardId = rewardId
        };

        if (type == BattleRewardType.Relic)
        {
            if (DataManager.Instance.RelicDatabase == null ||
                !DataManager.Instance.RelicDatabase.TryGet(rewardId, out RelicData relic) ||
                relic == null)
            {
                return false;
            }

            reward.Name = string.IsNullOrWhiteSpace(relic.Name) ? rewardId : relic.Name;
            reward.Description = relic.EffectDesc;

            if (DataManager.Instance.RelicIconDatabase != null &&
                DataManager.Instance.RelicIconDatabase.TryGetIcon(rewardId, out Sprite relicIcon))
            {
                reward.Icon = relicIcon;
            }
        }
        else if (type == BattleRewardType.Skill)
        {
            if (DataManager.Instance.SkillDatabase == null ||
                !DataManager.Instance.SkillDatabase.TryGet(rewardId, out SkillMasterData skill) ||
                skill == null)
            {
                return false;
            }

            reward.Name = string.IsNullOrWhiteSpace(skill.Name) ? rewardId : skill.Name;
            reward.Description = GetSkillDescription(skill, string.Empty);
            reward.Icon = skill.Icon;
        }
        else
        {
            return false;
        }

        panel.Open(reward, resolvedCallback);
        return true;
    }

    public void Open(BattleRewardData reward, Action resolvedCallback)
    {
        if (reward == null ||
            (reward.Type != BattleRewardType.Skill && reward.Type != BattleRewardType.Relic))
        {
            return;
        }

        ResolveReferencesIfNeeded();
        RegisterButtonEvents();

        currentReward = reward;
        this.resolvedCallback = resolvedCallback;
        selectedCharacterIndex = -1;
        selectedSkillViewIndex = -1;
        selectedRelicRuntimeSlotIndex = -1;
        isResolving = false;

        RefreshItemInfo();
        RefreshCharacterViews();
        RefreshDeleteButton();
        RefreshSelectionVisuals();
        RefreshConfirmButton();

        if (deleteButton != null)
            deleteButton.interactable = true;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        EnsureTopmostSorting();
    }

    private void EnsureTopmostSorting()
    {
        sortingCanvas ??= GetComponent<Canvas>();
        if (sortingCanvas == null)
            sortingCanvas = gameObject.AddComponent<Canvas>();

        Canvas parentCanvas = transform.parent != null
            ? transform.parent.GetComponentInParent<Canvas>()
            : null;

        if (parentCanvas != null && parentCanvas != sortingCanvas)
            sortingCanvas.sortingLayerID = parentCanvas.sortingLayerID;

        sortingCanvas.overrideSorting = true;
        sortingCanvas.sortingOrder = GetHighestCanvasSortingOrder(sortingCanvas) + Mathf.Max(1, sortingOrderOffset);

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private static int GetHighestCanvasSortingOrder(Canvas excludedCanvas)
    {
        int highest = 0;
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas == excludedCanvas || !canvas.gameObject.activeInHierarchy)
                continue;

            if (canvas.sortingOrder > highest)
                highest = canvas.sortingOrder;
        }

        return highest;
    }

    private void RegisterButtonEvents()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnClickConfirm);
            confirmButton.onClick.AddListener(OnClickConfirm);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(OnClickDelete);
            deleteButton.onClick.AddListener(OnClickDelete);
        }

        for (int i = 0; i < characterViews.Length; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            int characterIndex = i;

            if (view.CharacterButton != null)
            {
                view.CharacterClickAction ??= () => SelectCharacter(characterIndex);
                view.CharacterButton.onClick.RemoveListener(view.CharacterClickAction);
                view.CharacterButton.onClick.AddListener(view.CharacterClickAction);
            }

            for (int skillIndex = 0; skillIndex < view.SkillSlots.Length; skillIndex++)
            {
                SkillSlotView skillSlot = view.SkillSlots[skillIndex];
                if (skillSlot?.Button == null)
                    continue;

                int capturedSkillIndex = skillIndex;
                skillSlot.ClickAction ??= () => SelectCharacterArea(characterIndex, capturedSkillIndex);
                skillSlot.Button.onClick.RemoveListener(skillSlot.ClickAction);
                skillSlot.Button.onClick.AddListener(skillSlot.ClickAction);
            }
        }
    }

    private void UnregisterButtonEvents()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnClickConfirm);

        if (deleteButton != null)
            deleteButton.onClick.RemoveListener(OnClickDelete);
    }

    private void SelectCharacter(int characterIndex)
    {
        if (!CanInteract() || !IsValidCharacterIndex(characterIndex))
            return;

        CharacterView view = characterViews[characterIndex];
        if (view == null || string.IsNullOrWhiteSpace(view.CharacterId))
            return;

        selectedCharacterIndex = characterIndex;
        selectedSkillViewIndex = -1;
        selectedRelicRuntimeSlotIndex = -1;

        if (currentReward.Type == BattleRewardType.Relic)
            selectedRelicRuntimeSlotIndex = FindFirstCompatibleEmptyRelicSlot(view.CharacterId);
        else if (currentReward.Type == BattleRewardType.Skill)
            TrySelectSkillDestination(view.CharacterId);

        RefreshSelectionVisuals();
        RefreshConfirmButton();
    }

    private void SelectCharacterArea(int characterIndex, int skillViewIndex)
    {
        if (currentReward != null &&
            currentReward.Type == BattleRewardType.Skill &&
            IsSupportedSkillViewIndex(skillViewIndex))
        {
            SelectSkillSlot(characterIndex, skillViewIndex);
            return;
        }

        SelectCharacter(characterIndex);
    }

    private void SelectSkillSlot(int characterIndex, int skillViewIndex)
    {
        if (!CanInteract() || currentReward == null || currentReward.Type != BattleRewardType.Skill)
            return;

        if (!IsValidCharacterIndex(characterIndex) ||
            skillViewIndex < 0 || skillViewIndex >= RuntimeSkillSlotIndices.Length)
        {
            return;
        }

        int runtimeSkillIndex = RuntimeSkillSlotIndices[skillViewIndex];
        if (!SkillInventoryEquipService.IsFreeSkillSlotIndex(runtimeSkillIndex))
            return;

        CharacterView view = characterViews[characterIndex];
        if (view == null || string.IsNullOrWhiteSpace(view.CharacterId))
            return;

        CharacterRuntimeData character = GetCharacterRuntime(view.CharacterId);
        SkillMasterData nextSkill = ResolveSkill(currentReward.RewardId);
        string previousSkillId = GetEquippedSkillId(character, runtimeSkillIndex);
        if (character == null ||
            !SkillRarityUtility.CanEquipToFreeSlot(nextSkill) ||
            (!string.IsNullOrWhiteSpace(previousSkillId) &&
             !SkillRarityUtility.CanUnequip(ResolveSkill(previousSkillId))))
        {
            SelectCharacter(characterIndex);
            return;
        }

        selectedCharacterIndex = characterIndex;
        selectedSkillViewIndex = skillViewIndex;
        selectedRelicRuntimeSlotIndex = -1;

        RefreshSelectionVisuals();
        RefreshConfirmButton();
    }

    private void OnClickConfirm()
    {
        if (!CanInteract() || currentReward == null)
            return;

        if (currentReward.Type == BattleRewardType.Relic)
        {
            ConfirmRelicEquip();
            return;
        }

        if (currentReward.Type == BattleRewardType.Skill)
            OpenSkillConfirmDialog();
    }

    private void ConfirmRelicEquip()
    {
        if (!TryGetSelectedCharacter(out CharacterView view))
            return;

        if (DataManager.Instance == null || currentReward == null)
            return;

        CharacterRuntimeData character = GetCharacterRuntime(view.CharacterId);
        RelicData relic = ResolveRelic(currentReward.RewardId);
        if (character == null || relic == null)
            return;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(character);
        if (!IsSelectedRelicSlotValid(character, relic))
        {
            selectedRelicRuntimeSlotIndex = FindFirstCompatibleEmptyRelicSlot(view.CharacterId);
            if (!IsSelectedRelicSlotValid(character, relic))
            {
                RefreshSelectionVisuals();
                RefreshConfirmButton();
                return;
            }
        }

        bool isActiveRelic = ActiveRelicEffectResolver.IsActiveRelic(relic);
        bool isActiveSlot = selectedRelicRuntimeSlotIndex == ActiveRelicRuntimeUtility.ActiveRelicSlotIndex;
        if (isActiveRelic != isActiveSlot)
            return;

        character.EquippedRelicIds[selectedRelicRuntimeSlotIndex] = currentReward.RewardId.Trim();
        if (isActiveRelic)
            ActiveRelicRuntimeUtility.ResetUses(character, relic);

        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(character);
        BeginAppliedPreviewAndClose();
    }

    private void OpenSkillConfirmDialog()
    {
        if (!TryGetSelectedCharacter(out CharacterView view))
            return;

        if (!IsSelectedSkillDestinationValid(view.CharacterId))
        {
            selectedSkillViewIndex = -1;
            if (!TrySelectSkillDestination(view.CharacterId))
            {
                RefreshSelectionVisuals();
                RefreshConfirmButton();
                return;
            }
        }

        int runtimeSkillIndex = RuntimeSkillSlotIndices[selectedSkillViewIndex];
        if (!SkillInventoryEquipService.IsFreeSkillSlotIndex(runtimeSkillIndex))
            return;

        string previousSkillId = GetEquippedSkillId(view.CharacterId, runtimeSkillIndex);
        string message;

        if (string.IsNullOrWhiteSpace(previousSkillId))
        {
            string characterName = string.IsNullOrWhiteSpace(view.CharacterName)
                ? view.CharacterId
                : view.CharacterName;
            message = $"'{characterName}'에게 이 기억을 새기시겠습니까?";
        }
        else
        {
            message = "기억을 바꾸시겠습니까?\n기존에 새겨진 기억은 사라집니다.";
        }

        if (UIManager.Instance == null)
        {
            ApplySelectedSkill(view, runtimeSkillIndex, previousSkillId);
            return;
        }

        UIManager.Instance.ShowConfirmDialog(
            message,
            () =>
            {
                UIManager.Instance.HideConfirmDialog();
                ApplySelectedSkill(view, runtimeSkillIndex, previousSkillId);
            },
            () => UIManager.Instance.HideConfirmDialog());
    }

    private void ApplySelectedSkill(CharacterView view, int runtimeSkillIndex, string previousSkillId)
    {
        if (view == null || DataManager.Instance == null || currentReward == null)
            return;

        CharacterRuntimeData character = GetCharacterRuntime(view.CharacterId);
        if (character == null)
            return;

        SkillInventoryEquipService.EnsureEquippedSkillArray(character);
        if (!SkillInventoryEquipService.IsFreeSkillSlotIndex(runtimeSkillIndex))
            return;

        SkillMasterData nextSkill = ResolveSkill(currentReward.RewardId);
        if (!SkillRarityUtility.CanEquipToFreeSlot(nextSkill))
            return;

        if (!string.IsNullOrWhiteSpace(previousSkillId))
        {
            SkillMasterData previousSkill = ResolveSkill(previousSkillId);
            if (!SkillRarityUtility.CanUnequip(previousSkill))
                return;
        }

        character.EquippedSkillIds[runtimeSkillIndex] = currentReward.RewardId.Trim();
        DataManager.Instance.CharacterRuntimeStore.AddOrUpdate(character);

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
        runtime.AcquiredSkillIds ??= new List<string>();
        AddUnique(runtime.AcquiredSkillIds, currentReward.RewardId);
        DataManager.Instance.BattleRuntimeStore.Set(runtime);

        BeginAppliedPreviewAndClose();
    }

    private void OnClickDelete()
    {
        if (!CanInteract() || currentReward == null)
            return;

        if (currentReward.Type == BattleRewardType.Skill)
        {
            CompleteResolvedReward();
            return;
        }

        if (currentReward.Type == BattleRewardType.Relic)
        {
            int extractionValue = GetRelicExtractionValue(currentReward.RewardId);

            if (extractionValue > 0 && DataManager.Instance != null)
            {
                BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
                runtime.Remnant += extractionValue;
                DataManager.Instance.BattleRuntimeStore.Set(runtime);
                BattleGoldHudUI.RefreshAll();
            }

            CompleteResolvedReward();
        }
    }

    private void BeginAppliedPreviewAndClose()
    {
        if (isResolving)
            return;

        isResolving = true;
        RefreshCharacterViews();
        RefreshSelectionVisuals();
        RefreshConfirmButton();

        if (confirmButton != null)
            confirmButton.interactable = false;
        if (deleteButton != null)
            deleteButton.interactable = false;

        StartCoroutine(CloseAfterAppliedPreviewRoutine());
    }

    private IEnumerator CloseAfterAppliedPreviewRoutine()
    {
        float delay = Mathf.Max(0f, closeDelayAfterApply);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        FinishResolvedReward();
    }

    private void CompleteResolvedReward()
    {
        if (isResolving)
            return;

        isResolving = true;
        FinishResolvedReward();
    }

    private void FinishResolvedReward()
    {
        Action callback = resolvedCallback;

        currentReward = null;
        resolvedCallback = null;
        selectedCharacterIndex = -1;
        selectedSkillViewIndex = -1;
        selectedRelicRuntimeSlotIndex = -1;

        gameObject.SetActive(false);
        callback?.Invoke();
    }

    private void RefreshItemInfo()
    {
        if (currentReward == null)
            return;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = currentReward.Icon;
            itemIconImage.enabled = currentReward.Icon != null;
            itemIconImage.color = Color.white;
        }

        if (itemNameText != null)
            itemNameText.text = currentReward.GetDisplayName();

        if (currentReward.Type == BattleRewardType.Skill)
        {
            SkillMasterData skill = ResolveSkill(currentReward.RewardId);

            if (itemRarityText != null)
                itemRarityText.text = skill != null ? SkillRarityUtility.GetDisplayName(skill.Rarity) : string.Empty;

            if (itemEffectText != null)
                itemEffectText.text = GetSkillDescription(skill, currentReward.Description);
        }
        else
        {
            RelicData relic = ResolveRelic(currentReward.RewardId);

            if (itemRarityText != null)
                itemRarityText.text = relic?.Rarity ?? string.Empty;

            if (itemEffectText != null)
                itemEffectText.text = !string.IsNullOrWhiteSpace(relic?.EffectDesc)
                    ? relic.EffectDesc
                    : currentReward.Description ?? string.Empty;
        }
    }

    private void RefreshCharacterViews()
    {
        if (DataManager.Instance == null)
            return;

        for (int i = 0; i < characterViews.Length; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            string characterId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(i);
            view.CharacterId = characterId;
            view.CharacterName = string.Empty;

            bool hasCharacter = !string.IsNullOrWhiteSpace(characterId);
            if (view.Root != null)
                view.Root.gameObject.SetActive(hasCharacter);

            if (!hasCharacter)
                continue;

            CharacterMasterData master = DataManager.Instance.CharacterDatabase?.Get(characterId);
            view.CharacterName = master != null && !string.IsNullOrWhiteSpace(master.Name)
                ? master.Name
                : characterId;

            if (view.NameText != null)
                view.NameText.text = view.CharacterName;

            if (DataManager.Instance.CharacterIconDatabase != null)
            {
                DataManager.Instance.CharacterIconDatabase.TryGetMark(characterId, out Sprite mark1);
                DataManager.Instance.CharacterIconDatabase.TryGetMark2(characterId, out Sprite mark2);
                ApplyImage(view.Mark1Image, mark1);
                ApplyImage(view.Mark2Image, mark2);
            }

            RefreshCharacterSkills(view);
            RefreshCharacterRelics(view);
        }
    }

    private void RefreshCharacterSkills(CharacterView view)
    {
        CharacterRuntimeData runtime = GetCharacterRuntime(view.CharacterId);

        for (int i = 0; i < view.SkillSlots.Length; i++)
        {
            SkillSlotView slot = view.SkillSlots[i];
            if (slot == null)
                continue;

            int runtimeIndex = i < RuntimeSkillSlotIndices.Length ? RuntimeSkillSlotIndices[i] : -1;
            if (slot.Root != null)
                slot.Root.gameObject.SetActive(true);

            string skillId = GetEquippedSkillId(runtime, runtimeIndex);
            Sprite icon = null;

            if (!string.IsNullOrWhiteSpace(skillId) && DataManager.Instance?.SkillIconDatabase != null)
                DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out icon);

            ApplyImage(slot.IconImage, icon, SkillRarityUtility.GetSkillIconColor(skillId));

            if (slot.Button != null)
                slot.Button.interactable = currentReward != null;
        }
    }

    private void RefreshCharacterRelics(CharacterView view)
    {
        CharacterRuntimeData runtime = GetCharacterRuntime(view.CharacterId);
        if (runtime != null)
            ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);

        if (view.ActiveRelicIcon != null)
        {
            string activeRelicId = runtime?.EquippedRelicIds != null &&
                                   ActiveRelicRuntimeUtility.ActiveRelicSlotIndex < runtime.EquippedRelicIds.Length
                ? runtime.EquippedRelicIds[ActiveRelicRuntimeUtility.ActiveRelicSlotIndex]
                : null;

            Sprite activeIcon = null;
            if (!string.IsNullOrWhiteSpace(activeRelicId) && DataManager.Instance?.RelicIconDatabase != null)
                DataManager.Instance.RelicIconDatabase.TryGetIcon(activeRelicId, out activeIcon);

            ApplyImage(view.ActiveRelicIcon, activeIcon);
        }

        for (int i = 0; i < view.RelicIcons.Length; i++)
        {
            Image image = view.RelicIcons[i];
            if (image == null)
                continue;

            int runtimeRelicIndex = i + 1;
            string relicId = runtime?.EquippedRelicIds != null && runtimeRelicIndex < runtime.EquippedRelicIds.Length
                ? runtime.EquippedRelicIds[runtimeRelicIndex]
                : null;

            Sprite icon = null;
            if (!string.IsNullOrWhiteSpace(relicId) && DataManager.Instance?.RelicIconDatabase != null)
                DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out icon);

            ApplyImage(image, icon);
        }
    }

    private void RefreshDeleteButton()
    {
        if (currentReward == null)
            return;

        bool isRelic = currentReward.Type == BattleRewardType.Relic;

        if (relicDeleteTextRoot != null)
            relicDeleteTextRoot.SetActive(isRelic);

        if (skillDeleteTextRoot != null)
            skillDeleteTextRoot.SetActive(!isRelic);

        if (extractionValueText != null)
        {
            extractionValueText.gameObject.SetActive(isRelic);
            extractionValueText.text = isRelic
                ? GetRelicExtractionValue(currentReward.RewardId).ToString()
                : string.Empty;
        }
    }

    private void RefreshSelectionVisuals()
    {
        for (int i = 0; i < characterViews.Length; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            if (view.BackImage != null)
                view.BackImage.color = i == selectedCharacterIndex
                    ? selectedCharacterColor
                    : normalCharacterColor;

            for (int j = 0; j < view.SkillSlots.Length; j++)
            {
                SkillSlotView skillSlot = view.SkillSlots[j];
                if (skillSlot?.BackImage == null)
                    continue;

                skillSlot.BackImage.color = i == selectedCharacterIndex && j == selectedSkillViewIndex
                    ? selectedSkillColor
                    : skillSlot.DefaultBackColor;
            }
        }
    }

    private void RefreshConfirmButton()
    {
        if (confirmButton == null)
            return;

        bool interactable = false;

        if (currentReward != null && IsValidCharacterIndex(selectedCharacterIndex))
        {
            if (currentReward.Type == BattleRewardType.Skill)
            {
                CharacterView view = characterViews[selectedCharacterIndex];
                interactable = view != null && IsSelectedSkillDestinationValid(view.CharacterId);
            }
            else if (currentReward.Type == BattleRewardType.Relic)
            {
                CharacterView view = characterViews[selectedCharacterIndex];
                CharacterRuntimeData character = view != null ? GetCharacterRuntime(view.CharacterId) : null;
                RelicData relic = ResolveRelic(currentReward.RewardId);
                interactable = IsSelectedRelicSlotValid(character, relic);
            }
        }

        confirmButton.interactable = interactable;
    }

    private bool TrySelectSkillDestination(string characterId)
    {
        CharacterRuntimeData character = GetCharacterRuntime(characterId);
        SkillMasterData nextSkill = ResolveSkill(currentReward?.RewardId);
        if (!SkillRarityUtility.CanEquipToFreeSlot(nextSkill))
            return false;

        if (!BattleRewardEquipSelectionPolicy.TryFindSkillViewIndex(
                character,
                ResolveSkill,
                out int skillViewIndex))
        {
            return false;
        }

        selectedSkillViewIndex = skillViewIndex;
        return true;
    }

    private bool IsSelectedSkillDestinationValid(string characterId)
    {
        if (!IsSupportedSkillViewIndex(selectedSkillViewIndex))
            return false;

        int runtimeSkillIndex = RuntimeSkillSlotIndices[selectedSkillViewIndex];
        CharacterRuntimeData character = GetCharacterRuntime(characterId);
        SkillMasterData nextSkill = ResolveSkill(currentReward?.RewardId);
        if (character == null || !SkillRarityUtility.CanEquipToFreeSlot(nextSkill))
            return false;

        SkillInventoryEquipService.EnsureEquippedSkillArray(character);
        string previousSkillId = character.EquippedSkillIds[runtimeSkillIndex];
        return string.IsNullOrWhiteSpace(previousSkillId) ||
               SkillRarityUtility.CanUnequip(ResolveSkill(previousSkillId));
    }

    private bool IsSelectedRelicSlotValid(CharacterRuntimeData character, RelicData relic)
    {
        if (character == null || relic == null)
            return false;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(character);
        if (selectedRelicRuntimeSlotIndex < 0 ||
            selectedRelicRuntimeSlotIndex >= character.EquippedRelicIds.Length ||
            !string.IsNullOrWhiteSpace(character.EquippedRelicIds[selectedRelicRuntimeSlotIndex]))
        {
            return false;
        }

        bool isActiveRelic = ActiveRelicEffectResolver.IsActiveRelic(relic);
        bool isActiveSlot = selectedRelicRuntimeSlotIndex == ActiveRelicRuntimeUtility.ActiveRelicSlotIndex;
        return isActiveRelic == isActiveSlot;
    }

    private static bool IsSupportedSkillViewIndex(int skillViewIndex)
    {
        return skillViewIndex >= 0 &&
               skillViewIndex < RuntimeSkillSlotIndices.Length &&
               SkillInventoryEquipService.IsFreeSkillSlotIndex(RuntimeSkillSlotIndices[skillViewIndex]);
    }

    private int FindFirstCompatibleEmptyRelicSlot(string characterId)
    {
        CharacterRuntimeData runtime = GetCharacterRuntime(characterId);
        RelicData relic = ResolveRelic(currentReward?.RewardId);

        if (runtime == null || relic == null)
            return -1;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        bool activeRelic = ActiveRelicEffectResolver.IsActiveRelic(relic);

        int start = activeRelic ? ActiveRelicRuntimeUtility.ActiveRelicSlotIndex : 1;
        int endExclusive = activeRelic
            ? ActiveRelicRuntimeUtility.ActiveRelicSlotIndex + 1
            : Mathf.Min(ActiveRelicRuntimeUtility.ActiveRelicSlotIndex + 1 + VisibleRelicSlotCount, runtime.EquippedRelicIds.Length);

        for (int i = start; i < endExclusive; i++)
        {
            if (string.IsNullOrWhiteSpace(runtime.EquippedRelicIds[i]))
                return i;
        }

        return -1;
    }

    private int GetRelicExtractionValue(string relicId)
    {
        RelicData relic = ResolveRelic(relicId);
        if (relic == null)
            return 0;

        if (!LobbyRelicPricePolicy.TryGetPrice(relic.Rarity, out int purchasePrice))
            return 0;

        return Mathf.Max(0, Mathf.FloorToInt(purchasePrice * relicExtractionRate));
    }

    private SkillMasterData ResolveSkill(string skillId)
    {
        if (DataManager.Instance?.SkillDatabase == null || string.IsNullOrWhiteSpace(skillId))
            return null;

        DataManager.Instance.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData skill);
        return skill;
    }

    private RelicData ResolveRelic(string relicId)
    {
        if (DataManager.Instance?.RelicDatabase == null || string.IsNullOrWhiteSpace(relicId))
            return null;

        DataManager.Instance.RelicDatabase.TryGet(relicId.Trim(), out RelicData relic);
        return relic;
    }

    private CharacterRuntimeData GetCharacterRuntime(string characterId)
    {
        if (DataManager.Instance?.CharacterRuntimeStore == null || string.IsNullOrWhiteSpace(characterId))
            return null;

        DataManager.Instance.CharacterRuntimeStore.TryGet(characterId.Trim(), out CharacterRuntimeData runtime);
        return runtime;
    }

    private string GetEquippedSkillId(string characterId, int runtimeIndex)
    {
        return GetEquippedSkillId(GetCharacterRuntime(characterId), runtimeIndex);
    }

    private static string GetEquippedSkillId(CharacterRuntimeData runtime, int runtimeIndex)
    {
        if (runtime?.EquippedSkillIds == null || runtimeIndex < 0 || runtimeIndex >= runtime.EquippedSkillIds.Length)
            return null;

        return runtime.EquippedSkillIds[runtimeIndex];
    }

    private static string GetSkillDescription(SkillMasterData skill, string fallback)
    {
        if (skill == null)
            return fallback ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(skill.Details))
            return skill.Details;

        if (!string.IsNullOrWhiteSpace(skill.ToolTip))
            return skill.ToolTip;

        if (!string.IsNullOrWhiteSpace(skill.EffectDescription))
            return skill.EffectDescription;

        if (!string.IsNullOrWhiteSpace(skill.EffectDesc))
            return skill.EffectDesc;

        return fallback ?? string.Empty;
    }

    private bool TryGetSelectedCharacter(out CharacterView view)
    {
        view = null;

        if (!IsValidCharacterIndex(selectedCharacterIndex))
            return false;

        view = characterViews[selectedCharacterIndex];
        return view != null && !string.IsNullOrWhiteSpace(view.CharacterId);
    }

    private bool CanInteract()
    {
        return !isResolving && currentReward != null;
    }

    private static bool IsValidCharacterIndex(int index)
    {
        return index >= 0 && index < CharacterCount;
    }

    private static bool Contains(IList<string> list, string value)
    {
        if (list == null || string.IsNullOrWhiteSpace(value))
            return false;

        string target = value.Trim();
        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i]?.Trim(), target, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static void AddUnique(IList<string> list, string value)
    {
        if (list == null || string.IsNullOrWhiteSpace(value) || Contains(list, value))
            return;

        list.Add(value.Trim());
    }

    private static void RemoveAll(IList<string> list, string value)
    {
        if (list == null || string.IsNullOrWhiteSpace(value))
            return;

        string target = value.Trim();
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (string.Equals(list[i]?.Trim(), target, StringComparison.Ordinal))
                list.RemoveAt(i);
        }
    }

    private static void ApplyImage(Image image, Sprite sprite)
    {
        ApplyImage(image, sprite, Color.white);
    }

    private static void ApplyImage(Image image, Sprite sprite, Color color)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.color = color;
        image.enabled = sprite != null;
    }

    private void ResolveReferencesIfNeeded()
    {
        if (!autoBindHierarchy)
            return;

        Transform item = FindChildRecursive(transform, "item");
        Transform itemImage = item != null ? FindChildRecursive(item, "ItemImage") : null;

        itemIconImage ??= itemImage != null
            ? FindImage(itemImage, "Icon")
            : null;
        itemNameText ??= item != null ? FindText(item, "Name") : null;
        itemRarityText ??= item != null ? FindText(item, "Rarity") : null;
        itemEffectText ??= item != null ? FindText(item, "Effect") : null;

        confirmButton ??= FindButtonByNames(transform, "Comfirm_Button", "Confirm_Button", "ConfirmButton", "Confirm", "OK_Button", "OKButton");
        deleteButton ??= FindButtonByNames(transform, "Delete_Button", "DeleteButton", "Delete");

        if (deleteButton != null)
        {
            Transform relicText = FindChildRecursive(deleteButton.transform, "RelicText");
            Transform skillText = FindChildRecursive(deleteButton.transform, "SkillText");

            relicDeleteTextRoot ??= relicText != null ? relicText.gameObject : null;
            skillDeleteTextRoot ??= skillText != null ? skillText.gameObject : null;

            extractionValueText ??= relicText != null
                ? FindTextByNames(relicText, "Value", "Amount", "Price")
                : null;
        }

        for (int i = 0; i < CharacterCount; i++)
            characterViews[i] ??= BuildCharacterView(i);
    }

    private CharacterView BuildCharacterView(int index)
    {
        Transform root = FindChildRecursive(transform, "Char" + (index + 1));
        if (root == null)
            return null;

        Transform backTransform = root.Find("Back") ?? FindChildRecursive(root, "Back");
        Image backImage = backTransform != null ? backTransform.GetComponent<Image>() : null;
        Button characterButton = root.GetComponent<Button>();
        if (characterButton == null)
            characterButton = root.gameObject.AddComponent<Button>();
        characterButton.targetGraphic = backImage;

        CharacterView view = new CharacterView
        {
            Root = root,
            BackImage = backImage,
            CharacterButton = characterButton,
            NameText = FindTextByNames(root, "Name"),
            Mark1Image = FindImageByNames(root, "mark1", "Mark1"),
            Mark2Image = FindImageByNames(root, "mark2", "Mark2")
        };

        Transform skillRoot = root.Find("Skill") ?? FindChildRecursive(root, "Skill");
        for (int i = 0; i < VisibleSkillSlotCount; i++)
        {
            Transform slotRoot = skillRoot != null
                ? FindChildRecursive(skillRoot, "skill" + (i + 1))
                : null;

            if (slotRoot == null)
                continue;

            Transform skillBackTransform = slotRoot.Find("Back") ?? FindChildRecursive(slotRoot, "Back");
            Image skillBack = skillBackTransform != null ? skillBackTransform.GetComponent<Image>() : null;
            Button button = slotRoot.GetComponent<Button>();
            if (button == null)
                button = slotRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = skillBack;

            Image icon = FindImageByNames(slotRoot, "Icon");

            view.SkillSlots[i] = new SkillSlotView
            {
                Root = slotRoot,
                BackImage = skillBack,
                IconImage = icon,
                Button = button,
                DefaultBackColor = skillBack != null ? skillBack.color : Color.white
            };
        }

        Transform activeRoot = root.Find("Active") ?? FindChildRecursive(root, "Active");
        if (activeRoot != null)
            view.ActiveRelicIcon = FindImageByNames(activeRoot, "Icon");

        Transform relicRoot = root.Find("Relic") ?? FindChildRecursive(root, "Relic");
        for (int i = 0; i < VisibleRelicSlotCount; i++)
        {
            string slotName = "Relic" + (i + 1).ToString("00");
            Transform relicSlot = relicRoot != null ? FindChildRecursive(relicRoot, slotName) : null;
            if (relicSlot == null)
                continue;

            Image icon = FindImageByNames(relicSlot, "Icon") ?? relicSlot.GetComponent<Image>();
            view.RelicIcons[i] = icon;
        }

        return view;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (string.Equals(child.name, targetName, StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nested = FindChildRecursive(child, targetName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static Button FindButtonByNames(Transform root, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform target = FindChildRecursive(root, names[i]);
            if (target == null)
                continue;

            Button button = target.GetComponent<Button>() ?? target.GetComponentInChildren<Button>(true);
            if (button != null)
                return button;
        }

        return null;
    }

    private static TMP_Text FindTextByNames(Transform root, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            TMP_Text text = FindText(root, names[i]);
            if (text != null)
                return text;
        }

        return null;
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        Transform target = FindChildRecursive(root, name);
        return target != null ? target.GetComponent<TMP_Text>() ?? target.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private static Image FindImageByNames(Transform root, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Image image = FindImage(root, names[i]);
            if (image != null)
                return image;
        }

        return null;
    }

    private static Image FindImage(Transform root, string name)
    {
        Transform target = FindChildRecursive(root, name);
        return target != null ? target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true) : null;
    }

    [Serializable]
    private sealed class CharacterView
    {
        public Transform Root;
        public Image BackImage;
        public Button CharacterButton;
        public TMP_Text NameText;
        public Image Mark1Image;
        public Image Mark2Image;
        public UnityAction CharacterClickAction;
        public string CharacterId;
        public string CharacterName;
        public SkillSlotView[] SkillSlots = new SkillSlotView[VisibleSkillSlotCount];
        public Image ActiveRelicIcon;
        public Image[] RelicIcons = new Image[VisibleRelicSlotCount];
    }

    [Serializable]
    private sealed class SkillSlotView
    {
        public Transform Root;
        public Image BackImage;
        public Image IconImage;
        public Button Button;
        public UnityAction ClickAction;
        public Color DefaultBackColor;
    }
}
