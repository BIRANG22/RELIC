using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 로비의 탐사 준비 화면을 관리합니다.
/// Info_Panel과 Ready_Panel은 항상 활성 상태를 유지하며,
/// PlayButton의 탐사 준비 단계에서 화면 안쪽으로 슬라이드 이동합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyEquipPanelUI : MonoBehaviour
{
    private const string ReadyPanelName = "Ready_Panel";
    private const string InfoPanelName = "Info_Panel";
    private const int CharacterCount = 3;
    private const int VisibleRelicSlotCount = 6;
    private const int VisibleSkillSlotCount = 3;
    private const int CompoundMinimumSlotCount = 15;
    private const int ReadyRelicMinimumSlotCount = 3;
    private const int ReadyCompoundMinimumSlotCount = 6;
    private const float ReadyInventorySlotScale = 1.2f;
    private const int EquipmentDragSortingOrder = 10000;
    private const string EquipButtonDefaultText = "장착";
    private const string EquipButtonCancelText = "취소";

    // 로비 Equip_panel의 Skill 1~3은 교체 가능한 기억만 표시합니다.
    // Skill1 = 구현 기억(AbilitySkillId / EquippedSkillIds[1])
    // Skill2 = 자유 장착 기억 1(EquippedSkillIds[2])
    // Skill3 = 자유 장착 기억 2(EquippedSkillIds[3])
    // 본능 기억(PassiveSkillId)과 발현 기억(UniqueSkillId)은 표시하지 않습니다.
    private static readonly int[] RuntimeSkillSlotIndices = { 1, 2, 3 };

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Slide Targets")]
    [SerializeField] private RectTransform equipRect;
    [SerializeField] private RectTransform charterRect;

    [Header("Slide Position")]
    [Tooltip("Info_Panel의 대기 위치 X입니다.")]
    [SerializeField] private float equipStartX = -1350f;
    [Tooltip("탐사 준비 시 Info_Panel이 도착할 X입니다.")]
    [SerializeField] private float equipEndX = -350f;
    [Tooltip("Ready_Panel의 대기 위치 X입니다.")]
    [SerializeField] private float charterStartX = 1350f;
    [Tooltip("탐사 준비 시 Ready_Panel이 도착할 X입니다.")]
    [SerializeField] private float charterEndX = 350f;

    [Header("Slide Animation")]
    [SerializeField, Min(0f)] private float slideDuration = 0.35f;
    [SerializeField]
    private AnimationCurve slideCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f));


    [Header("Opened Panel")]
    [SerializeField] private bool bringToFront = true;

    [Header("Character Data")]
    [Tooltip("Info_Panel/Char1~3 구조를 이름으로 자동 연결합니다.")]
    [SerializeField] private bool autoBindCharacterHierarchy = true;

    [Header("Compound Inventory")]
    [Tooltip("Equip/Compound/Scroll View/Viewport/Content를 비워두면 이름으로 자동 연결합니다.")]
    [SerializeField] private Transform compoundContentRoot;
    [Tooltip("Content에 생성할 StorageSlotUI 프리팹입니다. BattleBagItemSlotUI가 붙어 있어야 합니다.")]
    [SerializeField] private BattleBagItemSlotUI compoundSlotPrefab;
    [Tooltip("연성제가 없어도 표시할 최소 빈 슬롯 수입니다.")]
    [SerializeField, Min(1)] private int compoundMinimumSlotCount = CompoundMinimumSlotCount;

    [Header("Ready Inventory Display")]
    [Tooltip("Ready_Panel/Relic/Viewport/Content입니다. 비워두면 새 구조에서 자동 연결합니다.")]
    [SerializeField] private Transform readyRelicContentRoot;
    [Tooltip("Ready_Panel/Compound/Viewport/Content입니다. 비워두면 새 구조에서 자동 연결합니다.")]
    [SerializeField] private Transform readyCompoundContentRoot;
    [Tooltip("Ready_Panel의 유물/연성제 표시용 StorageSlotUI 프리팹입니다. 비어 있으면 기존 Compound 슬롯 프리팹을 사용합니다.")]
    [SerializeField] private BattleBagItemSlotUI readyInventorySlotPrefab;
    [SerializeField, Min(1)] private int readyRelicMinimumSlotCount = ReadyRelicMinimumSlotCount;
    [SerializeField, Min(1)] private int readyCompoundMinimumSlotCount = ReadyCompoundMinimumSlotCount;
    [SerializeField, Min(0.1f)] private float readyInventorySlotScale = ReadyInventorySlotScale;

    [Header("Ready Inventory Detail")]
    [Tooltip("Ready_Panel/Detail입니다. 실제 유물 또는 연성제 슬롯에 마우스를 올릴 때만 활성화됩니다.")]
    [SerializeField] private GameObject readyDetailRoot;
    [SerializeField] private Image readyDetailIconImage;
    [SerializeField] private TMP_Text readyDetailNameText;
    [SerializeField] private TMP_Text readyDetailEffectText;

    [Header("Character Equip Target")]
    [Tooltip("장착 모드에서 캐릭터 선택 이미지에 마우스를 올렸을 때 사용할 색상입니다.")]
    [SerializeField] private Color characterSelectHoverColor = Color.white;
    [Tooltip("장착 모드에서 캐릭터 Back에 마우스를 올렸을 때 사용할 색상입니다.")]
    [SerializeField] private Color characterBackHoverColor = new Color32(0x3C, 0x44, 0x76, 0xFF);

    private readonly CharacterView[] characterViews = new CharacterView[CharacterCount];
    private OwnedRelicView ownedRelicView;
    private RecordPanelUI recordPanelUI;
    private bool isOwnedRelicSelected;
    private string selectedOwnedRelicId;
    private readonly List<BattleBagItemSlotUI> compoundSlots = new();
    private readonly List<BattleBagItemSlotUI> readyRelicSlots = new();
    private readonly List<BattleBagItemSlotUI> readyCompoundSlots = new();
    private CompoundSelectionView compoundSelectionView;
    private BattleBagItemSlotUI selectedCompoundSlot;
    private string selectedCompoundId;
    private bool isCompoundEquipSelectionActive;
    private GameObject characterTextObject;
    private BattleBagItemSlotUI readyDetailSourceSlot;
    private BattleBagItemSlotUI readyDetailPinnedSlot;
    private bool readyDetailInitialized;

    // Ready_Panel <-> Info_Panel 장착/해제 및 드래그 상태
    private readonly LobbyReadyEquipmentPointerRelay[,] infoRelicTargetRelays = new LobbyReadyEquipmentPointerRelay[CharacterCount, 2];
    private readonly LobbyReadyEquipmentPointerRelay[] infoCompoundTargetRelays = new LobbyReadyEquipmentPointerRelay[CharacterCount];
    private string equipmentDragItemId;
    private bool equipmentDragIsCompound;
    private int equipmentDragSourcePartyIndex = -1;
    private int equipmentDragSourceRuntimeSlotIndex = -1;
    private bool equipmentDragFromInfo;
    private bool equipmentDragHandled;
    private GameObject equipmentDragCanvasObject;
    private Canvas equipmentDragCanvas;
    private RectTransform equipmentDragGhostRect;
    private Image equipmentDragGhostImage;

    private Coroutine slideAnimationCoroutine;
    private RectTransform toggleButtonRect;
    private bool isOpen;
    private bool isClosing;
    private bool isPreparingOpen;

    public bool IsOpen => isOpen && !isClosing;
    public event Action<bool> OpenStateChanged;
    public event Action Closed;

    /// <summary>
    /// 공용 BackgroundPanel의 BackButton에서 탐사 준비 화면을 닫을 때 사용합니다.
    /// 열려 있는 탐사 준비 화면이 있으면 일반 Close 경로를 실행해
    /// Info_Panel / Ready_Panel의 퇴장 슬라이드를 그대로 재생합니다.
    /// </summary>
    public static bool TryCloseOpenReadyPanel()
    {
        LobbyEquipPanelUI[] panels = FindObjectsByType<LobbyEquipPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            LobbyEquipPanelUI panel = panels[i];
            if (panel == null || !panel.IsOpen)
                continue;

            panel.Close();
            return true;
        }

        return false;
    }

    private void Awake()
    {
        ResolvePanelRoot();
        ResolveSlideTargets();
        ResolveCharacterViewsIfNeeded();
        ResolveCharacterTextIfNeeded();
        ResolveOwnedRelicViewIfNeeded();
        ResolveCompoundInventoryIfNeeded();
        ResolveCompoundSelectionViewIfNeeded();
        ResolveReadyInventoryDisplayIfNeeded();
        ResolveReadyInventoryDetailIfNeeded();
        BindInfoPanelEquipmentTargets();
        HideReadyInventoryDetail();
        isOpen = false;
        isClosing = false;
        isPreparingOpen = false;
    }

    private void OnEnable()
    {
        ResolvePanelRoot();
        ResolveSlideTargets();
        EnsurePreparationPanelsActive();
        isClosing = false;
        isPreparingOpen = false;
        isOpen = AreSlideTargetsAtOpenPosition();
        RefreshCharacterData();
    }

    private void OnDisable()
    {
        StopSlideAnimation();
        bool wasOpen = isOpen || isClosing;
        isOpen = false;
        isClosing = false;
        isPreparingOpen = false;
        if (wasOpen)
            OpenStateChanged?.Invoke(false);
        ResetOwnedRelicSelection();
        ResetCompoundSelection();
        HideReadyInventoryDetail();
        ClearEquipmentDragState();
        LobbyPositionModalInputBlocker.Unblock(this);
        LobbyPositionSharedModalBackground.HideAfterReadyPanel();
    }

    private void OnDestroy()
    {
        LobbyPositionModalInputBlocker.Unblock(this);
        LobbyPositionSharedModalBackground.HideAfterReadyPanel();
    }

    public void SetToggleButton(RectTransform buttonRect)
    {
        toggleButtonRect = buttonRect;
    }

    /// <summary>
    /// Ready_Panel을 활성/비활성화합니다.
    /// </summary>
    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        GameObject root = ResolvePanelRoot();
        if (root == null)
        {
            Debug.LogWarning("[LobbyEquipPanelUI] Ready_Panel을 찾을 수 없습니다.", this);
            return;
        }

        if (IsOpen || isPreparingOpen)
            return;

        if (UIPanelButton.IsMenuPanelOpen)
            return;

        isPreparingOpen = true;
        LobbyPositionSharedModalBackground.PrepareForReadyPanel(root, BeginOpenAfterPanelFade);
    }

    private void BeginOpenAfterPanelFade()
    {
        isPreparingOpen = false;

        if (this == null || !isActiveAndEnabled)
            return;

        GameObject root = ResolvePanelRoot();
        if (root == null)
            return;

        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return;

        TitleManager.CloseTitleModePanelsExceptInScene(root);

        ResolveSlideTargets();
        EnsurePreparationPanelsActive();

        if (bringToFront)
        {
            if (equipRect != null)
                equipRect.SetAsLastSibling();
            if (charterRect != null)
                charterRect.SetAsLastSibling();
        }

        StopSlideAnimation();
        isClosing = false;
        isOpen = true;
        OpenStateChanged?.Invoke(true);
        LobbyPositionModalInputBlocker.Block(this);

        ResolveOwnedRelicViewIfNeeded();
        ResolveCompoundInventoryIfNeeded();
        ResolveCompoundSelectionViewIfNeeded();
        RefreshCharacterData();

        slideAnimationCoroutine = StartCoroutine(PlaySlideAnimation(true));
    }

    public void Close()
    {
        GameObject root = ResolvePanelRoot();
        if (root == null)
            return;

        if (!isOpen && !isClosing && !isPreparingOpen)
            return;

        isPreparingOpen = false;

        ResetOwnedRelicSelection();
        ResetCompoundSelection();
        StopSlideAnimation();

        isOpen = false;
        isClosing = true;
        OpenStateChanged?.Invoke(false);

        EnsurePreparationPanelsActive();
        slideAnimationCoroutine = StartCoroutine(PlaySlideAnimation(false));
    }

    /// <summary>
    /// 현재 PartyRuntimeStore / CharacterRuntimeStore 기준으로 Char1~3 표시를 다시 갱신합니다.
    /// 파티 변경, 기억 장착, 연성제/유물 장착 후 필요하면 외부에서도 호출할 수 있습니다.
    /// </summary>
    public void RefreshCharacterData()
    {
        ResolveCharacterViewsIfNeeded();
        ResolveOwnedRelicViewIfNeeded();
        ResolveCompoundInventoryIfNeeded();
        ResolveCompoundSelectionViewIfNeeded();
        ResolveReadyInventoryDisplayIfNeeded();
        ResolveReadyInventoryDetailIfNeeded();
        BindInfoPanelEquipmentTargets();
        RefreshOwnedRelicData();
        RefreshCompoundInventorySlots();
        HideReadyInventoryDetail();
        RefreshReadyInventoryDisplay();

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null)
        {
            ClearCharacterViews();
            return;
        }

        PartyRuntimeStore partyStore = dataManager.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = dataManager.CharacterRuntimeStore;

        if (partyStore == null || characterStore == null)
        {
            ClearCharacterViews();
            return;
        }

        for (int i = 0; i < CharacterCount; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            string characterId = partyStore.GetCharacterId(i);
            bool hasCharacter = !string.IsNullOrWhiteSpace(characterId);

            if (view.Root != null)
                view.Root.gameObject.SetActive(hasCharacter);

            if (!hasCharacter)
            {
                ClearCharacterView(view);
                continue;
            }

            CharacterMasterData master = null;
            dataManager.CharacterDatabase?.TryGet(characterId, out master);

            CharacterRuntimeData runtime = null;
            characterStore.TryGet(characterId, out runtime);

            RefreshCharacterIdentity(view, characterId, master);
            RefreshCharacterActiveCompound(view, runtime);
            RefreshCharacterRelics(view, runtime);
            RefreshCharacterSkills(view, runtime);
        }

        UpdateRelicEquipCandidateVisuals();
        UpdateActiveCompoundCandidateVisuals();
        UpdateCharacterEquipTargetVisuals();
    }

    public static void RefreshAllCharacterData()
    {
        LobbyEquipPanelUI[] panels = FindObjectsByType<LobbyEquipPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].RefreshCharacterData();
        }
    }

    private IEnumerator PlaySlideAnimation(bool opening)
    {
        ResolveSlideTargets();

        float equipFromX = equipRect != null ? equipRect.anchoredPosition.x : (opening ? equipStartX : equipEndX);
        float charterFromX = charterRect != null ? charterRect.anchoredPosition.x : (opening ? charterStartX : charterEndX);
        float equipToX = opening ? equipEndX : equipStartX;
        float charterToX = opening ? charterEndX : charterStartX;

        if (slideDuration <= 0f)
        {
            SetAnchoredX(equipRect, equipToX);
            SetAnchoredX(charterRect, charterToX);
            FinishSlide(opening);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / slideDuration);
            float curveValue = slideCurve != null ? slideCurve.Evaluate(normalized) : normalized;

            SetAnchoredX(equipRect, Mathf.LerpUnclamped(equipFromX, equipToX, curveValue));
            SetAnchoredX(charterRect, Mathf.LerpUnclamped(charterFromX, charterToX, curveValue));
            yield return null;
        }

        SetAnchoredX(equipRect, equipToX);
        SetAnchoredX(charterRect, charterToX);
        FinishSlide(opening);
    }

    private void FinishSlide(bool opening)
    {
        slideAnimationCoroutine = null;

        if (opening)
        {
            isOpen = true;
            isClosing = false;
            return;
        }

        FinishClose();
    }

    private void FinishClose()
    {
        slideAnimationCoroutine = null;
        isOpen = false;
        isClosing = false;
        LobbyPositionModalInputBlocker.Unblock(this);
        LobbyPositionSharedModalBackground.HideAfterReadyPanel();
        Closed?.Invoke();

        // Equip_panel 자체는 비활성화하지 않습니다.
        // 닫힘 상태는 Equip=-1350, Charter=1350 위치로만 표현합니다.
    }

    private void RefreshOwnedRelicData()
    {
        if (ownedRelicView == null)
            return;

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        string relicId = GetLatestOwnedRelicId(lobby);

        if (string.IsNullOrWhiteSpace(relicId) ||
            DataManager.Instance?.RelicDatabase == null ||
            !DataManager.Instance.RelicDatabase.TryGet(relicId, out RelicData relic))
        {
            ClearOwnedRelicView();
            return;
        }

        ApplyImage(ownedRelicView.IconImage, ResolveRelicIcon(relicId));

        if (ownedRelicView.NameText != null)
            ownedRelicView.NameText.text = GameDataLocalization.RelicName(relic);

        if (ownedRelicView.RarityText != null)
        {
            ownedRelicView.RarityText.text = FormatRelicRarityLabel(relic.Rarity);
            ownedRelicView.RarityText.color = ResolveRecordRarityColor(relic.Rarity);
        }

        if (ownedRelicView.EffectText != null)
            ownedRelicView.EffectText.text = FormatRelicEffectDescription(relic);

        if (ownedRelicView.EquipButton != null)
            ownedRelicView.EquipButton.interactable = true;

        if (isOwnedRelicSelected &&
            !string.Equals(selectedOwnedRelicId, relicId, StringComparison.Ordinal))
        {
            ResetOwnedRelicSelection();
        }
    }

    private void ToggleOwnedRelicSelection()
    {
        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        string relicId = GetLatestOwnedRelicId(lobby);

        if (string.IsNullOrWhiteSpace(relicId))
        {
            ResetOwnedRelicSelection();
            return;
        }

        if (isOwnedRelicSelected &&
            string.Equals(selectedOwnedRelicId, relicId, StringComparison.Ordinal))
        {
            ResetOwnedRelicSelection();
            return;
        }

        // 유물 장착 모드로 전환할 때 연성제 장착 모드만 해제합니다.
        // 선택된 연성제의 이름/아이콘 정보는 그대로 유지합니다.
        SetCompoundEquipSelectionActive(false);

        isOwnedRelicSelected = true;
        selectedOwnedRelicId = relicId;
        SetOwnedRelicLineActive(true);
        UpdateRelicEquipCandidateVisuals();
        UpdateCharacterEquipTargetVisuals();
        UpdateEquipButtonTexts();
    }

    private void ResetOwnedRelicSelection()
    {
        isOwnedRelicSelected = false;
        selectedOwnedRelicId = null;
        SetOwnedRelicLineActive(false);
        UpdateRelicEquipCandidateVisuals();
        UpdateCharacterEquipTargetVisuals();
        UpdateEquipButtonTexts();
    }

    private void UpdateEquipButtonTexts()
    {
        if (compoundSelectionView?.EquipButtonText != null)
        {
            compoundSelectionView.EquipButtonText.text = isCompoundEquipSelectionActive
                ? EquipButtonCancelText
                : EquipButtonDefaultText;
        }

        if (ownedRelicView?.EquipButtonText != null)
        {
            ownedRelicView.EquipButtonText.text = isOwnedRelicSelected
                ? EquipButtonCancelText
                : EquipButtonDefaultText;
        }
    }

    private void SetOwnedRelicLineActive(bool active)
    {
        if (ownedRelicView?.LineObject != null)
            ownedRelicView.LineObject.SetActive(active);
    }

    private void UpdateRelicEquipCandidateVisuals()
    {
        DataManager dataManager = DataManager.Instance;

        for (int i = 0; i < CharacterCount; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            string characterId = dataManager?.PartyRuntimeStore?.GetCharacterId(i);
            CharacterRuntimeData runtime = null;
            if (!string.IsNullOrWhiteSpace(characterId))
                dataManager?.CharacterRuntimeStore?.TryGet(characterId, out runtime);

            for (int slot = 0; slot < VisibleRelicSlotCount; slot++)
            {
                RelicSlotView slotView = view.RelicSlots[slot];
                if (slotView == null)
                    continue;

                int runtimeSlotIndex = slot + 1;
                string equippedRelicId = runtime?.EquippedRelicIds != null &&
                                         runtimeSlotIndex < runtime.EquippedRelicIds.Length
                    ? runtime.EquippedRelicIds[runtimeSlotIndex]
                    : null;

                bool hasEquippedRelic = !string.IsNullOrWhiteSpace(equippedRelicId);
                ApplyImage(slotView.IconImage, ResolveRelicIcon(equippedRelicId));

                if (slotView.IconImage != null)
                    slotView.IconImage.gameObject.SetActive(hasEquippedRelic);

                // 장착 대상은 이제 개별 유물 슬롯이 아니라 캐릭터 Back 전체입니다.
                slotView.IsCandidate = false;
                if (slotView.Button != null)
                    slotView.Button.interactable = false;
            }
        }
    }

    private static int FirstEmptyRelicRuntimeSlotIndex(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return -1;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);

        int max = Mathf.Min(VisibleRelicSlotCount, runtime.EquippedRelicIds.Length - 1);
        for (int visibleIndex = 0; visibleIndex < max; visibleIndex++)
        {
            int runtimeSlotIndex = visibleIndex + 1;
            if (string.IsNullOrWhiteSpace(runtime.EquippedRelicIds[runtimeSlotIndex]))
                return runtimeSlotIndex;
        }

        return -1;
    }

    private void TryEquipSelectedOwnedRelic(int partySlotIndex, int visibleRelicSlotIndex)
    {
        if (!isOwnedRelicSelected || string.IsNullOrWhiteSpace(selectedOwnedRelicId))
            return;

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null)
            return;

        string characterId = dataManager.PartyRuntimeStore?.GetCharacterId(partySlotIndex);
        if (string.IsNullOrWhiteSpace(characterId) ||
            !dataManager.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtime))
        {
            return;
        }

        int expectedRuntimeSlot = FirstEmptyRelicRuntimeSlotIndex(runtime);
        int clickedRuntimeSlot = visibleRelicSlotIndex + 1;
        if (expectedRuntimeSlot < 0 || clickedRuntimeSlot != expectedRuntimeSlot)
            return;

        LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
        if (lobby?.OwnedRelicIds == null)
            return;

        var service = new RelicEquipService(
            dataManager.CharacterRuntimeStore,
            lobby.OwnedRelicIds,
            dataManager.RelicDatabase);

        if (!service.EquipRelic(characterId, clickedRuntimeSlot, selectedOwnedRelicId))
            return;

        ResetOwnedRelicSelection();
        RefreshCharacterData();
        RelicEquipPanelUI.RefreshAll();
    }

    private void TryEquipSelectedOwnedRelicToCharacter(int partySlotIndex)
    {
        if (!isOwnedRelicSelected || string.IsNullOrWhiteSpace(selectedOwnedRelicId))
            return;

        DataManager dataManager = DataManager.Instance;
        string characterId = dataManager?.PartyRuntimeStore?.GetCharacterId(partySlotIndex);
        if (string.IsNullOrWhiteSpace(characterId) ||
            dataManager?.CharacterRuntimeStore == null ||
            !dataManager.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtime))
        {
            return;
        }

        int runtimeSlotIndex = FirstEmptyRelicRuntimeSlotIndex(runtime);
        if (runtimeSlotIndex <= 0)
            return;

        TryEquipSelectedOwnedRelic(partySlotIndex, runtimeSlotIndex - 1);
    }

    private static string GetLatestOwnedRelicId(LobbyRuntimeData lobby)
    {
        if (lobby?.OwnedRelicIds == null)
            return null;

        for (int i = lobby.OwnedRelicIds.Count - 1; i >= 0; i--)
        {
            string relicId = lobby.OwnedRelicIds[i]?.Trim();
            if (!string.IsNullOrWhiteSpace(relicId))
                return relicId;
        }

        return null;
    }

    private void ClearOwnedRelicView()
    {
        if (ownedRelicView == null)
            return;

        isOwnedRelicSelected = false;
        selectedOwnedRelicId = null;
        SetOwnedRelicLineActive(false);
        ApplyImage(ownedRelicView.IconImage, null);

        if (ownedRelicView.NameText != null)
            ownedRelicView.NameText.text = string.Empty;

        if (ownedRelicView.RarityText != null)
        {
            ownedRelicView.RarityText.text = string.Empty;
            ownedRelicView.RarityText.color = Color.white;
        }

        if (ownedRelicView.EffectText != null)
            ownedRelicView.EffectText.text = string.Empty;

        if (ownedRelicView.EquipButton != null)
            ownedRelicView.EquipButton.interactable = false;

        UpdateCharacterEquipTargetVisuals();
    }


    private static string FormatRelicRarityLabel(string rarity)
    {
        string normalized = string.IsNullOrWhiteSpace(rarity) ? string.Empty : rarity.Trim();

        if (string.Equals(normalized, "Common", StringComparison.OrdinalIgnoreCase)) return "일반 유물";
        if (string.Equals(normalized, "Rare", StringComparison.OrdinalIgnoreCase)) return "레어 유물";
        if (string.Equals(normalized, "Epic", StringComparison.OrdinalIgnoreCase)) return "에픽 유물";
        if (string.Equals(normalized, "Unique", StringComparison.OrdinalIgnoreCase)) return "유니크 유물";

        return normalized;
    }

    private Color ResolveRecordRarityColor(string rarity)
    {
        if (recordPanelUI == null)
        {
            RecordPanelUI[] panels = FindObjectsByType<RecordPanelUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (panels.Length > 0)
                recordPanelUI = panels[0];
        }

        return recordPanelUI != null
            ? recordPanelUI.GetRarityDisplayColor(rarity)
            : Color.white;
    }

    private void RefreshCharacterIdentity(CharacterView view, string characterId, CharacterMasterData master)
    {
        if (view.NameText != null)
        {
            string displayName = master != null
                ? GameDataLocalization.CharacterName(master)
                : characterId;

            view.NameText.text = string.IsNullOrWhiteSpace(displayName) ? characterId : displayName;
        }

        Sprite mark1 = null;
        Sprite mark2 = null;

        CharacterIconDatabase iconDatabase = DataManager.Instance?.CharacterIconDatabase;
        if (iconDatabase != null)
        {
            iconDatabase.TryGetMark(characterId, out mark1);
            iconDatabase.TryGetMark2(characterId, out mark2);
        }

        ApplyImage(view.Mark1Image, mark1);
        ApplyImage(view.Mark2Image, mark2);
    }

    private void RefreshCharacterActiveCompound(CharacterView view, CharacterRuntimeData runtime)
    {
        string compoundId = ActiveRelicRuntimeUtility.GetActiveRelicId(runtime);
        Sprite icon = ResolveRelicIcon(compoundId);
        ApplyImage(view.ActiveCompoundIcon, icon);
    }

    private void RefreshCharacterRelics(CharacterView view, CharacterRuntimeData runtime)
    {
        if (runtime != null)
            ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);

        for (int i = 0; i < VisibleRelicSlotCount; i++)
        {
            int runtimeRelicIndex = i + 1; // 0번은 Active 연성제 슬롯입니다.
            string relicId = runtime?.EquippedRelicIds != null && runtimeRelicIndex < runtime.EquippedRelicIds.Length
                ? runtime.EquippedRelicIds[runtimeRelicIndex]
                : null;

            RelicSlotView slotView = view.RelicSlots[i];
            if (slotView == null)
                continue;

            bool hasEquippedRelic = !string.IsNullOrWhiteSpace(relicId);
            ApplyImage(slotView.IconImage, ResolveRelicIcon(relicId));

            if (slotView.NumberText != null)
                slotView.NumberText.gameObject.SetActive(!hasEquippedRelic);
        }
    }

    private void RefreshCharacterSkills(CharacterView view, CharacterRuntimeData runtime)
    {
        for (int i = 0; i < VisibleSkillSlotCount; i++)
        {
            int runtimeIndex = RuntimeSkillSlotIndices[i];
            string skillId = GetEquippedSkillId(runtime, runtimeIndex);

            Sprite icon = null;
            if (!string.IsNullOrWhiteSpace(skillId) && DataManager.Instance?.SkillIconDatabase != null)
                DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out icon);

            ApplyImage(view.SkillIcons[i], icon, Color.white);
            SkillUpgradeMarkStyle.ApplyShared(view.SkillIcons[i], skillId);
        }
    }

    private static string GetEquippedSkillId(CharacterRuntimeData runtime, int runtimeIndex)
    {
        if (runtime == null)
            return null;

        if (runtimeIndex == 1 && !string.IsNullOrWhiteSpace(runtime.AbilitySkillId))
            return runtime.AbilitySkillId;

        if (runtime.EquippedSkillIds == null ||
            runtimeIndex < 0 ||
            runtimeIndex >= runtime.EquippedSkillIds.Length)
        {
            return null;
        }

        return runtime.EquippedSkillIds[runtimeIndex];
    }

    private static Sprite ResolveRelicIcon(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId) || DataManager.Instance?.RelicIconDatabase == null)
            return null;

        DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon);
        return icon;
    }

    private void ClearCharacterViews()
    {
        for (int i = 0; i < characterViews.Length; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            ClearCharacterView(view);
        }
    }

    private static void ClearCharacterView(CharacterView view)
    {
        if (view == null)
            return;

        if (view.NameText != null)
            view.NameText.text = string.Empty;

        ApplyImage(view.Mark1Image, null);
        ApplyImage(view.Mark2Image, null);
        view.IsCharacterEquipTarget = false;
        RestoreCharacterSelectColor(view);
        ApplyImage(view.ActiveCompoundIcon, null);
        if (view.ActiveCompoundButton != null)
            view.ActiveCompoundButton.interactable = false;

        for (int i = 0; i < view.RelicSlots.Length; i++)
        {
            RelicSlotView slotView = view.RelicSlots[i];
            if (slotView == null)
                continue;

            ApplyImage(slotView.IconImage, null);
            if (slotView.NumberText != null)
                slotView.NumberText.gameObject.SetActive(true);
            if (slotView.Button != null)
                slotView.Button.interactable = false;
        }

        for (int i = 0; i < view.SkillIcons.Length; i++)
        {
            ApplyImage(view.SkillIcons[i], null);
            SkillUpgradeMarkStyle.ApplyShared(view.SkillIcons[i], (string)null);
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

    private bool IsPointerInsideOpenArea(Vector2 screenPosition)
    {
        if (ContainsScreenPoint(equipRect, screenPosition))
            return true;

        if (ContainsScreenPoint(charterRect, screenPosition))
            return true;

        if (ContainsScreenPoint(toggleButtonRect, screenPosition))
            return true;

        return false;
    }

    private static bool ContainsScreenPoint(RectTransform rect, Vector2 screenPosition)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera eventCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }

    private void StopSlideAnimation()
    {
        if (slideAnimationCoroutine == null)
            return;

        StopCoroutine(slideAnimationCoroutine);
        slideAnimationCoroutine = null;
    }

    private void ResetSlidePositions()
    {
        ResolveSlideTargets();
        SetAnchoredX(equipRect, equipStartX);
        SetAnchoredX(charterRect, charterStartX);
    }

    private void ResolveSlideTargets()
    {
        // 새 로비 구조에서는 Info_Panel과 Ready_Panel이 PositionPanel의 형제 오브젝트입니다.
        // 둘 다 항상 활성 상태를 유지하고 X 위치만 이동합니다.
        if (equipRect == null || equipRect.gameObject.name != InfoPanelName)
        {
            GameObject infoPanel = FindSceneObject(InfoPanelName);
            equipRect = infoPanel != null ? infoPanel.transform as RectTransform : null;
        }

        if (charterRect == null || charterRect.gameObject.name != ReadyPanelName)
        {
            GameObject readyPanel = FindSceneObject(ReadyPanelName);
            charterRect = readyPanel != null ? readyPanel.transform as RectTransform : null;
        }
    }

    private void EnsurePreparationPanelsActive()
    {
        ResolveSlideTargets();

        if (equipRect != null && !equipRect.gameObject.activeSelf)
            equipRect.gameObject.SetActive(true);

        if (charterRect != null && !charterRect.gameObject.activeSelf)
            charterRect.gameObject.SetActive(true);

        LobbyInfoPanelUI.RefreshAll();
    }

    private bool AreSlideTargetsAtOpenPosition()
    {
        const float tolerance = 0.5f;

        bool infoOpen = equipRect == null || Mathf.Abs(equipRect.anchoredPosition.x - equipEndX) <= tolerance;
        bool readyOpen = charterRect == null || Mathf.Abs(charterRect.anchoredPosition.x - charterEndX) <= tolerance;
        return infoOpen && readyOpen;
    }

    private void ResolveReadyInventoryDisplayIfNeeded()
    {
        ResolveSlideTargets();
        Transform readyRoot = charterRect != null ? charterRect : ResolvePanelRoot()?.transform;
        if (readyRoot == null)
            return;

        if (readyRelicContentRoot == null)
        {
            Transform relicRoot = readyRoot.Find("Relic");
            Transform viewport = relicRoot != null ? relicRoot.Find("Viewport") : null;
            Transform content = viewport != null ? viewport.Find("Content") : null;
            if (content != null)
                readyRelicContentRoot = content;
        }

        if (readyCompoundContentRoot == null)
        {
            Transform compoundRoot = readyRoot.Find("Compound");
            Transform viewport = compoundRoot != null ? compoundRoot.Find("Viewport") : null;
            Transform content = viewport != null ? viewport.Find("Content") : null;
            if (content != null)
                readyCompoundContentRoot = content;
        }

        if (readyInventorySlotPrefab == null)
            readyInventorySlotPrefab = compoundSlotPrefab;

        RegisterExistingReadySlots(readyRelicContentRoot, readyRelicSlots);
        RegisterExistingReadySlots(readyCompoundContentRoot, readyCompoundSlots);
    }

    private void ResolveReadyInventoryDetailIfNeeded()
    {
        ResolveSlideTargets();
        Transform readyRoot = charterRect != null ? charterRect : ResolvePanelRoot()?.transform;
        if (readyRoot == null)
            return;

        if (readyDetailRoot == null)
        {
            Transform detail = readyRoot.Find("Detail");
            if (detail != null)
                readyDetailRoot = detail.gameObject;
        }

        if (readyDetailRoot == null)
            return;

        Transform detailRoot = readyDetailRoot.transform;
        if (readyDetailIconImage == null)
            readyDetailIconImage = FindImageByNames(detailRoot, "Icon");
        if (readyDetailNameText == null)
            readyDetailNameText = FindTextByNames(detailRoot, "Name");
        if (readyDetailEffectText == null)
            readyDetailEffectText = FindTextByNames(detailRoot, "Effect");

        // Detail의 Name/Effect는 호버 중 원본 DB 데이터로 직접 갱신되는 동적 텍스트입니다.
        // 정적 로컬라이즈 컴포넌트가 Detail 활성화 시 Inspector 기본값(예: "아이템")으로
        // 다시 덮어쓰지 못하도록 동적 출력 대상으로 보호합니다.
        ProtectReadyDetailText(readyDetailNameText);
        ProtectReadyDetailText(readyDetailEffectText);

        if (!readyDetailInitialized)
        {
            readyDetailInitialized = true;
            readyDetailRoot.SetActive(false);
        }
    }


    private static void ProtectReadyDetailText(TMP_Text text)
    {
        if (text == null)
            return;

        GameObject target = text.gameObject;
        if (target.GetComponent<LocalizationIgnore>() == null)
            target.AddComponent<LocalizationIgnore>();

        LocalizedTMPText localizedTmp = target.GetComponent<LocalizedTMPText>();
        if (localizedTmp != null)
            localizedTmp.enabled = false;

        LocalizeStringEvent localizeStringEvent = target.GetComponent<LocalizeStringEvent>();
        if (localizeStringEvent != null)
            localizeStringEvent.enabled = false;
    }

    private void RefreshReadyInventoryDisplay()
    {
        ResolveReadyInventoryDisplayIfNeeded();
        BattleBagItemSlotUI prefab = readyInventorySlotPrefab != null ? readyInventorySlotPrefab : compoundSlotPrefab;
        if (prefab == null)
            return;

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();

        List<string> relicIds = new();
        if (lobby?.OwnedRelicIds != null)
        {
            HashSet<string> seenRelics = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < lobby.OwnedRelicIds.Count; i++)
            {
                string relicId = lobby.OwnedRelicIds[i]?.Trim();
                if (string.IsNullOrWhiteSpace(relicId) || !seenRelics.Add(relicId))
                    continue;

                relicIds.Add(relicId);
            }
        }

        IReadOnlyList<string> storedCompoundIds = lobby?.StoredCompoundIds;
        List<BagItemStack> compoundStacks = BagItemStackUtility.BuildStacks(storedCompoundIds);

        RefreshReadyRelicSlots(prefab, relicIds);
        RefreshReadyCompoundSlots(prefab, compoundStacks);
    }

    private void RefreshReadyRelicSlots(BattleBagItemSlotUI prefab, List<string> relicIds)
    {
        if (readyRelicContentRoot == null || prefab == null)
            return;

        int dataCount = relicIds != null ? relicIds.Count : 0;
        int targetCount = Mathf.Max(Mathf.Max(1, readyRelicMinimumSlotCount), dataCount);
        EnsureReadySlotCount(readyRelicContentRoot, readyRelicSlots, prefab, targetCount, "ReadyRelicSlot");

        for (int i = 0; i < readyRelicSlots.Count; i++)
        {
            BattleBagItemSlotUI slot = readyRelicSlots[i];
            if (slot == null)
                continue;

            bool visible = i < targetCount;
            slot.gameObject.SetActive(visible);
            if (!visible)
                continue;

            ApplyReadySlotScale(slot);
            slot.SetQuantityVisible(false);

            if (i < dataCount)
            {
                slot.Setup(relicIds[i], 1, ShowReadyInventoryDetail, HideReadyInventoryDetail, PinReadyInventoryDetail);
                ConfigureReadyInventoryDrag(slot, isCompound: false);
            }
            else
            {
                slot.Clear(null, null, null);
                ConfigureReadyInventoryDrag(slot, isCompound: false);
            }

            slot.SetQuantityVisible(false);
            slot.SetSelected(false);
            slot.SetHovered(false);
        }
    }

    private void RefreshReadyCompoundSlots(BattleBagItemSlotUI prefab, List<BagItemStack> stacks)
    {
        if (readyCompoundContentRoot == null || prefab == null)
            return;

        int dataCount = stacks != null ? stacks.Count : 0;
        int targetCount = Mathf.Max(Mathf.Max(1, readyCompoundMinimumSlotCount), dataCount);
        EnsureReadySlotCount(readyCompoundContentRoot, readyCompoundSlots, prefab, targetCount, "ReadyCompoundSlot");

        for (int i = 0; i < readyCompoundSlots.Count; i++)
        {
            BattleBagItemSlotUI slot = readyCompoundSlots[i];
            if (slot == null)
                continue;

            bool visible = i < targetCount;
            slot.gameObject.SetActive(visible);
            if (!visible)
                continue;

            ApplyReadySlotScale(slot);
            slot.SetQuantityVisible(true);

            if (i < dataCount)
            {
                BagItemStack stack = stacks[i];
                slot.Setup(stack.ItemId, stack.Count, ShowReadyInventoryDetail, HideReadyInventoryDetail, PinReadyInventoryDetail);
                ConfigureReadyInventoryDrag(slot, isCompound: true);
            }
            else
            {
                slot.Clear(null, null, null);
                ConfigureReadyInventoryDrag(slot, isCompound: true);
            }

            slot.SetQuantityVisible(true);
            slot.SetSelected(false);
            slot.SetHovered(false);
        }
    }

    private void ShowReadyInventoryDetail(BattleBagItemSlotUI slot)
    {
        if (slot == null || !slot.HasItem || string.IsNullOrWhiteSpace(slot.ItemId))
            return;

        ResolveReadyInventoryDetailIfNeeded();
        if (readyDetailRoot == null || DataManager.Instance == null)
            return;

        string itemId = slot.ItemId.Trim();
        Sprite icon = null;
        string displayName = string.Empty;
        string effectText = string.Empty;
        bool resolved = false;

        if (DataManager.Instance.CompoundDatabase != null &&
            DataManager.Instance.CompoundDatabase.TryGet(itemId, out CompoundData compound))
        {
            displayName = GameDataLocalization.CompoundName(compound);
            effectText = GameDataLocalization.CompoundDescription(compound);
            resolved = true;
        }
        else if (DataManager.Instance.RelicDatabase != null &&
                 DataManager.Instance.RelicDatabase.TryGet(itemId, out RelicData relic))
        {
            displayName = GameDataLocalization.RelicName(relic);
            effectText = GameDataLocalization.RelicEffectDescription(relic);
            resolved = true;
        }

        if (!resolved)
        {
            HideReadyInventoryDetail();
            return;
        }

        if (DataManager.Instance.RelicIconDatabase != null)
            DataManager.Instance.RelicIconDatabase.TryGetIcon(itemId, out icon);

        readyDetailSourceSlot = slot;

        if (readyDetailIconImage != null)
        {
            readyDetailIconImage.sprite = icon;
            readyDetailIconImage.enabled = icon != null;
        }

        if (readyDetailNameText != null)
            readyDetailNameText.text = displayName;

        if (readyDetailEffectText != null)
            readyDetailEffectText.text = effectText;

        readyDetailRoot.SetActive(true);
    }

    private void HideReadyInventoryDetail(BattleBagItemSlotUI slot)
    {
        if (readyDetailSourceSlot != null && slot != null && readyDetailSourceSlot != slot)
            return;

        if (readyDetailPinnedSlot != null)
        {
            ShowReadyInventoryDetail(readyDetailPinnedSlot);
            return;
        }

        HideReadyInventoryDetail();
    }

    private void PinReadyInventoryDetail(BattleBagItemSlotUI slot)
    {
        if (slot == null || !slot.HasItem)
            return;

        if (readyDetailPinnedSlot != null && readyDetailPinnedSlot != slot)
            readyDetailPinnedSlot.SetSelected(false);

        readyDetailPinnedSlot = slot;
        readyDetailPinnedSlot.SetSelected(true);
        ShowReadyInventoryDetail(slot);
    }

    private void HideReadyInventoryDetail()
    {
        readyDetailSourceSlot = null;

        if (readyDetailPinnedSlot != null)
            readyDetailPinnedSlot.SetSelected(false);

        readyDetailPinnedSlot = null;

        if (readyDetailRoot != null)
            readyDetailRoot.SetActive(false);
    }


    private void ConfigureReadyInventoryDrag(BattleBagItemSlotUI slot, bool isCompound)
    {
        if (slot == null)
            return;

        LobbyReadyEquipmentPointerRelay relay = slot.GetComponent<LobbyReadyEquipmentPointerRelay>();
        if (relay == null)
            relay = slot.gameObject.AddComponent<LobbyReadyEquipmentPointerRelay>();

        relay.Configure(
            null,
            data => BeginReadyInventoryDrag(slot, isCompound, data),
            UpdateEquipmentDrag,
            EndEquipmentDrag,
            null);
    }

    private void BindInfoPanelEquipmentTargets()
    {
        ResolveSlideTargets();
        Transform infoRoot = equipRect;
        if (infoRoot == null)
            return;

        for (int partyIndex = 0; partyIndex < CharacterCount; partyIndex++)
        {
            Transform charRoot = FindChildRecursive(infoRoot, "Char" + (partyIndex + 1));
            if (charRoot == null)
                continue;

            Transform relicRoot = charRoot.Find("Relic") ?? FindChildRecursive(charRoot, "Relic");
            for (int visibleIndex = 0; visibleIndex < 2; visibleIndex++)
            {
                Transform slotRoot = relicRoot != null
                    ? relicRoot.Find("Relic" + (visibleIndex + 1).ToString("00"))
                    : null;
                if (slotRoot == null)
                    continue;

                int capturedParty = partyIndex;
                int capturedVisible = visibleIndex;
                int runtimeSlotIndex = visibleIndex + 1;
                LobbyReadyEquipmentPointerRelay relay = GetOrAddEquipmentRelay(slotRoot.gameObject);
                relay.Configure(
                    _ => OnInfoEquipmentSlotClicked(capturedParty, runtimeSlotIndex, isCompound: false),
                    data => BeginInfoEquipmentDrag(capturedParty, runtimeSlotIndex, isCompound: false, data),
                    UpdateEquipmentDrag,
                    EndEquipmentDrag,
                    _ => CompleteEquipmentDrop(capturedParty, runtimeSlotIndex, isCompound: false));
                infoRelicTargetRelays[partyIndex, visibleIndex] = relay;
            }

            Transform compoundRoot = charRoot.Find("Compound") ?? FindChildRecursive(charRoot, "Compound");
            Transform compoundSlot = compoundRoot != null ? compoundRoot.Find("Compound01") : null;
            if (compoundSlot != null)
            {
                int capturedParty = partyIndex;
                int activeSlotIndex = ActiveRelicRuntimeUtility.ActiveRelicSlotIndex;
                LobbyReadyEquipmentPointerRelay relay = GetOrAddEquipmentRelay(compoundSlot.gameObject);
                relay.Configure(
                    _ => OnInfoEquipmentSlotClicked(capturedParty, activeSlotIndex, isCompound: true),
                    data => BeginInfoEquipmentDrag(capturedParty, activeSlotIndex, isCompound: true, data),
                    UpdateEquipmentDrag,
                    EndEquipmentDrag,
                    _ => CompleteEquipmentDrop(capturedParty, activeSlotIndex, isCompound: true));
                infoCompoundTargetRelays[partyIndex] = relay;
            }
        }
    }

    private static LobbyReadyEquipmentPointerRelay GetOrAddEquipmentRelay(GameObject target)
    {
        if (target == null)
            return null;

        LobbyReadyEquipmentPointerRelay relay = target.GetComponent<LobbyReadyEquipmentPointerRelay>();
        if (relay == null)
            relay = target.AddComponent<LobbyReadyEquipmentPointerRelay>();
        return relay;
    }

    private void OnInfoEquipmentSlotClicked(int partyIndex, int runtimeSlotIndex, bool isCompound)
    {
        if (readyDetailPinnedSlot != null && readyDetailPinnedSlot.HasItem)
        {
            TryEquipReadySelectionToInfoSlot(partyIndex, runtimeSlotIndex, isCompound);
            return;
        }

        TryUnequipInfoSlot(partyIndex, runtimeSlotIndex, isCompound);
    }

    private bool TryEquipReadySelectionToInfoSlot(int partyIndex, int runtimeSlotIndex, bool isCompound)
    {
        BattleBagItemSlotUI selected = readyDetailPinnedSlot;
        if (selected == null || !selected.HasItem || string.IsNullOrWhiteSpace(selected.ItemId))
            return false;

        string itemId = selected.ItemId.Trim();
        if (!IsEquipmentType(itemId, isCompound))
            return false;

        bool changed = isCompound
            ? EquipStoredCompoundToInfoSlot(partyIndex, itemId)
            : EquipOwnedRelicToInfoSlot(partyIndex, runtimeSlotIndex, itemId);

        if (changed)
            RefreshAfterEquipmentChange();

        return changed;
    }

    private bool EquipOwnedRelicToInfoSlot(int partyIndex, int runtimeSlotIndex, string relicId)
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CharacterRuntimeStore == null || dataManager.RelicDatabase == null)
            return false;

        string characterId = dataManager.PartyRuntimeStore?.GetCharacterId(partyIndex);
        LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
        if (string.IsNullOrWhiteSpace(characterId) || lobby?.OwnedRelicIds == null)
            return false;

        RelicEquipService service = new RelicEquipService(
            dataManager.CharacterRuntimeStore,
            lobby.OwnedRelicIds,
            dataManager.RelicDatabase);
        return service.EquipRelic(characterId, runtimeSlotIndex, relicId);
    }

    private bool EquipStoredCompoundToInfoSlot(int partyIndex, string compoundId)
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CompoundDatabase == null || dataManager.CharacterRuntimeStore == null)
            return false;

        string characterId = dataManager.PartyRuntimeStore?.GetCharacterId(partyIndex);
        if (string.IsNullOrWhiteSpace(characterId) ||
            !dataManager.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtime) ||
            !dataManager.CompoundDatabase.TryGet(compoundId, out CompoundData compound))
        {
            return false;
        }

        LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
        if (lobby?.StoredCompoundIds == null)
            return false;

        int storedIndex = FindStoredCompoundIndex(lobby.StoredCompoundIds, compoundId);
        if (storedIndex < 0)
            return false;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        int activeSlot = ActiveRelicRuntimeUtility.ActiveRelicSlotIndex;
        string previous = runtime.EquippedRelicIds[activeSlot];

        lobby.StoredCompoundIds.RemoveAt(storedIndex);
        if (!string.IsNullOrWhiteSpace(previous))
            lobby.StoredCompoundIds.Add(previous.Trim());

        runtime.EquippedRelicIds[activeSlot] = compoundId.Trim();
        ActiveRelicRuntimeUtility.ResetUses(runtime, compound);
        return true;
    }

    private bool TryUnequipInfoSlot(int partyIndex, int runtimeSlotIndex, bool isCompound)
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CharacterRuntimeStore == null)
            return false;

        string characterId = dataManager.PartyRuntimeStore?.GetCharacterId(partyIndex);
        if (string.IsNullOrWhiteSpace(characterId) ||
            !dataManager.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtime))
        {
            return false;
        }

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        if (runtimeSlotIndex < 0 || runtimeSlotIndex >= runtime.EquippedRelicIds.Length)
            return false;

        string itemId = runtime.EquippedRelicIds[runtimeSlotIndex];
        if (string.IsNullOrWhiteSpace(itemId) || !IsEquipmentType(itemId, isCompound))
            return false;

        bool changed;
        if (isCompound)
        {
            LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
            if (lobby == null)
                return false;

            lobby.StoredCompoundIds ??= new List<string>();
            lobby.StoredCompoundIds.Add(itemId.Trim());
            runtime.EquippedRelicIds[runtimeSlotIndex] = null;
            changed = true;
        }
        else
        {
            LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
            if (lobby?.OwnedRelicIds == null || dataManager.RelicDatabase == null)
                return false;

            RelicEquipService service = new RelicEquipService(
                dataManager.CharacterRuntimeStore,
                lobby.OwnedRelicIds,
                dataManager.RelicDatabase);
            changed = service.UnequipRelic(characterId, runtimeSlotIndex);
        }

        if (changed)
            RefreshAfterEquipmentChange();

        return changed;
    }

    private bool IsEquipmentType(string itemId, bool isCompound)
    {
        if (string.IsNullOrWhiteSpace(itemId) || DataManager.Instance == null)
            return false;

        if (isCompound)
            return DataManager.Instance.CompoundDatabase != null &&
                   DataManager.Instance.CompoundDatabase.TryGet(itemId.Trim(), out _);

        return DataManager.Instance.RelicDatabase != null &&
               DataManager.Instance.RelicDatabase.TryGet(itemId.Trim(), out RelicData relic) &&
               !ActiveRelicEffectResolver.IsActiveRelic(relic);
    }

    private void BeginReadyInventoryDrag(BattleBagItemSlotUI slot, bool isCompound, PointerEventData eventData)
    {
        if (slot == null || !slot.HasItem || string.IsNullOrWhiteSpace(slot.ItemId))
            return;

        // 홀드/드래그를 시작한 아이템을 클릭한 것과 동일하게 선택합니다.
        // Detail과 선택 Back 색도 드래그 중인 아이템 기준으로 즉시 이동합니다.
        PinReadyInventoryDetail(slot);

        equipmentDragItemId = slot.ItemId.Trim();
        equipmentDragIsCompound = isCompound;
        equipmentDragFromInfo = false;
        equipmentDragSourcePartyIndex = -1;
        equipmentDragSourceRuntimeSlotIndex = -1;
        equipmentDragHandled = false;
        CreateEquipmentDragGhost(equipmentDragItemId, eventData);
    }

    private void BeginInfoEquipmentDrag(int partyIndex, int runtimeSlotIndex, bool isCompound, PointerEventData eventData)
    {
        if (!TryGetEquippedItemId(partyIndex, runtimeSlotIndex, out string itemId) ||
            !IsEquipmentType(itemId, isCompound))
        {
            return;
        }

        equipmentDragItemId = itemId;
        equipmentDragIsCompound = isCompound;
        equipmentDragFromInfo = true;
        equipmentDragSourcePartyIndex = partyIndex;
        equipmentDragSourceRuntimeSlotIndex = runtimeSlotIndex;
        equipmentDragHandled = false;
        CreateEquipmentDragGhost(itemId, eventData);
    }

    private void UpdateEquipmentDrag(PointerEventData eventData)
    {
        UpdateEquipmentDragGhostPosition(eventData);
    }

    private void EndEquipmentDrag(PointerEventData eventData)
    {
        if (equipmentDragFromInfo && !equipmentDragHandled &&
            equipmentDragSourcePartyIndex >= 0 && equipmentDragSourceRuntimeSlotIndex >= 0)
        {
            TryUnequipInfoSlot(
                equipmentDragSourcePartyIndex,
                equipmentDragSourceRuntimeSlotIndex,
                equipmentDragIsCompound);
        }

        ClearEquipmentDragState();
    }

    private void CompleteEquipmentDrop(int targetPartyIndex, int targetRuntimeSlotIndex, bool isCompound)
    {
        if (string.IsNullOrWhiteSpace(equipmentDragItemId) || equipmentDragIsCompound != isCompound)
            return;

        bool changed;
        if (equipmentDragFromInfo)
        {
            changed = MoveEquippedItem(
                equipmentDragSourcePartyIndex,
                equipmentDragSourceRuntimeSlotIndex,
                targetPartyIndex,
                targetRuntimeSlotIndex,
                isCompound);
        }
        else
        {
            changed = isCompound
                ? EquipStoredCompoundToInfoSlot(targetPartyIndex, equipmentDragItemId)
                : EquipOwnedRelicToInfoSlot(targetPartyIndex, targetRuntimeSlotIndex, equipmentDragItemId);
        }

        if (!changed)
            return;

        equipmentDragHandled = true;
        RefreshAfterEquipmentChange();
    }

    private bool MoveEquippedItem(
        int sourcePartyIndex,
        int sourceRuntimeSlotIndex,
        int targetPartyIndex,
        int targetRuntimeSlotIndex,
        bool isCompound)
    {
        if (sourcePartyIndex == targetPartyIndex && sourceRuntimeSlotIndex == targetRuntimeSlotIndex)
        {
            equipmentDragHandled = true;
            return true;
        }

        if (!TryGetCharacterRuntime(sourcePartyIndex, out CharacterRuntimeData sourceRuntime) ||
            !TryGetCharacterRuntime(targetPartyIndex, out CharacterRuntimeData targetRuntime))
        {
            return false;
        }

        ActiveRelicRuntimeUtility.EnsureRelicSlots(sourceRuntime);
        ActiveRelicRuntimeUtility.EnsureRelicSlots(targetRuntime);

        string sourceItem = sourceRuntime.EquippedRelicIds[sourceRuntimeSlotIndex];
        if (string.IsNullOrWhiteSpace(sourceItem) || !IsEquipmentType(sourceItem, isCompound))
            return false;

        string targetItem = targetRuntime.EquippedRelicIds[targetRuntimeSlotIndex];
        if (!string.IsNullOrWhiteSpace(targetItem) && !IsEquipmentType(targetItem, isCompound))
            return false;

        sourceRuntime.EquippedRelicIds[sourceRuntimeSlotIndex] = targetItem;
        targetRuntime.EquippedRelicIds[targetRuntimeSlotIndex] = sourceItem;

        if (isCompound && DataManager.Instance?.CompoundDatabase != null)
        {
            if (!string.IsNullOrWhiteSpace(sourceRuntime.EquippedRelicIds[sourceRuntimeSlotIndex]) &&
                DataManager.Instance.CompoundDatabase.TryGet(sourceRuntime.EquippedRelicIds[sourceRuntimeSlotIndex], out CompoundData sourceCompound))
            {
                ActiveRelicRuntimeUtility.ResetUses(sourceRuntime, sourceCompound);
            }

            if (DataManager.Instance.CompoundDatabase.TryGet(sourceItem, out CompoundData targetCompound))
                ActiveRelicRuntimeUtility.ResetUses(targetRuntime, targetCompound);
        }

        return true;
    }

    private bool TryGetEquippedItemId(int partyIndex, int runtimeSlotIndex, out string itemId)
    {
        itemId = null;
        if (!TryGetCharacterRuntime(partyIndex, out CharacterRuntimeData runtime))
            return false;

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        if (runtimeSlotIndex < 0 || runtimeSlotIndex >= runtime.EquippedRelicIds.Length)
            return false;

        itemId = runtime.EquippedRelicIds[runtimeSlotIndex]?.Trim();
        return !string.IsNullOrWhiteSpace(itemId);
    }

    private bool TryGetCharacterRuntime(int partyIndex, out CharacterRuntimeData runtime)
    {
        runtime = null;
        DataManager dataManager = DataManager.Instance;
        string characterId = dataManager?.PartyRuntimeStore?.GetCharacterId(partyIndex);
        return !string.IsNullOrWhiteSpace(characterId) &&
               dataManager?.CharacterRuntimeStore != null &&
               dataManager.CharacterRuntimeStore.TryGet(characterId, out runtime);
    }

    private void CreateEquipmentDragGhost(string itemId, PointerEventData eventData)
    {
        DestroyEquipmentDragGhost();

        Sprite icon = ResolveRelicIcon(itemId);
        if (icon == null)
            return;

        Canvas sourceCanvas = GetComponentInParent<Canvas>();
        if (sourceCanvas == null)
            return;

        Canvas rootCanvas = sourceCanvas.rootCanvas != null ? sourceCanvas.rootCanvas : sourceCanvas;

        equipmentDragCanvasObject = new GameObject(
            "ReadyEquipmentDragCanvas",
            typeof(RectTransform),
            typeof(Canvas));

        RectTransform dragCanvasRect = equipmentDragCanvasObject.GetComponent<RectTransform>();
        dragCanvasRect.SetParent(rootCanvas.transform, false);
        dragCanvasRect.anchorMin = Vector2.zero;
        dragCanvasRect.anchorMax = Vector2.one;
        dragCanvasRect.offsetMin = Vector2.zero;
        dragCanvasRect.offsetMax = Vector2.zero;
        dragCanvasRect.localScale = Vector3.one;
        dragCanvasRect.SetAsLastSibling();

        equipmentDragCanvas = equipmentDragCanvasObject.GetComponent<Canvas>();
        equipmentDragCanvas.overrideSorting = true;
        equipmentDragCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        equipmentDragCanvas.sortingOrder = EquipmentDragSortingOrder;
        equipmentDragCanvas.additionalShaderChannels = rootCanvas.additionalShaderChannels;

        GameObject ghost = new GameObject(
            "ReadyEquipmentDragGhost",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image));

        equipmentDragGhostRect = ghost.GetComponent<RectTransform>();
        equipmentDragGhostRect.SetParent(equipmentDragCanvas.transform, false);
        equipmentDragGhostRect.SetAsLastSibling();
        equipmentDragGhostRect.anchorMin = new Vector2(0.5f, 0.5f);
        equipmentDragGhostRect.anchorMax = new Vector2(0.5f, 0.5f);
        equipmentDragGhostRect.pivot = new Vector2(0.5f, 0.5f);
        equipmentDragGhostRect.sizeDelta = new Vector2(96f, 96f);
        equipmentDragGhostRect.localScale = Vector3.one * 1.2f;

        CanvasGroup group = ghost.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.ignoreParentGroups = true;

        equipmentDragGhostImage = ghost.GetComponent<Image>();
        equipmentDragGhostImage.sprite = icon;
        equipmentDragGhostImage.preserveAspect = true;
        equipmentDragGhostImage.raycastTarget = false;

        UpdateEquipmentDragGhostPosition(eventData);
    }

    private void UpdateEquipmentDragGhostPosition(PointerEventData eventData)
    {
        if (equipmentDragGhostRect == null || equipmentDragCanvas == null || eventData == null)
            return;

        RectTransform canvasRect = equipmentDragCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Camera uiCamera = equipmentDragCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (equipmentDragCanvas.worldCamera != null ? equipmentDragCanvas.worldCamera : eventData.pressEventCamera);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                uiCamera,
                out Vector2 localPoint))
        {
            equipmentDragGhostRect.anchoredPosition = localPoint;
        }
    }

    private void DestroyEquipmentDragGhost()
    {
        if (equipmentDragCanvasObject != null)
            Destroy(equipmentDragCanvasObject);
        else if (equipmentDragGhostRect != null)
            Destroy(equipmentDragGhostRect.gameObject);

        equipmentDragCanvasObject = null;
        equipmentDragCanvas = null;
        equipmentDragGhostRect = null;
        equipmentDragGhostImage = null;
    }

    private void ClearEquipmentDragState()
    {
        DestroyEquipmentDragGhost();
        equipmentDragItemId = null;
        equipmentDragIsCompound = false;
        equipmentDragSourcePartyIndex = -1;
        equipmentDragSourceRuntimeSlotIndex = -1;
        equipmentDragFromInfo = false;
        equipmentDragHandled = false;
    }

    private void RefreshAfterEquipmentChange()
    {
        HideReadyInventoryDetail();
        RefreshReadyInventoryDisplay();
        LobbyInfoPanelUI.RefreshAll();
        RelicEquipPanelUI.RefreshAll();
    }

    private static void RegisterExistingReadySlots(Transform contentRoot, List<BattleBagItemSlotUI> slots)
    {
        if (contentRoot == null || slots == null)
            return;

        slots.RemoveAll(slot => slot == null);
        if (slots.Count > 0)
            return;

        for (int i = 0; i < contentRoot.childCount; i++)
        {
            Transform child = contentRoot.GetChild(i);
            BattleBagItemSlotUI slot = child != null ? child.GetComponent<BattleBagItemSlotUI>() : null;
            if (slot != null)
                slots.Add(slot);
        }
    }

    private void EnsureReadySlotCount(
        Transform contentRoot,
        List<BattleBagItemSlotUI> slots,
        BattleBagItemSlotUI prefab,
        int targetCount,
        string slotNamePrefix)
    {
        if (contentRoot == null || slots == null || prefab == null)
            return;

        slots.RemoveAll(slot => slot == null);
        while (slots.Count < targetCount)
        {
            int index = slots.Count;
            BattleBagItemSlotUI slot = Instantiate(prefab, contentRoot, false);
            slot.name = $"{slotNamePrefix}_{index + 1}";
            slot.gameObject.SetActive(true);
            slot.Clear(null, null, null);
            ApplyReadySlotScale(slot);
            slots.Add(slot);
        }
    }

    private void ApplyReadySlotScale(BattleBagItemSlotUI slot)
    {
        if (slot == null)
            return;

        float scale = Mathf.Max(0.1f, readyInventorySlotScale);
        Vector3 current = slot.transform.localScale;
        slot.transform.localScale = new Vector3(scale, scale, Mathf.Approximately(current.z, 0f) ? 1f : current.z);
    }

    private void ResolveCompoundInventoryIfNeeded()
    {
        if (compoundContentRoot != null)
            return;

        ResolveSlideTargets();
        Transform searchRoot = equipRect != null ? equipRect : ResolvePanelRoot()?.transform;
        if (searchRoot == null)
            return;

        // 새 Info_Panel의 CharN/Compound를 예전 연성제 선택 UI로 오인하지 않도록
        // Info_Panel 바로 아래의 구 구조 Compound만 허용합니다.
        Transform compoundRoot = searchRoot.Find("Compound");
        if (compoundRoot == null)
            return;

        Transform scrollView = compoundRoot.Find("Scroll View") ?? FindChildRecursive(compoundRoot, "Scroll View");
        Transform viewport = scrollView != null
            ? scrollView.Find("Viewport") ?? FindChildRecursive(scrollView, "Viewport")
            : null;
        Transform content = viewport != null
            ? viewport.Find("Content") ?? FindChildRecursive(viewport, "Content")
            : null;

        if (content != null)
            compoundContentRoot = content;
    }

    private void RefreshCompoundInventorySlots()
    {
        ResolveCompoundInventoryIfNeeded();
        if (compoundContentRoot == null || compoundSlotPrefab == null)
            return;

        RegisterExistingCompoundSlots();

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        IReadOnlyList<string> storedCompoundIds = lobby?.StoredCompoundIds;
        List<BagItemStack> stacks = BagItemStackUtility.BuildStacks(storedCompoundIds);

        int stackCount = stacks != null ? stacks.Count : 0;
        int minimumCount = Mathf.Max(CompoundMinimumSlotCount, compoundMinimumSlotCount);
        int targetCount = Mathf.Max(minimumCount, stackCount);
        EnsureCompoundSlotCount(targetCount);

        for (int i = 0; i < compoundSlots.Count; i++)
        {
            BattleBagItemSlotUI slot = compoundSlots[i];
            if (slot == null)
                continue;

            bool visible = i < targetCount;
            slot.gameObject.SetActive(visible);
            if (!visible)
                continue;

            if (i < stackCount)
            {
                BagItemStack stack = stacks[i];
                slot.Setup(stack.ItemId, stack.Count, null, null, SelectCompoundSlot);
                slot.SetSelected(slot == selectedCompoundSlot &&
                                 string.Equals(stack.ItemId, selectedCompoundId, StringComparison.Ordinal));
            }
            else
            {
                slot.Clear(null, null, null);
            }
        }

        ValidateSelectedCompound();
        SelectFirstCompoundIfNeeded();
    }

    private void ResolveCompoundSelectionViewIfNeeded()
    {
        if (compoundSelectionView != null)
            return;

        ResolveSlideTargets();
        Transform searchRoot = equipRect != null ? equipRect : ResolvePanelRoot()?.transform;
        if (searchRoot == null)
            return;

        // 새 Info_Panel의 CharN/Compound를 예전 연성제 선택 UI로 오인하지 않도록
        // Info_Panel 바로 아래의 구 구조 Compound만 허용합니다.
        Transform compoundRoot = searchRoot.Find("Compound");
        if (compoundRoot == null)
            return;

        Transform itemImageRoot = compoundRoot.Find("Itemimage") ?? FindChildRecursive(compoundRoot, "Itemimage");
        Transform line2 = itemImageRoot != null ? FindChildRecursive(itemImageRoot, "Line2") : null;

        Transform equipButtonRoot = compoundRoot.Find("Button") ?? FindChildRecursive(compoundRoot, "Button");

        compoundSelectionView = new CompoundSelectionView
        {
            ItemImageRoot = itemImageRoot,
            IconImage = itemImageRoot != null ? FindImageByNames(itemImageRoot, "Icon") : null,
            LineObject = line2 != null ? line2.gameObject : null,
            NameText = FindTextByNames(compoundRoot, "Name"),
            EquipButton = equipButtonRoot != null ? equipButtonRoot.GetComponent<Button>() : null,
            EquipButtonText = equipButtonRoot != null
                ? FindTextByNames(equipButtonRoot, "Compound_Select", "Text", "Label")
                : null
        };

        if (compoundSelectionView.EquipButton != null)
        {
            compoundSelectionView.EquipButton.onClick.RemoveListener(ToggleCompoundEquipSelection);
            compoundSelectionView.EquipButton.onClick.AddListener(ToggleCompoundEquipSelection);
            compoundSelectionView.EquipButton.interactable = !string.IsNullOrWhiteSpace(selectedCompoundId);
        }

        SetCompoundItemLineActive(false);
        ApplyImage(compoundSelectionView.IconImage, null);
        if (compoundSelectionView.NameText != null)
            compoundSelectionView.NameText.text = string.Empty;
        UpdateEquipButtonTexts();
    }

    private void SelectCompoundSlot(BattleBagItemSlotUI slot)
    {
        if (slot == null || !slot.HasItem || string.IsNullOrWhiteSpace(slot.ItemId))
            return;

        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CompoundDatabase == null ||
            !dataManager.CompoundDatabase.TryGet(slot.ItemId, out _))
        {
            return;
        }

        ResolveCompoundSelectionViewIfNeeded();

        selectedCompoundSlot = slot;
        selectedCompoundId = slot.ItemId.Trim();

        for (int i = 0; i < compoundSlots.Count; i++)
        {
            BattleBagItemSlotUI candidate = compoundSlots[i];
            if (candidate != null)
                candidate.SetSelected(candidate == selectedCompoundSlot);
        }

        ApplyImage(compoundSelectionView?.IconImage, ResolveRelicIcon(selectedCompoundId));

        if (compoundSelectionView?.NameText != null &&
            dataManager.CompoundDatabase.TryGet(selectedCompoundId, out CompoundData selectedCompound))
        {
            compoundSelectionView.NameText.text = selectedCompound != null && !string.IsNullOrWhiteSpace(selectedCompound.Name)
                ? selectedCompound.Name
                : string.Empty;
        }

        // 단순 연성제 선택은 장착 모드가 아닙니다.
        // Line2는 Compound/Button으로 장착 모드를 시작했을 때만 표시합니다.
        SetCompoundItemLineActive(isCompoundEquipSelectionActive);

        if (compoundSelectionView?.EquipButton != null)
            compoundSelectionView.EquipButton.interactable = true;

        UpdateActiveCompoundCandidateVisuals();
    }

    private void ToggleCompoundEquipSelection()
    {
        if (string.IsNullOrWhiteSpace(selectedCompoundId))
        {
            SetCompoundEquipSelectionActive(false);
            return;
        }

        // 연성제 장착 모드와 유물 장착 모드는 동시에 활성화되지 않습니다.
        ResetOwnedRelicSelection();
        SetCompoundEquipSelectionActive(!isCompoundEquipSelectionActive);
    }

    private void SetCompoundEquipSelectionActive(bool active)
    {
        isCompoundEquipSelectionActive = active && !string.IsNullOrWhiteSpace(selectedCompoundId);
        SetCompoundItemLineActive(isCompoundEquipSelectionActive);
        UpdateActiveCompoundCandidateVisuals();
        UpdateCharacterEquipTargetVisuals();
        UpdateEquipButtonTexts();
    }

    private void ValidateSelectedCompound()
    {
        if (string.IsNullOrWhiteSpace(selectedCompoundId))
            return;

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (lobby?.StoredCompoundIds == null || FindStoredCompoundIndex(lobby.StoredCompoundIds, selectedCompoundId) < 0)
            ResetCompoundSelection();
    }

    private void SelectFirstCompoundIfNeeded()
    {
        if (!string.IsNullOrWhiteSpace(selectedCompoundId))
            return;

        for (int i = 0; i < compoundSlots.Count; i++)
        {
            BattleBagItemSlotUI slot = compoundSlots[i];
            if (slot == null || !slot.gameObject.activeSelf || !slot.HasItem || string.IsNullOrWhiteSpace(slot.ItemId))
                continue;

            SelectCompoundSlot(slot);
            return;
        }

        ResolveCompoundSelectionViewIfNeeded();
        if (compoundSelectionView?.NameText != null)
            compoundSelectionView.NameText.text = string.Empty;
    }

    private void ResetCompoundSelection()
    {
        isCompoundEquipSelectionActive = false;
        selectedCompoundId = null;
        selectedCompoundSlot = null;

        for (int i = 0; i < compoundSlots.Count; i++)
        {
            if (compoundSlots[i] != null)
                compoundSlots[i].SetSelected(false);
        }

        ResolveCompoundSelectionViewIfNeeded();
        ApplyImage(compoundSelectionView?.IconImage, null);
        if (compoundSelectionView?.NameText != null)
            compoundSelectionView.NameText.text = string.Empty;
        SetCompoundItemLineActive(false);
        if (compoundSelectionView?.EquipButton != null)
            compoundSelectionView.EquipButton.interactable = false;
        UpdateActiveCompoundCandidateVisuals();
        UpdateCharacterEquipTargetVisuals();
        UpdateEquipButtonTexts();
    }

    private void SetCompoundItemLineActive(bool active)
    {
        if (compoundSelectionView?.LineObject != null)
            compoundSelectionView.LineObject.SetActive(active);
    }

    private void UpdateActiveCompoundCandidateVisuals()
    {
        // 장착 대상은 이제 Active 슬롯이 아니라 캐릭터 Back 전체입니다.
        for (int i = 0; i < CharacterCount; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;
            if (view.ActiveCompoundButton != null)
                view.ActiveCompoundButton.interactable = false;
        }
    }

    private bool IsCharacterEquipSelectionActive()
    {
        return (isCompoundEquipSelectionActive && !string.IsNullOrWhiteSpace(selectedCompoundId)) ||
               (isOwnedRelicSelected && !string.IsNullOrWhiteSpace(selectedOwnedRelicId));
    }

    private void UpdateCharacterEquipTargetVisuals()
    {
        ResolveCharacterTextIfNeeded();
        bool equipMode = IsCharacterEquipSelectionActive();

        if (characterTextObject != null)
            characterTextObject.SetActive(equipMode);

        DataManager dataManager = DataManager.Instance;
        for (int i = 0; i < CharacterCount; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            if (view.CharacterSelectImage != null)
            {
                RestoreCharacterSelectColor(view);
                view.CharacterSelectImage.gameObject.SetActive(equipMode);
            }

            string characterId = dataManager?.PartyRuntimeStore?.GetCharacterId(i);
            bool canSelect = equipMode && !string.IsNullOrWhiteSpace(characterId);
            view.IsCharacterEquipTarget = canSelect;

            if (!canSelect)
                RestoreCharacterSelectColor(view);
        }
    }

    private void OnCharacterBackPointerEnter(int partySlotIndex)
    {
        if (partySlotIndex < 0 || partySlotIndex >= CharacterCount)
            return;

        CharacterView view = characterViews[partySlotIndex];
        if (view == null || !view.IsCharacterEquipTarget)
            return;

        if (view.BackImage != null)
            view.BackImage.color = characterBackHoverColor;

        if (view.CharacterSelectImage != null)
            view.CharacterSelectImage.color = characterSelectHoverColor;
    }

    private void OnCharacterBackPointerExit(int partySlotIndex)
    {
        if (partySlotIndex < 0 || partySlotIndex >= CharacterCount)
            return;

        RestoreCharacterSelectColor(characterViews[partySlotIndex]);
    }

    private void OnCharacterBackClicked(int partySlotIndex)
    {
        if (partySlotIndex < 0 || partySlotIndex >= CharacterCount)
            return;

        CharacterView view = characterViews[partySlotIndex];
        if (view == null || !view.IsCharacterEquipTarget)
            return;

        RestoreCharacterSelectColor(view);

        if (isCompoundEquipSelectionActive)
        {
            TryEquipSelectedCompoundToCharacter(partySlotIndex);
            return;
        }

        if (isOwnedRelicSelected)
            TryEquipSelectedOwnedRelicToCharacter(partySlotIndex);
    }

    private static void RestoreCharacterSelectColor(CharacterView view)
    {
        if (view == null)
            return;

        if (view.BackImage != null)
            view.BackImage.color = view.BackOriginalColor;

        if (view.CharacterSelectImage != null)
            view.CharacterSelectImage.color = view.CharacterSelectOriginalColor;
    }

    private void TryEquipSelectedCompoundToCharacter(int partySlotIndex)
    {
        if (!isCompoundEquipSelectionActive || string.IsNullOrWhiteSpace(selectedCompoundId))
            return;

        DataManager dataManager = DataManager.Instance;
        if (dataManager?.CompoundDatabase == null || dataManager.CharacterRuntimeStore == null)
            return;

        string characterId = dataManager.PartyRuntimeStore?.GetCharacterId(partySlotIndex);
        if (string.IsNullOrWhiteSpace(characterId) ||
            !dataManager.CharacterRuntimeStore.TryGet(characterId, out CharacterRuntimeData runtime))
        {
            return;
        }

        if (!dataManager.CompoundDatabase.TryGet(selectedCompoundId, out CompoundData compound))
            return;

        LobbyRuntimeData lobby = dataManager.LobbyRuntimeStore?.GetOrCreate();
        if (lobby?.StoredCompoundIds == null)
            return;

        int storedIndex = FindStoredCompoundIndex(lobby.StoredCompoundIds, selectedCompoundId);
        if (storedIndex < 0)
        {
            ResetCompoundSelection();
            RefreshCharacterData();
            return;
        }

        ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);
        int activeSlotIndex = ActiveRelicRuntimeUtility.ActiveRelicSlotIndex;
        string previousCompoundId = runtime.EquippedRelicIds[activeSlotIndex];

        // 보관 중인 동일 연성제가 여러 개여도 선택한 1개만 제거합니다.
        lobby.StoredCompoundIds.RemoveAt(storedIndex);

        if (!string.IsNullOrWhiteSpace(previousCompoundId))
            lobby.StoredCompoundIds.Add(previousCompoundId.Trim());

        runtime.EquippedRelicIds[activeSlotIndex] = selectedCompoundId;
        ActiveRelicRuntimeUtility.ResetUses(runtime, compound);

        ResetCompoundSelection();
        RefreshCharacterData();
        RelicEquipPanelUI.RefreshAll();
    }

    private static int FindStoredCompoundIndex(IList<string> ids, string targetId)
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

    private void RegisterExistingCompoundSlots()
    {
        if (compoundContentRoot == null)
            return;

        compoundSlots.RemoveAll(slot => slot == null);
        if (compoundSlots.Count > 0)
            return;

        for (int i = 0; i < compoundContentRoot.childCount; i++)
        {
            Transform child = compoundContentRoot.GetChild(i);
            if (child == null)
                continue;

            BattleBagItemSlotUI slot = child.GetComponent<BattleBagItemSlotUI>();
            if (slot != null)
                compoundSlots.Add(slot);
        }
    }

    private void EnsureCompoundSlotCount(int targetCount)
    {
        if (compoundContentRoot == null || compoundSlotPrefab == null)
            return;

        compoundSlots.RemoveAll(slot => slot == null);

        while (compoundSlots.Count < targetCount)
        {
            int index = compoundSlots.Count;
            BattleBagItemSlotUI slot = Instantiate(compoundSlotPrefab, compoundContentRoot, false);
            slot.name = $"{compoundSlotPrefab.name}_{index}";
            slot.gameObject.SetActive(true);
            slot.Clear(null, null, null);
            compoundSlots.Add(slot);
        }
    }

    private void ResolveOwnedRelicViewIfNeeded()
    {
        if (ownedRelicView != null)
            return;

        ResolveSlideTargets();
        Transform searchRoot = equipRect != null ? equipRect : ResolvePanelRoot()?.transform;
        if (searchRoot == null)
            return;

        // 새 Info_Panel의 CharN/Relic을 예전 유물 선택 UI로 오인하지 않도록
        // Info_Panel 바로 아래의 구 구조 Relic만 허용합니다.
        Transform relicRoot = searchRoot.Find("Relic");
        if (relicRoot == null)
            return;

        Transform itemImageRoot = FindChildRecursive(relicRoot, "Itemimage");
        Transform lineRoot = itemImageRoot != null
            ? FindChildRecursive(itemImageRoot, "Line2")
            : null;
        Transform equipButtonRoot = relicRoot.Find("Button") ?? FindChildRecursive(relicRoot, "Button");

        ownedRelicView = new OwnedRelicView
        {
            Root = relicRoot,
            ItemButton = itemImageRoot != null ? itemImageRoot.GetComponent<Button>() : null,
            EquipButton = equipButtonRoot != null ? equipButtonRoot.GetComponent<Button>() : null,
            LineObject = lineRoot != null ? lineRoot.gameObject : null,
            IconImage = itemImageRoot != null
                ? FindImageByNames(itemImageRoot, "Icon") ?? itemImageRoot.GetComponent<Image>()
                : FindImageByNames(relicRoot, "Icon"),
            NameText = FindTextByNames(relicRoot, "Name"),
            RarityText = FindTextByNames(relicRoot, "Rarity"),
            EffectText = FindTextByNames(relicRoot, "Effect"),
            EquipButtonText = equipButtonRoot != null
                ? FindTextByNames(equipButtonRoot, "Relic_Select", "Text", "Label")
                : null
        };

        // 아이콘 클릭은 장착 선택을 시작하지 않습니다.
        if (ownedRelicView.ItemButton != null)
            ownedRelicView.ItemButton.onClick.RemoveListener(ToggleOwnedRelicSelection);

        if (ownedRelicView.EquipButton != null)
        {
            ownedRelicView.EquipButton.onClick.RemoveListener(ToggleOwnedRelicSelection);
            ownedRelicView.EquipButton.onClick.AddListener(ToggleOwnedRelicSelection);
            ownedRelicView.EquipButton.interactable = false;
        }

        SetOwnedRelicLineActive(false);
        UpdateEquipButtonTexts();
    }

    private void ResolveCharacterTextIfNeeded()
    {
        if (characterTextObject != null)
            return;

        Transform searchRoot = ResolveInfoPanelTransform();
        Transform characterText = searchRoot != null ? searchRoot.Find("Character_Text") : null;

        if (characterText != null)
        {
            characterTextObject = characterText.gameObject;
            if (!IsCharacterEquipSelectionActive())
                characterTextObject.SetActive(false);
        }
    }

    private void ResolveCharacterViewsIfNeeded()
    {
        if (!autoBindCharacterHierarchy)
            return;

        Transform searchRoot = ResolveInfoPanelTransform();
        if (searchRoot == null)
            return;

        for (int i = 0; i < CharacterCount; i++)
        {
            if (characterViews[i] != null && characterViews[i].Root != null)
                continue;

            characterViews[i] = BuildCharacterView(searchRoot, i);
        }
    }

    private static CharacterView BuildCharacterView(Transform searchRoot, int index)
    {
        Transform root = FindChildRecursive(searchRoot, "Char" + (index + 1));
        if (root == null)
            return null;

        // 새 Info_Panel의 Char1~3은 표시 전용이며 직접 Name 자식이 없습니다.
        // 예전 장착 UI만 직접 Name 자식을 가지고 있으므로, 이 조건으로 구 구조와 새 구조를 분리합니다.
        Transform legacyNameRoot = root.Find("Name");
        if (legacyNameRoot == null)
            return null;

        Transform backRoot = root.Find("Back") ?? FindChildRecursive(root, "Back");
        Image backImage = backRoot != null ? backRoot.GetComponent<Image>() : null;
        Transform selectRoot = FindChildRecursive(searchRoot, $"Character{index + 1}_Select");
        Image characterSelectImage = selectRoot != null ? selectRoot.GetComponent<Image>() : null;

        TMP_Text legacyNameText = legacyNameRoot.GetComponent<TMP_Text>() ??
                                  legacyNameRoot.GetComponentInChildren<TMP_Text>(true);

        CharacterView view = new CharacterView
        {
            Root = root,
            NameText = legacyNameText,
            Mark1Image = FindImageByNames(root, "mark1", "Mark1"),
            Mark2Image = FindImageByNames(root, "mark2", "Mark2"),
            BackImage = backImage,
            BackOriginalColor = backImage != null ? backImage.color : Color.white,
            CharacterSelectImage = characterSelectImage,
            CharacterSelectOriginalColor = characterSelectImage != null ? characterSelectImage.color : Color.white
        };

        if (characterSelectImage != null)
            characterSelectImage.gameObject.SetActive(false);

        if (backRoot != null)
        {
            if (backImage != null)
                backImage.raycastTarget = true;

            EventTrigger trigger = backRoot.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = backRoot.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();

            int partySlotIndex = index;
            AddEventTrigger(trigger, EventTriggerType.PointerEnter,
                _ => FindEquipPanelOwner(backRoot)?.OnCharacterBackPointerEnter(partySlotIndex));
            AddEventTrigger(trigger, EventTriggerType.PointerExit,
                _ => FindEquipPanelOwner(backRoot)?.OnCharacterBackPointerExit(partySlotIndex));
            AddEventTrigger(trigger, EventTriggerType.PointerClick,
                _ => FindEquipPanelOwner(backRoot)?.OnCharacterBackClicked(partySlotIndex));
        }

        Transform activeRoot = root.Find("Active") ?? FindChildRecursive(root, "Active");
        if (activeRoot != null)
        {
            view.ActiveCompoundIcon = FindImageByNames(activeRoot, "Icon");

            Button activeButton = activeRoot.GetComponent<Button>();
            if (activeButton == null)
                activeButton = activeRoot.gameObject.AddComponent<Button>();

            activeButton.transition = Selectable.Transition.None;
            activeButton.interactable = false;

            int partySlotIndex = index;
            activeButton.onClick.AddListener(() =>
                FindEquipPanelOwner(activeRoot)?.TryEquipSelectedCompoundToCharacter(partySlotIndex));
            view.ActiveCompoundButton = activeButton;
        }

        Transform relicRoot = root.Find("Relic") ?? FindChildRecursive(root, "Relic");
        for (int i = 0; i < VisibleRelicSlotCount; i++)
        {
            string twoDigitName = "Relic" + (i + 1).ToString("00");
            string oneDigitName = "Relic" + (i + 1);
            Transform slotRoot = relicRoot != null
                ? FindChildRecursive(relicRoot, twoDigitName) ?? FindChildRecursive(relicRoot, oneDigitName)
                : null;

            if (slotRoot == null)
                continue;

            Image iconImage = FindImageByNames(slotRoot, "Icon");
            Button slotButton = slotRoot.GetComponent<Button>();
            if (slotButton == null)
                slotButton = slotRoot.gameObject.AddComponent<Button>();

            slotButton.transition = Selectable.Transition.None;
            slotButton.interactable = false;

            int partySlotIndex = index;
            int visibleRelicSlotIndex = i;
            slotButton.onClick.AddListener(() =>
                FindEquipPanelOwner(slotRoot)?.TryEquipSelectedOwnedRelic(
                    partySlotIndex,
                    visibleRelicSlotIndex));

            view.RelicSlots[i] = new RelicSlotView
            {
                Root = slotRoot,
                NumberText = FindTextByNames(slotRoot, "Number"),
                IconImage = iconImage,
                Button = slotButton
            };
        }

        Transform skillRoot = root.Find("Skill") ?? FindChildRecursive(root, "Skill");
        for (int i = 0; i < VisibleSkillSlotCount; i++)
        {
            string lowerName = "skill" + (i + 1);
            string upperName = "Skill" + (i + 1);
            Transform slotRoot = skillRoot != null
                ? FindChildRecursive(skillRoot, lowerName) ?? FindChildRecursive(skillRoot, upperName)
                : null;

            if (slotRoot == null)
                continue;

            view.SkillIcons[i] = FindImageByNames(slotRoot, "Icon") ?? slotRoot.GetComponent<Image>();
        }

        return view;
    }

    private static string FormatRelicEffectDescription(RelicData relic)
    {
        if (relic == null || string.IsNullOrWhiteSpace(relic.EffectDesc))
            return string.Empty;

        string result = relic.EffectDesc;
        result = ReplaceIndexedEffectValues(result, "ValueRate", relic.ValueRate);
        result = ReplaceIndexedEffectValues(result, "CountRate", relic.CountRate);
        result = ReplaceEffectValue(result, "{ValueRate}", relic.ValueRate);
        result = ReplaceEffectValue(result, "{CountRate}", relic.CountRate);
        return result;
    }

    private static string ReplaceIndexedEffectValues(string source, string tokenName, string values)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrWhiteSpace(tokenName))
            return source;

        string[] splitValues = string.IsNullOrWhiteSpace(values)
            ? Array.Empty<string>()
            : values.Split(';');

        for (int i = 0; i < splitValues.Length; i++)
        {
            string token = $"{{{tokenName}{i + 1}}}";
            if (!source.Contains(token))
                continue;

            source = source.Replace(token, GetDisplayRateValue(splitValues[i]));
        }

        return source;
    }

    private static string ReplaceEffectValue(string source, string token, string value)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token) || !source.Contains(token))
            return source;

        return source.Replace(token, GetDisplayRateValue(value));
    }

    private static string GetDisplayRateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        string displayValue = value.Trim();
        if (displayValue.Length > 1 &&
            displayValue[0] == '-' &&
            float.TryParse(
                displayValue.Substring(1),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out _))
        {
            return displayValue.Substring(1);
        }

        return displayValue;
    }

    private static void AddEventTrigger(
        EventTrigger trigger,
        EventTriggerType eventType,
        UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        if (trigger == null)
            return;

        if (trigger.triggers == null)
            trigger.triggers = new List<EventTrigger.Entry>();

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    private static LobbyEquipPanelUI FindEquipPanelOwner(Transform child)
    {
        LobbyEquipPanelUI owner = child != null
            ? child.GetComponentInParent<LobbyEquipPanelUI>(true)
            : null;

        if (owner != null)
            return owner;

        return FindFirstObjectByType<LobbyEquipPanelUI>(FindObjectsInactive.Include);
    }

    private void SetInfoPanelActive(bool active)
    {
        // Info_Panel은 항상 활성 상태를 유지합니다. 닫힘/열림은 X 위치로만 표현합니다.
        if (!active)
            return;

        Transform infoPanel = ResolveInfoPanelTransform();
        if (infoPanel != null && !infoPanel.gameObject.activeSelf)
            infoPanel.gameObject.SetActive(true);

        LobbyInfoPanelUI.RefreshAll();
    }

    private Transform ResolveInfoPanelTransform()
    {
        GameObject infoPanel = FindSceneObject(InfoPanelName);
        return infoPanel != null ? infoPanel.transform : null;
    }

    private GameObject ResolvePanelRoot()
    {
        if (panelRoot != null && panelRoot.name == ReadyPanelName)
            return panelRoot;

        if (gameObject.name == ReadyPanelName)
        {
            panelRoot = gameObject;
            return panelRoot;
        }

        GameObject found = FindSceneObject(ReadyPanelName);
        if (found != null)
            panelRoot = found;
        else if (panelRoot == null)
            panelRoot = gameObject;

        return panelRoot;
    }

    private static GameObject FindSceneObject(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, targetName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static void SetAnchoredX(RectTransform target, float x)
    {
        if (target == null)
            return;

        Vector2 position = target.anchoredPosition;
        position.x = x;
        target.anchoredPosition = position;
    }

    private static TMP_Text FindTextByNames(Transform root, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform target = FindChildRecursive(root, names[i]);
            if (target == null)
                continue;

            TMP_Text text = target.GetComponent<TMP_Text>() ?? target.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                return text;
        }

        return null;
    }

    private static Image FindImageByNames(Transform root, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform target = FindChildRecursive(root, names[i]);
            if (target == null)
                continue;

            Image image = target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true);
            if (image != null)
                return image;
        }

        return null;
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

    [Serializable]
    private sealed class CompoundSelectionView
    {
        public Transform ItemImageRoot;
        public Image IconImage;
        public GameObject LineObject;
        public TMP_Text NameText;
        public Button EquipButton;
        public TMP_Text EquipButtonText;
    }

    [Serializable]
    private sealed class OwnedRelicView
    {
        public Transform Root;
        public Button ItemButton;
        public Button EquipButton;
        public GameObject LineObject;
        public Image IconImage;
        public TMP_Text NameText;
        public TMP_Text RarityText;
        public TMP_Text EffectText;
        public TMP_Text EquipButtonText;
    }

    [Serializable]
    private sealed class RelicSlotView
    {
        public Transform Root;
        public TMP_Text NumberText;
        public Image IconImage;
        public Button Button;
        public bool IsCandidate;
    }

    [Serializable]
    private sealed class CharacterView
    {
        public Transform Root;
        public TMP_Text NameText;
        public Image Mark1Image;
        public Image Mark2Image;
        public Image BackImage;
        public Color BackOriginalColor = Color.white;
        public Image CharacterSelectImage;
        public Color CharacterSelectOriginalColor = Color.white;
        public bool IsCharacterEquipTarget;
        public Image ActiveCompoundIcon;
        public Button ActiveCompoundButton;
        public RelicSlotView[] RelicSlots = new RelicSlotView[VisibleRelicSlotCount];
        public Image[] SkillIcons = new Image[VisibleSkillSlotCount];
    }
}

/// <summary>
/// Ready_Panel/Info_Panel 장착 슬롯의 런타임 포인터 이벤트 중계기입니다.
/// 프리팹 수정 없이 클릭/드래그/드롭을 연결하기 위해 사용합니다.
/// </summary>
public sealed class LobbyReadyEquipmentPointerRelay : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private Action<PointerEventData> onClick;
    private Action<PointerEventData> onBeginDrag;
    private Action<PointerEventData> onDrag;
    private Action<PointerEventData> onEndDrag;
    private Action<PointerEventData> onDrop;

    public void Configure(
        Action<PointerEventData> click,
        Action<PointerEventData> beginDrag,
        Action<PointerEventData> drag,
        Action<PointerEventData> endDrag,
        Action<PointerEventData> drop)
    {
        onClick = click;
        onBeginDrag = beginDrag;
        onDrag = drag;
        onEndDrag = endDrag;
        onDrop = drop;
    }

    public void OnPointerClick(PointerEventData eventData) => onClick?.Invoke(eventData);
    public void OnBeginDrag(PointerEventData eventData) => onBeginDrag?.Invoke(eventData);
    public void OnDrag(PointerEventData eventData) => onDrag?.Invoke(eventData);
    public void OnEndDrag(PointerEventData eventData) => onEndDrag?.Invoke(eventData);
    public void OnDrop(PointerEventData eventData) => onDrop?.Invoke(eventData);
}

