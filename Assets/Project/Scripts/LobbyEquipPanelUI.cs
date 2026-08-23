using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 장비 관리용 Equip_panel 컨트롤러입니다.
/// 패널 자체는 항상 활성 상태로 유지하고 Equip/Charter의 위치로 열림/닫힘을 표현합니다.
/// 또한 Charter/Char1~3에 현재 파티 캐릭터의 이름, 마크, 연성제, 유물, 교체 가능한 기억을 표시합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyEquipPanelUI : MonoBehaviour
{
    private const int CharacterCount = 3;
    private const int VisibleRelicSlotCount = 6;
    private const int VisibleSkillSlotCount = 3;
    private const int CompoundMinimumSlotCount = 15;

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
    [SerializeField] private float equipStartX = -1350f;
    [SerializeField] private float equipEndX = -450f;
    [SerializeField] private float charterStartX = 1350f;
    [SerializeField] private float charterEndX = 450f;

    [Header("Slide Animation")]
    [SerializeField, Min(0f)] private float slideDuration = 0.35f;
    [SerializeField]
    private AnimationCurve slideCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f));

    [Header("Close Input")]
    [SerializeField] private bool closeOnOutsideClick = true;

    [Header("Opened Panel")]
    [SerializeField] private bool bringToFront = true;

    [Header("Character Data")]
    [Tooltip("Charter/Char1~3 구조를 이름으로 자동 연결합니다.")]
    [SerializeField] private bool autoBindCharacterHierarchy = true;

    [Header("Compound Inventory")]
    [Tooltip("Equip/Compound/Scroll View/Viewport/Content를 비워두면 이름으로 자동 연결합니다.")]
    [SerializeField] private Transform compoundContentRoot;
    [Tooltip("Content에 생성할 StorageSlotUI 프리팹입니다. BattleBagItemSlotUI가 붙어 있어야 합니다.")]
    [SerializeField] private BattleBagItemSlotUI compoundSlotPrefab;
    [Tooltip("연성제가 없어도 표시할 최소 빈 슬롯 수입니다.")]
    [SerializeField, Min(1)] private int compoundMinimumSlotCount = CompoundMinimumSlotCount;

    [Header("Compound Equip Candidate Pulse")]
    [Tooltip("연성제 선택 시 Char1~3 Active/Back의 숨쉬기 최소 알파값입니다.")]
    [SerializeField, Range(0f, 1f)] private float activeCandidatePulseMinAlpha = 0.25f;
    [Tooltip("연성제 선택 시 Char1~3 Active/Back의 숨쉬기 최대 알파값입니다.")]
    [SerializeField, Range(0f, 1f)] private float activeCandidatePulseMaxAlpha = 1f;
    [Tooltip("연성제 장착 후보 Active/Back 숨쉬기 속도입니다.")]
    [SerializeField, Min(0.01f)] private float activeCandidatePulseSpeed = 0.6f;

    [Header("Relic Equip Candidate Pulse")]
    [Tooltip("장착 가능한 첫 빈 유물 슬롯의 숨쉬기 최소 알파값입니다. RGB는 항상 FFFFFF로 유지됩니다.")]
    [SerializeField, Range(0f, 1f)] private float candidatePulseMinAlpha = 0.25f;
    [Tooltip("장착 가능한 첫 빈 유물 슬롯의 숨쉬기 최대 알파값입니다. RGB는 항상 FFFFFF로 유지됩니다.")]
    [SerializeField, Range(0f, 1f)] private float candidatePulseMaxAlpha = 1f;
    [Tooltip("유물 장착 후보 슬롯의 숨쉬기 속도입니다.")]
    [SerializeField, Min(0.01f)] private float candidatePulseSpeed = 2f;

    private readonly CharacterView[] characterViews = new CharacterView[CharacterCount];
    private OwnedRelicView ownedRelicView;
    private RecordPanelUI recordPanelUI;
    private bool isOwnedRelicSelected;
    private string selectedOwnedRelicId;
    private readonly List<BattleBagItemSlotUI> compoundSlots = new();
    private CompoundSelectionView compoundSelectionView;
    private BattleBagItemSlotUI selectedCompoundSlot;
    private string selectedCompoundId;

    private Coroutine slideAnimationCoroutine;
    private RectTransform toggleButtonRect;
    private bool isOpen;
    private bool isClosing;
    private int lastToggleFrame = -1;

    public bool IsOpen => isOpen && !isClosing;

    private void Awake()
    {
        ResolvePanelRoot();
        ResolveSlideTargets();
        ResolveCharacterViewsIfNeeded();
        ResolveOwnedRelicViewIfNeeded();
        ResolveCompoundInventoryIfNeeded();
        ResolveCompoundSelectionViewIfNeeded();
        ResetSlidePositions();
        isOpen = false;
        isClosing = false;
    }

    private void OnEnable()
    {
        // Equip_panel은 항상 활성화된 상태를 유지합니다.
        // 다시 활성화된 경우에도 닫힌 위치에서 시작합니다.
        if (!isOpen && !isClosing)
            ResetSlidePositions();

        RefreshCharacterData();
    }

    private void Update()
    {
        UpdateRelicCandidatePulse();
        UpdateActiveCompoundCandidatePulse();

        if (!IsOpen || Time.frameCount == lastToggleFrame)
            return;

        if (!closeOnOutsideClick || !Input.GetMouseButtonDown(0))
            return;

        Vector2 pointerPosition = Input.mousePosition;
        if (IsPointerInsideOpenArea(pointerPosition))
            return;

        Close();
    }

    private void OnDisable()
    {
        StopSlideAnimation();
        isOpen = false;
        isClosing = false;
        ResetOwnedRelicSelection();
        ResetCompoundSelection();
        ResetSlidePositions();
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDestroy()
    {
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    /// <summary>
    /// Equip 버튼 자신의 RectTransform을 등록합니다.
    /// 버튼 클릭을 패널 바깥 클릭으로 오인하지 않도록 사용합니다.
    /// </summary>
    public void SetToggleButton(RectTransform buttonRect)
    {
        toggleButtonRect = buttonRect;
    }

    /// <summary>
    /// Equip 버튼에서 호출합니다.
    /// 닫혀 있으면 열고, 열려 있으면 시작 위치로 슬라이드 아웃합니다.
    /// </summary>
    public void Toggle()
    {
        lastToggleFrame = Time.frameCount;

        if (isOpen && !isClosing)
            Close();
        else
            Open();
    }

    public void Open()
    {
        GameObject root = ResolvePanelRoot();
        if (root == null)
        {
            Debug.LogWarning("[LobbyEquipPanelUI] Equip_panel을 찾을 수 없습니다.", this);
            return;
        }

        if (isOpen && !isClosing)
            return;

        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return;

        if (UIPanelButton.IsMenuPanelOpen)
            return;

        TitleManager.CloseTitleModePanelsExceptInScene(root);

        if (!root.activeSelf)
            root.SetActive(true);

        if (bringToFront)
            root.transform.SetAsLastSibling();

        ResolveSlideTargets();
        ResolveCharacterViewsIfNeeded();
        ResolveOwnedRelicViewIfNeeded();
        ResolveCompoundInventoryIfNeeded();
        ResolveCompoundSelectionViewIfNeeded();
        RefreshCharacterData();
        StopSlideAnimation();

        // 닫히는 도중 다시 열면 현재 위치에서 자연스럽게 이어서 엽니다.
        isClosing = false;
        isOpen = true;
        LobbyPositionModalInputBlocker.Block(this);
        slideAnimationCoroutine = StartCoroutine(PlaySlideAnimation(true));
    }

    public void Close()
    {
        if (!isOpen || isClosing)
            return;

        ResetOwnedRelicSelection();
        ResetCompoundSelection();
        ResolveSlideTargets();
        StopSlideAnimation();
        isClosing = true;
        isOpen = false;
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
        RefreshOwnedRelicData();
        RefreshCompoundInventorySlots();

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

        isOwnedRelicSelected = true;
        selectedOwnedRelicId = relicId;
        SetOwnedRelicLineActive(true);
        UpdateRelicEquipCandidateVisuals();
    }

    private void ResetOwnedRelicSelection()
    {
        isOwnedRelicSelected = false;
        selectedOwnedRelicId = null;
        SetOwnedRelicLineActive(false);
        UpdateRelicEquipCandidateVisuals();
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

            int firstEmptyRuntimeSlot = isOwnedRelicSelected
                ? FirstEmptyRelicRuntimeSlotIndex(runtime)
                : -1;

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

                // 장착 전에는 Icon을 미리 보여주지 않습니다.
                // 실제 유물이 장착된 슬롯만 Icon을 활성화합니다.
                if (slotView.IconImage != null && !hasEquippedRelic)
                    slotView.IconImage.gameObject.SetActive(false);
                else if (slotView.IconImage != null)
                    slotView.IconImage.gameObject.SetActive(true);

                bool candidate =
                    isOwnedRelicSelected &&
                    !hasEquippedRelic &&
                    runtimeSlotIndex == firstEmptyRuntimeSlot;

                slotView.IsCandidate = candidate;

                if (slotView.Button != null)
                    slotView.Button.interactable = candidate;

                if (slotView.SlotBackgroundImage != null)
                {
                    if (candidate)
                    {
                        slotView.SlotBackgroundImage.enabled = true;
                    }
                    else
                    {
                        slotView.SlotBackgroundImage.color = slotView.OriginalBackgroundColor;
                        slotView.SlotBackgroundImage.enabled = slotView.OriginalBackgroundEnabled;
                    }
                }
            }
        }
    }

    private void UpdateRelicCandidatePulse()
    {
        if (!isOwnedRelicSelected)
            return;

        float pulse = (Mathf.Sin(Time.unscaledTime * candidatePulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float pulseAlpha = Mathf.Lerp(candidatePulseMinAlpha, candidatePulseMaxAlpha, pulse);
        Color pulseColor = new Color(1f, 1f, 1f, pulseAlpha);

        for (int i = 0; i < CharacterCount; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            for (int slot = 0; slot < VisibleRelicSlotCount; slot++)
            {
                RelicSlotView slotView = view.RelicSlots[slot];
                if (slotView?.SlotBackgroundImage == null || !slotView.IsCandidate)
                    continue;

                slotView.SlotBackgroundImage.enabled = true;
                slotView.SlotBackgroundImage.color = pulseColor;
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
            if (slotView != null)
                ApplyImage(slotView.IconImage, ResolveRelicIcon(relicId));
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

            ApplyImage(view.SkillIcons[i], icon, SkillRarityUtility.GetSkillIconColor(skillId));
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
        ApplyImage(view.ActiveCompoundIcon, null);
        view.IsActiveCompoundCandidate = false;
        if (view.ActiveCompoundButton != null)
            view.ActiveCompoundButton.interactable = false;
        if (view.ActiveCompoundBackImage != null)
        {
            view.ActiveCompoundBackImage.color = view.ActiveCompoundBackOriginalColor;
            view.ActiveCompoundBackImage.enabled = view.ActiveCompoundBackOriginalEnabled;
        }

        for (int i = 0; i < view.RelicSlots.Length; i++)
        {
            RelicSlotView slotView = view.RelicSlots[i];
            if (slotView == null)
                continue;

            ApplyImage(slotView.IconImage, null);
            if (slotView.Button != null)
                slotView.Button.interactable = false;
        }

        for (int i = 0; i < view.SkillIcons.Length; i++)
            ApplyImage(view.SkillIcons[i], null);
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
        GameObject root = ResolvePanelRoot();
        if (root == null)
            return;

        if (equipRect == null)
        {
            Transform equip = FindChildRecursive(root.transform, "Equip");
            if (equip != null)
                equipRect = equip as RectTransform;
        }

        if (charterRect == null)
        {
            Transform charter = FindChildRecursive(root.transform, "Charter");
            if (charter != null)
                charterRect = charter as RectTransform;
        }
    }

    private void ResolveCompoundInventoryIfNeeded()
    {
        if (compoundContentRoot != null)
            return;

        ResolveSlideTargets();
        Transform searchRoot = equipRect != null ? equipRect : ResolvePanelRoot()?.transform;
        if (searchRoot == null)
            return;

        Transform compoundRoot = searchRoot.Find("Compound") ?? FindChildRecursive(searchRoot, "Compound");
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
    }

    private void ResolveCompoundSelectionViewIfNeeded()
    {
        if (compoundSelectionView != null)
            return;

        ResolveSlideTargets();
        Transform searchRoot = equipRect != null ? equipRect : ResolvePanelRoot()?.transform;
        if (searchRoot == null)
            return;

        Transform compoundRoot = searchRoot.Find("Compound") ?? FindChildRecursive(searchRoot, "Compound");
        if (compoundRoot == null)
            return;

        Transform itemImageRoot = compoundRoot.Find("Itemimage") ?? FindChildRecursive(compoundRoot, "Itemimage");
        Transform line2 = itemImageRoot != null ? FindChildRecursive(itemImageRoot, "Line2") : null;

        compoundSelectionView = new CompoundSelectionView
        {
            ItemImageRoot = itemImageRoot,
            IconImage = itemImageRoot != null ? FindImageByNames(itemImageRoot, "Icon") : null,
            LineObject = line2 != null ? line2.gameObject : null
        };

        SetCompoundItemLineActive(false);
        ApplyImage(compoundSelectionView.IconImage, null);
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
        ResetOwnedRelicSelection();

        selectedCompoundSlot = slot;
        selectedCompoundId = slot.ItemId.Trim();

        for (int i = 0; i < compoundSlots.Count; i++)
        {
            BattleBagItemSlotUI candidate = compoundSlots[i];
            if (candidate != null)
                candidate.SetSelected(candidate == selectedCompoundSlot);
        }

        ApplyImage(compoundSelectionView?.IconImage, ResolveRelicIcon(selectedCompoundId));
        SetCompoundItemLineActive(true);
        UpdateActiveCompoundCandidateVisuals();
    }

    private void ValidateSelectedCompound()
    {
        if (string.IsNullOrWhiteSpace(selectedCompoundId))
            return;

        LobbyRuntimeData lobby = DataManager.Instance?.LobbyRuntimeStore?.GetOrCreate();
        if (lobby?.StoredCompoundIds == null || FindStoredCompoundIndex(lobby.StoredCompoundIds, selectedCompoundId) < 0)
            ResetCompoundSelection();
    }

    private void ResetCompoundSelection()
    {
        selectedCompoundId = null;
        selectedCompoundSlot = null;

        for (int i = 0; i < compoundSlots.Count; i++)
        {
            if (compoundSlots[i] != null)
                compoundSlots[i].SetSelected(false);
        }

        ResolveCompoundSelectionViewIfNeeded();
        ApplyImage(compoundSelectionView?.IconImage, null);
        SetCompoundItemLineActive(false);
        UpdateActiveCompoundCandidateVisuals();
    }

    private void SetCompoundItemLineActive(bool active)
    {
        if (compoundSelectionView?.LineObject != null)
            compoundSelectionView.LineObject.SetActive(active);
    }

    private void UpdateActiveCompoundCandidateVisuals()
    {
        bool hasSelection = !string.IsNullOrWhiteSpace(selectedCompoundId);
        DataManager dataManager = DataManager.Instance;

        for (int i = 0; i < CharacterCount; i++)
        {
            CharacterView view = characterViews[i];
            if (view == null)
                continue;

            string characterId = dataManager?.PartyRuntimeStore?.GetCharacterId(i);
            bool candidate = hasSelection && !string.IsNullOrWhiteSpace(characterId);
            view.IsActiveCompoundCandidate = candidate;

            if (view.ActiveCompoundButton != null)
                view.ActiveCompoundButton.interactable = candidate;

            if (view.ActiveCompoundBackImage != null)
            {
                if (candidate)
                {
                    view.ActiveCompoundBackImage.enabled = true;
                }
                else
                {
                    view.ActiveCompoundBackImage.color = view.ActiveCompoundBackOriginalColor;
                    view.ActiveCompoundBackImage.enabled = view.ActiveCompoundBackOriginalEnabled;
                }
            }
        }
    }

    private void UpdateActiveCompoundCandidatePulse()
    {
        if (string.IsNullOrWhiteSpace(selectedCompoundId))
            return;

        float pulse = (Mathf.Sin(Time.unscaledTime * activeCandidatePulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(activeCandidatePulseMinAlpha, activeCandidatePulseMaxAlpha, pulse);

        for (int i = 0; i < CharacterCount; i++)
        {
            CharacterView view = characterViews[i];
            if (view?.ActiveCompoundBackImage == null || !view.IsActiveCompoundCandidate)
                continue;

            Color baseColor = view.ActiveCompoundBackOriginalColor;
            baseColor.a = alpha;
            view.ActiveCompoundBackImage.enabled = true;
            view.ActiveCompoundBackImage.color = baseColor;
        }
    }

    private void TryEquipSelectedCompoundToCharacter(int partySlotIndex)
    {
        if (string.IsNullOrWhiteSpace(selectedCompoundId))
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

        Transform relicRoot = FindChildRecursive(searchRoot, "Relic");
        if (relicRoot == null)
            return;

        Transform itemImageRoot = FindChildRecursive(relicRoot, "Itemimage");
        Transform lineRoot = itemImageRoot != null
            ? FindChildRecursive(itemImageRoot, "Line2")
            : null;
        ownedRelicView = new OwnedRelicView
        {
            Root = relicRoot,
            ItemButton = itemImageRoot != null ? itemImageRoot.GetComponent<Button>() : null,
            LineObject = lineRoot != null ? lineRoot.gameObject : null,
            IconImage = itemImageRoot != null
                ? FindImageByNames(itemImageRoot, "Icon") ?? itemImageRoot.GetComponent<Image>()
                : FindImageByNames(relicRoot, "Icon"),
            NameText = FindTextByNames(relicRoot, "Name"),
            RarityText = FindTextByNames(relicRoot, "Rarity"),
            EffectText = FindTextByNames(relicRoot, "Effect")
        };

        if (ownedRelicView.ItemButton != null)
        {
            ownedRelicView.ItemButton.onClick.RemoveListener(ToggleOwnedRelicSelection);
            ownedRelicView.ItemButton.onClick.AddListener(ToggleOwnedRelicSelection);
        }

        SetOwnedRelicLineActive(false);
    }

    private void ResolveCharacterViewsIfNeeded()
    {
        if (!autoBindCharacterHierarchy)
            return;

        ResolveSlideTargets();
        Transform searchRoot = charterRect != null ? charterRect : ResolvePanelRoot()?.transform;
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

        CharacterView view = new CharacterView
        {
            Root = root,
            NameText = FindTextByNames(root, "Name"),
            Mark1Image = FindImageByNames(root, "mark1", "Mark1"),
            Mark2Image = FindImageByNames(root, "mark2", "Mark2")
        };

        Transform activeRoot = root.Find("Active") ?? FindChildRecursive(root, "Active");
        if (activeRoot != null)
        {
            view.ActiveCompoundIcon = FindImageByNames(activeRoot, "Icon");
            view.ActiveCompoundBackImage = FindImageByNames(activeRoot, "Back") ?? activeRoot.GetComponent<Image>();
            view.ActiveCompoundBackOriginalColor = view.ActiveCompoundBackImage != null
                ? view.ActiveCompoundBackImage.color
                : Color.white;
            view.ActiveCompoundBackOriginalEnabled = view.ActiveCompoundBackImage != null &&
                                                     view.ActiveCompoundBackImage.enabled;

            Button activeButton = activeRoot.GetComponent<Button>();
            if (activeButton == null)
                activeButton = activeRoot.gameObject.AddComponent<Button>();

            activeButton.transition = Selectable.Transition.None;
            if (view.ActiveCompoundBackImage != null)
                activeButton.targetGraphic = view.ActiveCompoundBackImage;
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

            // 후보 숨쉬기 색상은 실제로 화면에 보이는 슬롯 배경 Image에 적용합니다.
            // Button.targetGraphic이 따로 지정되어 있거나 슬롯 루트가 아닌 자식 Image를
            // 사용하는 구조도 대응하되, 실제 유물 Icon은 후보 색상 대상으로 사용하지 않습니다.
            Image slotBackgroundImage = ResolveRelicSlotBackgroundImage(slotRoot, iconImage, slotButton);

            slotButton.transition = Selectable.Transition.None;
            if (slotBackgroundImage != null)
                slotButton.targetGraphic = slotBackgroundImage;
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
                SlotBackgroundImage = slotBackgroundImage,
                OriginalBackgroundColor = slotBackgroundImage != null ? slotBackgroundImage.color : Color.white,
                OriginalBackgroundEnabled = slotBackgroundImage != null && slotBackgroundImage.enabled,
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

    private static Image ResolveRelicSlotBackgroundImage(
        Transform slotRoot,
        Image iconImage,
        Button slotButton)
    {
        if (slotRoot == null)
            return null;

        // 이미 Button이 실제 표시 Graphic을 가리키고 있다면 그 Image를 우선 사용합니다.
        Image buttonTargetImage = slotButton != null
            ? slotButton.targetGraphic as Image
            : null;
        if (buttonTargetImage != null && buttonTargetImage != iconImage)
            return buttonTargetImage;

        // 일반적인 구조: Relic01~06 루트에 Image가 있습니다.
        Image rootImage = slotRoot.GetComponent<Image>();
        if (rootImage != null && rootImage != iconImage)
            return rootImage;

        // 프리팹 구조가 달라 루트가 아닌 자식에 배경 Image가 있는 경우를 보정합니다.
        Image[] childImages = slotRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < childImages.Length; i++)
        {
            Image image = childImages[i];
            if (image == null || image == iconImage)
                continue;

            if (string.Equals(image.gameObject.name, "Icon", StringComparison.OrdinalIgnoreCase))
                continue;

            return image;
        }

        return null;
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

    private static LobbyEquipPanelUI FindEquipPanelOwner(Transform child)
    {
        return child != null ? child.GetComponentInParent<LobbyEquipPanelUI>(true) : null;
    }

    private GameObject ResolvePanelRoot()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        return panelRoot;
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
    }

    [Serializable]
    private sealed class OwnedRelicView
    {
        public Transform Root;
        public Button ItemButton;
        public GameObject LineObject;
        public Image IconImage;
        public TMP_Text NameText;
        public TMP_Text RarityText;
        public TMP_Text EffectText;
    }

    [Serializable]
    private sealed class RelicSlotView
    {
        public Transform Root;
        public Image SlotBackgroundImage;
        public Color OriginalBackgroundColor;
        public bool OriginalBackgroundEnabled;
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
        public Image ActiveCompoundIcon;
        public Image ActiveCompoundBackImage;
        public Color ActiveCompoundBackOriginalColor = Color.white;
        public bool ActiveCompoundBackOriginalEnabled;
        public Button ActiveCompoundButton;
        public bool IsActiveCompoundCandidate;
        public RelicSlotView[] RelicSlots = new RelicSlotView[VisibleRelicSlotCount];
        public Image[] SkillIcons = new Image[VisibleSkillSlotCount];
    }
}
