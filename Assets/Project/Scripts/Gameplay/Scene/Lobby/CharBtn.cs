using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Relic.Gameplay.Data;

public class CharBtn : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Character")]
    [SerializeField] private CharacterType characterType;
    [SerializeField] private string characterId;

    [Header("Lock")]
    [SerializeField] private bool isLocked;

    [Header("Option")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    [Header("Legacy Direct Register")]

    [Header("Selected Party Marker")]
    [SerializeField] private bool showSelectedPartyMarker = true;
    [SerializeField] private GameObject selectedPartyMarkerRoot;
    [SerializeField] private Image selectedPartyMarkerImage;
    [SerializeField] private TMP_Text selectedPartyMarkerText;
    [SerializeField] private string selectedPartyTextFormat = "{0}";

    [Header("Character Select Icon")]
    [SerializeField] private Image characterSelectIconImage;

    [Header("Character Select Hover")]
    [FormerlySerializedAs("jobmarkHoverScale")]
    [SerializeField] private float charBtnHoverScale = 1.15f;
    [FormerlySerializedAs("jobmarkHoverTransitionDuration")]
    [SerializeField] private float charBtnHoverTransitionDuration = 0.15f;
    [Tooltip("Info_Panel의 CharBtn Back에 마우스를 올렸을 때 사용할 색입니다.")]
    [SerializeField] private Color infoPanelHoverBackgroundColor = new Color32(0x3C, 0x44, 0x76, 0xFF);

    [Header("Info Panel Character Button")]
    [Tooltip("Char/Image. 비워두면 새 Info_Panel 구조에서 자동으로 찾습니다.")]
    [SerializeField] private Image infoPanelCharacterImage;
    [Tooltip("Idle / Battle Idle 프레임은 CharacterIconDatabase에서 CharacterId 기준으로 자동으로 불러옵니다.")]
    [SerializeField] private bool useCharacterIconDatabaseIdleFrames = true;
    [Tooltip("Idle / Battle Idle 애니메이션의 한 프레임 유지 시간입니다.")]
    [SerializeField, Min(0.01f)] private float infoPanelIdleFrameInterval = 0.12f;
    [Tooltip("CharBtn/Relic. 파티 선택 전에는 비활성화되고 선택되면 활성화됩니다.")]
    [SerializeField] private GameObject infoPanelRelicRoot;
    [Tooltip("CharBtn/Compound. 파티 선택 전에는 비활성화되고 선택되면 활성화됩니다.")]
    [SerializeField] private GameObject infoPanelCompoundRoot;

    [Header("Character Select State")]
    [SerializeField] private float viewedCharacterFixedScale = 1.2f;
    [SerializeField] private Image jobmarkImage;
    [SerializeField] private Image jobmarkInImage;
    [SerializeField] private Color viewedJobmarkColor = Color.white;
    [SerializeField] private Color lockedJobmarkColor = new Color32(0x77, 0x77, 0x77, 0xFF);

    [Header("현재 보고 있는 캐릭터 표시 (Legacy)")]
    [SerializeField] private RectTransform viewedCharacterBorder;
    [SerializeField] private string viewedCharacterBorderName = "BorderImg1";
    [SerializeField] private RectTransform[] viewedCharacterBorders;
    [SerializeField] private string[] viewedCharacterBorderNames = { "BorderImg1", "BorderImg2" };
    [SerializeField, Range(0f, 1f)] private float remoteViewedCharacterAlpha = 0.45f;
    [SerializeField] private float viewedCharacterRotationZ = -10f;
    [SerializeField] private float viewedCharacterScale = 1.2f;
    [SerializeField] private float viewedCharacterTransitionDuration = 0.2f;

    private CharPick charPick;
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Button characterButton;
    private static readonly Color ViewedCharacterSelectedColor = new Color32(0x4E, 0x66, 0xDF, 0xFF);

    private ColorBlock originalButtonColors;
    private bool hasOriginalButtonColors;
    private bool isViewedCharacter;
    private bool isRemoteViewedCharacter;
    private int lastHandledClickFrame = -1;
    private int suppressClickUntilFrame = -1;
    private bool isReadyPanelCharacterDragging;

    private readonly List<RectTransform> viewedCharacterBorderTargets = new();
    private readonly List<Quaternion> viewedCharacterBorderOriginalRotations = new();
    private readonly List<Graphic> viewedCharacterBorderGraphics = new();
    private readonly List<Color> viewedCharacterBorderOriginalColors = new();
    private Vector3 viewedCharacterOriginalScale = Vector3.one;
    private bool hasViewedCharacterOriginalValues;
    private Coroutine viewedCharacterTransitionCoroutine;
    private Vector3 charBtnOriginalScale = Vector3.one;
    private bool hasCharBtnOriginalScale;
    private Coroutine charBtnHoverCoroutine;
    private bool isCharBtnHovered;
    private Color jobmarkOriginalColor = Color.white;
    private Color jobmarkInOriginalColor = Color.white;
    private bool hasJobmarkOriginalColors;
    private Image infoPanelHoverBackgroundImage;
    private Color infoPanelHoverBackgroundOriginalColor = Color.white;
    private bool hasInfoPanelHoverBackgroundOriginalColor;
    private Coroutine infoPanelIdleAnimationCoroutine;
    private bool infoPanelSelectedForParty;

    public CharacterType CharacterType => characterType;
    public string CharacterId => characterId;
    public RectTransform Rect => rect;
    public bool IsLocked => isLocked;
    public bool IsInfoPanelReadyDragSource => IsInfoPanelPartyEditButton();


    public bool TryGetReadyDragPreview(out Sprite sprite, out Vector2 size)
    {
        sprite = null;
        size = new Vector2(96f, 96f);

        if (!IsInfoPanelPartyEditButton() || isLocked || string.IsNullOrWhiteSpace(characterId))
            return false;

        AutoPrepareInfoPanelCharacterButtonReferences();

        CharacterIconDatabase database = DataManager.Instance?.CharacterIconDatabase;
        if (database == null || !database.TryGetLobbyIdleFrames(characterId, out Sprite[] frames))
            return false;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null)
                continue;

            sprite = frames[i];
            break;
        }

        if (sprite == null)
            return false;

        if (infoPanelCharacterImage != null)
        {
            Rect rect = infoPanelCharacterImage.rectTransform.rect;
            if (rect.width > 0f && rect.height > 0f)
                size = rect.size;
        }

        return true;
    }

    public bool TryGetOrRegisterPartySlotForReadyDrag(out int partySlotIndex)
    {
        partySlotIndex = -1;

        if (!IsInfoPanelPartyEditButton() || isLocked || string.IsNullOrWhiteSpace(characterId))
            return false;

        if (charPick != null)
            return charPick.TryGetOrRegisterPartySlotForReadyDrag(this, out partySlotIndex);

        if (!PrepareCharacterForPartyAction(false) || DataManager.Instance?.PartyRuntimeStore == null)
            return false;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        partySlotIndex = partyStore.FindCharacterSlot(characterId);
        if (partySlotIndex >= 0)
            return true;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (!string.IsNullOrWhiteSpace(partyStore.GetCharacterId(i)))
                continue;

            if (!partyStore.SetCharacter(i, characterId))
                return false;

            partySlotIndex = i;
            RefreshPartyViews();
            LobbyInfoPanelUI.RefreshAll();
            LobbyEquipPanelUI.RefreshAllCharacterData();
            LobbyPartyCharacterSettingOpenButton.RefreshAll();
            return true;
        }

        return false;
    }

    public bool TryRegisterToReadyGrid(int gridIndex, out int partySlotIndex)
    {
        partySlotIndex = -1;

        if (!IsInfoPanelPartyEditButton() || isLocked || string.IsNullOrWhiteSpace(characterId))
            return false;

        if (charPick != null)
            return charPick.TryRegisterCharacterToReadyGrid(this, gridIndex, out partySlotIndex);

        if (!PrepareCharacterForPartyAction(false) || DataManager.Instance?.PartyRuntimeStore == null)
            return false;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        partySlotIndex = partyStore.FindCharacterSlot(characterId);
        bool newlyRegistered = partySlotIndex < 0;

        if (newlyRegistered)
        {
            for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
            {
                if (!string.IsNullOrWhiteSpace(partyStore.GetCharacterId(i)))
                    continue;

                if (!partyStore.SetCharacter(i, characterId))
                    return false;

                partySlotIndex = i;
                break;
            }
        }

        if (partySlotIndex < 0 || !partyStore.SetSpawnGridIndex(partySlotIndex, gridIndex))
        {
            if (newlyRegistered && partySlotIndex >= 0)
                partyStore.ClearSlot(partySlotIndex);

            partySlotIndex = -1;
            return false;
        }

        RefreshPartyViews();
        RefreshSelectedPartyMarker();
        LobbyInfoPanelUI.RefreshAll();
        LobbyEquipPanelUI.RefreshAllCharacterData();
        LobbyPartyCharacterSettingOpenButton.RefreshAll();
        return true;
    }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        characterButton = GetComponent<Button>();

        if (characterButton != null)
        {
            originalButtonColors = characterButton.colors;
            hasOriginalButtonColors = true;
        }

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        AutoPrepareSelectedPartyMarkerReferences();
        AutoPrepareCharacterSelectIcon();
        RefreshCharacterSelectIcon();
        AutoPrepareJobmarkReferences();
        CacheJobmarkOriginalColors();
        AutoPrepareInfoPanelHoverBackground();
        CacheInfoPanelHoverBackgroundColor();
        AutoPrepareInfoPanelCharacterButtonReferences();
        CacheInfoPanelIdleImage();
        CacheCharBtnOriginalScale();
        AutoPrepareViewedCharacterBorder();
        CacheViewedCharacterOriginalValues();
        RefreshSelectedPartyMarker();
    }

    private void OnEnable()
    {
        AutoPrepareCharacterSelectIcon();
        RefreshCharacterSelectIcon();
        AutoPrepareJobmarkReferences();
        CacheJobmarkOriginalColors();
        AutoPrepareInfoPanelHoverBackground();
        CacheInfoPanelHoverBackgroundColor();
        AutoPrepareInfoPanelCharacterButtonReferences();
        CacheInfoPanelIdleImage();
        CacheCharBtnOriginalScale();
        isCharBtnHovered = false;
        AutoPrepareViewedCharacterBorder();
        CacheViewedCharacterOriginalValues();
        RefreshSelectedPartyMarker();
        SetNetworkViewedCharacterState(isViewedCharacter, isRemoteViewedCharacter, true);
    }

    private void OnDisable()
    {
        StopInfoPanelIdleAnimation();

        if (charBtnHoverCoroutine != null)
        {
            StopCoroutine(charBtnHoverCoroutine);
            charBtnHoverCoroutine = null;
        }

        CacheCharBtnOriginalScale();

        if (rect != null)
            rect.localScale = charBtnOriginalScale;

        if (viewedCharacterTransitionCoroutine != null)
        {
            StopCoroutine(viewedCharacterTransitionCoroutine);
            viewedCharacterTransitionCoroutine = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoPrepareSelectedPartyMarkerReferences();
        AutoPrepareCharacterSelectIcon();
        AutoPrepareJobmarkReferences();
        AutoPrepareInfoPanelHoverBackground();
        AutoPrepareInfoPanelCharacterButtonReferences();
        AutoPrepareViewedCharacterBorder();
    }
#endif

    public void Init(CharPick pick)
    {
        charPick = pick;

        SetCenter(false);
        SetVisible(false);
        RefreshCharacterSelectIcon();
        RefreshSelectedPartyMarker();
        ApplyJobmarkState();
        RefreshCharacterSelectScale(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.frameCount <= suppressClickUntilFrame)
            return;

        // 새 Info_Panel에서는 CharBtn 전체가 캐릭터 선택 버튼이지만,
        // Relic/Compound 영역 클릭은 캐릭터 선택보다 장착/해제 입력이 우선입니다.
        // 자식 Image의 raycastTarget 설정에 따라 클릭이 CharBtn까지 올라오는 경우에도
        // 실제 포인터 위치를 검사해 장착 슬롯 클릭으로 전달합니다.
        if (TryHandleInfoPanelEquipmentAreaClick(eventData))
            return;

        NotifyClickToCharPickOrExecuteDirect();
    }

    private bool TryHandleInfoPanelEquipmentAreaClick(PointerEventData eventData)
    {
        if (!IsInfoPanelPartyEditButton() || eventData == null || string.IsNullOrWhiteSpace(characterId))
            return false;

        Transform relicRoot = transform.Find("Relic");
        if (IsPointerInsideRect(relicRoot as RectTransform, eventData))
            return ForwardInfoEquipmentSlotClick(isCompound: false);

        Transform compoundRoot = transform.Find("Compound");
        if (IsPointerInsideRect(compoundRoot as RectTransform, eventData))
            return ForwardInfoEquipmentSlotClick(isCompound: true);

        return false;
    }

    private static bool IsPointerInsideRect(RectTransform rect, PointerEventData eventData)
    {
        if (rect == null || eventData == null || !rect.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera != null ? canvas.worldCamera : eventData.pressEventCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventCamera);
    }

    private bool ForwardInfoEquipmentSlotClick(bool isCompound)
    {
        LobbyEquipPanelUI equipPanel = GetComponentInParent<LobbyEquipPanelUI>(true);
        if (equipPanel == null)
            equipPanel = FindFirstObjectByType<LobbyEquipPanelUI>(FindObjectsInactive.Include);

        return equipPanel != null &&
               equipPanel.HandleInfoCharacterEquipmentSlotClick(characterId, isCompound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLocked)
            return;

        if (IsInfoPanelPartyEditButton())
            ApplyInfoPanelHoverBackground(true);
        else
            SetCharBtnHover(true);

        if (charPick != null)
            charPick.PointerEnterButton(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsInfoPanelPartyEditButton())
            ApplyInfoPanelHoverBackground(false);
        else
            SetCharBtnHover(false);

        if (isLocked)
            return;

        if (charPick != null)
            charPick.PointerExitButton(this);
    }

    public void Execute()
    {
        Execute(false);
    }

    public void Execute(bool playClickSoundBeforeAction)
    {
        NotifyClickToCharPickOrExecuteDirect(playClickSoundBeforeAction);
    }

    private void NotifyClickToCharPickOrExecuteDirect()
    {
        NotifyClickToCharPickOrExecuteDirect(false);
    }

    private void NotifyClickToCharPickOrExecuteDirect(bool playClickSoundBeforeAction)
    {
        if (lastHandledClickFrame == Time.frameCount)
            return;

        lastHandledClickFrame = Time.frameCount;

        if (playClickSoundBeforeAction)
            PlayClickSound();

        if (charPick != null)
        {
            charPick.ClickBtn(this, !playClickSoundBeforeAction);
            return;
        }

        if (playClickSoundBeforeAction)
            ConfirmCharacterToSelectedPartySlotDirectly(false);
        else
            ConfirmCharacterToSelectedPartySlotDirectly();
    }

    public bool PrepareCharacterForPartyAction(bool withClickSound)
    {
        if (isLocked)
        {
            Debug.Log("[CharBtn] 잠긴 캐릭터입니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning("[CharBtn] CharacterId is empty.");
            return false;
        }

        if (withClickSound)
            PlayClickSound();

        CreateOrUpdateRuntimeData();
        SelectCharacterState();
        return true;
    }

    public void ConfirmCharacterToParty()
    {
        if (charPick != null)
        {
            charPick.ToggleButtonPartyMarker(this);
            return;
        }

        ConfirmCharacterToSelectedPartySlotDirectly();
    }

    private void ConfirmCharacterToSelectedPartySlotDirectly()
    {
        ConfirmCharacterToSelectedPartySlotDirectly(true);
    }

    private void ConfirmCharacterToSelectedPartySlotDirectly(bool withClickSound)
    {
        if (!PrepareCharacterForPartyAction(withClickSound))
            return;

        SaveCharacterToSelectedPartySlot();
        RefreshPartyViews();
    }

    private void PlayClickSound()
    {
        if (!playClickSound)
            return;

        UIPanelButton panelButton = GetComponent<UIPanelButton>();

        if (panelButton == null)
            panelButton = GetComponentInChildren<UIPanelButton>(true);

        if (panelButton == null)
            panelButton = GetComponentInParent<UIPanelButton>();

        if (panelButton != null)
        {
            panelButton.PlayClickSoundOnly();
            return;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(clickSfx);
    }

    private bool SelectCharacterState()
    {
        if (CharacterSelectionState.Instance == null)
        {
            Debug.LogWarning("[CharBtn] CharacterSelectionState instance is missing.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            Debug.LogWarning("[CharBtn] CharacterId is empty.");
            return false;
        }

        CharacterSelectionState.Instance.SelectCharacter(characterType, characterId);
        return true;
    }

    private void CreateOrUpdateRuntimeData()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharBtn] DataManager instance is missing.");
            return;
        }

        if (!DataManager.Instance.CharacterDatabase.TryGet(characterId, out var master))
        {
            Debug.LogWarning($"[CharBtn] Character master not found: {characterId}");
            return;
        }

        var runtimeStore = DataManager.Instance.CharacterRuntimeStore;

        if (runtimeStore.TryGet(characterId, out var runtime))
        {
            CharacterStartingRelicUtility.EnsureStartingRelicEquippedIfEmpty(
                runtime,
                master,
                DataManager.Instance.RelicDatabase);
            return;
        }

        runtime = new CharacterRuntimeData
        {
            CharacterId = master.CharacterId,
            Level = 1,
            Exp = 0,

            CurrentHP = master.MaxHP,
            CurrentCost = master.MaxCost,
            CurrentResource = 0,
            CurrentMoveLevel = 0,

            IsUnlocked = master.IsDefaultProvided,

            MoveSkillId = "S_Move_1",
            PassiveSkillId = master.PassiveSkill1,
            UniqueSkillId = master.UniqueSkill1,
            AbilitySkillId = master.CharacterSkill1,

            EquippedSkillIds = new string[4]
            {
                master.UniqueSkill1,
                master.CharacterSkill1,
                "",
                ""
            },

            EquippedRuneIds = new string[6],
            EquippedRelicIds = CharacterStartingRelicUtility.CreateStartingRelicSlots(master)
        };

        CharacterStartingRelicUtility.InitializeActiveRelicUses(
            runtime,
            DataManager.Instance.RelicDatabase);

        runtimeStore.AddOrUpdate(runtime);
    }

    private void SaveCharacterToSelectedPartySlot()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[CharBtn] DataManager instance is missing.");
            return;
        }

        if (CharacterSelectionState.Instance == null)
        {
            Debug.LogWarning("[CharBtn] CharacterSelectionState instance is missing.");
            return;
        }

        var partyStore = DataManager.Instance.PartyRuntimeStore;
        int selectedSlot = CharacterSelectionState.Instance.CurrentPartySlotIndex;

        SteamLobbyPartySynchronizer synchronizer = SteamLobbyPartySynchronizer.Instance;

        if (synchronizer != null && synchronizer.IsNetworkPartyActive)
        {
            synchronizer.RequestAutomaticCharacterToggle(characterId);
            return;
        }

        if (selectedSlot < 0 || selectedSlot >= partyStore.MaxPartyCountValue)
        {
            Debug.LogWarning("[Party] 선택된 파티 슬롯이 없습니다.");
            return;
        }

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (i == selectedSlot)
                continue;

            if (partyStore.GetCharacterId(i) != characterId)
                continue;

            partyStore.ClearSlot(i);
        }

        string previousCharacterId = partyStore.GetCharacterId(selectedSlot);
        bool characterChanged = !string.IsNullOrWhiteSpace(previousCharacterId) &&
                                !string.Equals(previousCharacterId, characterId, System.StringComparison.Ordinal);

        if (characterChanged)
        {
            LobbyCharacterEquipmentReleaseUtility.ReleaseAll(previousCharacterId);
            // 캐릭터가 교체되면 이전 캐릭터의 시작 위치를 이어받지 않습니다.
            // 새 캐릭터는 Info_Panel에서 선택한 뒤 Ready_Panel의 Grid01~15를 직접 눌러 배치합니다.
            partyStore.ClearSlot(selectedSlot);
        }

        bool success = partyStore.SetCharacter(selectedSlot, characterId);

        if (!success)
            return;

        Debug.Log(
            $"[Party] Set Slot / CharacterId:{characterId} / " +
            $"Slot:{selectedSlot} / Grid:{partyStore.GetSpawnGridIndex(selectedSlot)}"
        );
    }

    private void RefreshPartyViews()
    {
        SpawnGridPanel[] spawnGridPanels = FindObjectsByType<SpawnGridPanel>(FindObjectsSortMode.None);

        for (int i = 0; i < spawnGridPanels.Length; i++)
        {
            if (spawnGridPanels[i] == null)
                continue;

            spawnGridPanels[i].Refresh();
        }

        CharBtn[] charButtons = FindObjectsByType<CharBtn>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < charButtons.Length; i++)
        {
            if (charButtons[i] != null)
                charButtons[i].RefreshSelectedPartyMarker();
        }
    }

    public void RefreshSelectedPartyMarker()
    {
        AutoPrepareSelectedPartyMarkerReferences();
        RefreshNetworkAvailability();
        RefreshNetworkViewedCharacterState(true);

        int registeredSlot = FindDisplayedPartySlot();
        bool isRegistered = registeredSlot >= 0;

        RefreshInfoPanelPartyEditVisualState(isRegistered);

        if (!showSelectedPartyMarker)
        {
            SetSelectedPartyMarkerActive(false);
            return;
        }

        SetSelectedPartyMarkerActive(isRegistered);

        if (!isRegistered)
            return;

        RefreshSelectedPartyMarkerText(registeredSlot);
    }

    private int FindDisplayedPartySlot()
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return -1;

        SteamLobbyPartySynchronizer synchronizer = SteamLobbyPartySynchronizer.Instance;

        if (synchronizer != null && synchronizer.IsNetworkPartyActive)
            return synchronizer.FindDisplayedCharacterSlot(characterId);

        if (charPick != null)
            return charPick.FindPendingPartySlot(characterId);

        if (DataManager.Instance == null)
            return -1;

        return DataManager.Instance.PartyRuntimeStore.FindCharacterSlot(characterId);
    }

    private void SetSelectedPartyMarkerActive(bool active)
    {
        if (selectedPartyMarkerRoot != null)
            selectedPartyMarkerRoot.SetActive(active);

        if (selectedPartyMarkerImage != null)
            selectedPartyMarkerImage.enabled = active;

        if (selectedPartyMarkerText != null)
            selectedPartyMarkerText.enabled = active;
    }

    private void RefreshSelectedPartyMarkerText(int registeredSlot)
    {
        if (selectedPartyMarkerText == null)
            return;

        string format = string.IsNullOrWhiteSpace(selectedPartyTextFormat)
            ? "{0}"
            : selectedPartyTextFormat;

        int displaySlotNumber = registeredSlot + 1;
        selectedPartyMarkerText.text = string.Format(format, displaySlotNumber);
        selectedPartyMarkerText.enabled = true;
    }

    private void AutoPrepareSelectedPartyMarkerReferences()
    {
        if (selectedPartyMarkerRoot == null)
        {
            Transform marker = transform.Find("SelectedPartyMarker");

            if (marker == null)
                marker = transform.Find("SelectedMarker");

            if (marker != null)
                selectedPartyMarkerRoot = marker.gameObject;
        }

        Transform markerRootTransform = selectedPartyMarkerRoot != null
            ? selectedPartyMarkerRoot.transform
            : transform;

        if (selectedPartyMarkerImage == null && markerRootTransform != null)
            selectedPartyMarkerImage = markerRootTransform.GetComponentInChildren<Image>(true);

        if (selectedPartyMarkerText == null && markerRootTransform != null)
            selectedPartyMarkerText = markerRootTransform.GetComponentInChildren<TMP_Text>(true);
    }

    private void AutoPrepareCharacterSelectIcon()
    {
        if (characterSelectIconImage != null)
            return;

        Transform iconRoot = transform.Find("Icon");
        if (iconRoot == null)
            return;

        Transform charMask = iconRoot.Find("CharMask");
        if (charMask == null)
            return;

        Transform icon = charMask.Find("Icon");
        if (icon != null)
            characterSelectIconImage = icon.GetComponent<Image>();
    }

    public void RefreshCharacterSelectIcon()
    {
        AutoPrepareCharacterSelectIcon();

        if (characterSelectIconImage == null)
            return;

        Sprite sprite = null;

        if (!string.IsNullOrWhiteSpace(characterId) &&
            DataManager.Instance != null &&
            DataManager.Instance.CharacterIconDatabase != null)
        {
            DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out sprite);
        }

        characterSelectIconImage.sprite = sprite;
        characterSelectIconImage.enabled = sprite != null;
    }

    private void CacheCharBtnOriginalScale()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (rect == null || hasCharBtnOriginalScale)
            return;

        charBtnOriginalScale = rect.localScale;
        hasCharBtnOriginalScale = true;
    }

    private void SetCharBtnHover(bool hovered)
    {
        CacheCharBtnOriginalScale();

        if (rect == null)
            return;

        isCharBtnHovered = hovered && !isLocked;

        if (charBtnHoverCoroutine != null)
        {
            StopCoroutine(charBtnHoverCoroutine);
            charBtnHoverCoroutine = null;
        }

        float scaleMultiplier;

        if (IsInfoPanelPartyEditButton())
            scaleMultiplier = 1f;
        else if (isViewedCharacter)
            scaleMultiplier = Mathf.Max(0f, viewedCharacterFixedScale);
        else if (isCharBtnHovered)
            scaleMultiplier = Mathf.Max(0f, charBtnHoverScale);
        else
            scaleMultiplier = 1f;

        Vector3 targetScale = charBtnOriginalScale * scaleMultiplier;

        if (!isActiveAndEnabled || charBtnHoverTransitionDuration <= 0f)
        {
            rect.localScale = targetScale;
            return;
        }

        charBtnHoverCoroutine = StartCoroutine(AnimateCharBtnHoverRoutine(targetScale));
    }

    private IEnumerator AnimateCharBtnHoverRoutine(Vector3 targetScale)
    {
        Vector3 startScale = rect.localScale;
        float duration = Mathf.Max(0.01f, charBtnHoverTransitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            rect.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            yield return null;
        }

        rect.localScale = targetScale;
        charBtnHoverCoroutine = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isReadyPanelCharacterDragging = false;

        if (IsInfoPanelPartyEditButton() && !isLocked && !string.IsNullOrWhiteSpace(characterId))
        {
            SpawnGridPanel[] panels = FindObjectsByType<SpawnGridPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                    panels[i].BeginInfoCharacterDrag(this, eventData);
            }

            isReadyPanelCharacterDragging = true;
        }

        charPick?.BeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isReadyPanelCharacterDragging)
        {
            SpawnGridPanel[] panels = FindObjectsByType<SpawnGridPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                    panels[i].UpdateDragPreview(eventData);
            }
        }

        charPick?.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isReadyPanelCharacterDragging)
        {
            SpawnGridPanel[] panels = FindObjectsByType<SpawnGridPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                    panels[i].EndInfoCharacterDrag(this);
            }

            suppressClickUntilFrame = Time.frameCount + 1;
            isReadyPanelCharacterDragging = false;
        }

        charPick?.EndDrag(eventData);
    }

    public void SetCenter(bool isCenter)
    {
        // 고정형 버튼 배치에서는 중앙 정렬 효과를 사용하지 않는다.
    }

    /// <summary>
    /// 현재 정보를 보고 있는 캐릭터인지 표시한다.
    /// 선택된 버튼은 BorderImg1이 부드럽게 Z -10도까지 회전하고,
    /// 버튼 전체의 X/Y 스케일이 1.1까지 커진다.
    /// 다른 버튼은 원래 회전값과 크기로 돌아간다.
    /// </summary>
    public void SetViewedCharacter(bool isViewed, bool immediate = false)
    {
        SetNetworkViewedCharacterState(isViewed, false, immediate);
    }

    public void SetNetworkViewedCharacterState(
        bool isLocalViewed,
        bool isRemoteViewed,
        bool immediate = false)
    {
        isViewedCharacter = isLocalViewed;
        isRemoteViewedCharacter = !isLocalViewed && isRemoteViewed;

        AutoPrepareJobmarkReferences();
        CacheJobmarkOriginalColors();
        ApplyJobmarkState();
        ApplyViewedCharacterBorderAlpha(isRemoteViewedCharacter);
        RefreshCharacterSelectScale(immediate);
    }

    private void AutoPrepareJobmarkReferences()
    {
        if (jobmarkImage == null)
        {
            Transform jobmark = transform.Find("jobmark");
            if (jobmark == null)
                jobmark = FindChildByExactName(transform, "jobmark");

            if (jobmark != null)
                jobmarkImage = jobmark.GetComponent<Image>();
        }

        if (jobmarkInImage == null)
        {
            Transform jobmarkIn = null;

            if (jobmarkImage != null)
                jobmarkIn = jobmarkImage.transform.Find("jobmark_in");

            if (jobmarkIn == null)
                jobmarkIn = FindChildByExactName(transform, "jobmark_in");

            if (jobmarkIn != null)
                jobmarkInImage = jobmarkIn.GetComponent<Image>();
        }
    }

    private static Transform FindChildByExactName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == targetName)
                return child;
        }

        return null;
    }

    private void CacheJobmarkOriginalColors()
    {
        if (hasJobmarkOriginalColors)
            return;

        AutoPrepareJobmarkReferences();

        if (jobmarkImage == null && jobmarkInImage == null)
            return;

        if (jobmarkImage != null)
            jobmarkOriginalColor = jobmarkImage.color;

        if (jobmarkInImage != null)
            jobmarkInOriginalColor = jobmarkInImage.color;

        hasJobmarkOriginalColors = true;
    }

    private void ApplyJobmarkState()
    {
        CacheJobmarkOriginalColors();

        if (jobmarkImage != null)
        {
            bool useViewedState = isViewedCharacter && !IsInfoPanelPartyEditButton();
            Color targetColor = isLocked
                ? lockedJobmarkColor
                : useViewedState
                    ? viewedJobmarkColor
                    : jobmarkOriginalColor;

            jobmarkImage.color = WithPreservedAlpha(targetColor, jobmarkImage.color.a);
        }

        if (jobmarkInImage != null)
        {
            bool useViewedState = isViewedCharacter && !IsInfoPanelPartyEditButton();
            Color targetColor = isLocked
                ? lockedJobmarkColor
                : useViewedState
                    ? viewedJobmarkColor
                    : jobmarkInOriginalColor;

            jobmarkInImage.color = WithPreservedAlpha(targetColor, jobmarkInImage.color.a);
        }
    }

    private bool IsInfoPanelPartyEditButton()
    {
        Transform current = transform;

        while (current != null)
        {
            if (string.Equals(current.name, "Info_Panel", System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(current.name, "CharacterSettingPanel", System.StringComparison.OrdinalIgnoreCase))
                return false;

            current = current.parent;
        }

        return false;
    }

    private void AutoPrepareInfoPanelHoverBackground()
    {
        if (infoPanelHoverBackgroundImage != null || !IsInfoPanelPartyEditButton())
            return;

        // 새 Info_Panel 구조: CharacterSelect/CharBtn_0~4/Back
        Transform background = transform.Find("Back");

        // 이전 구조 호환
        if (background == null)
            background = transform.Find("Icon/Background");
        if (background == null)
            background = FindChildByExactName(transform, "Background");

        if (background != null)
            infoPanelHoverBackgroundImage = background.GetComponent<Image>();
    }

    private void CacheInfoPanelHoverBackgroundColor()
    {
        if (hasInfoPanelHoverBackgroundOriginalColor)
            return;

        AutoPrepareInfoPanelHoverBackground();
        if (infoPanelHoverBackgroundImage == null)
            return;

        infoPanelHoverBackgroundOriginalColor = infoPanelHoverBackgroundImage.color;
        hasInfoPanelHoverBackgroundOriginalColor = true;
    }

    private void ApplyInfoPanelHoverBackground(bool hovered)
    {
        CacheInfoPanelHoverBackgroundColor();
        if (infoPanelHoverBackgroundImage == null)
            return;

        // 선택된 캐릭터는 포인터가 빠져도 Back의 선택 색상을 유지합니다.
        // 미선택 상태에서만 호버가 끝나면 원래 색으로 돌아갑니다.
        Color target = (hovered || infoPanelSelectedForParty)
            ? infoPanelHoverBackgroundColor
            : infoPanelHoverBackgroundOriginalColor;
        infoPanelHoverBackgroundImage.color = WithPreservedAlpha(target, infoPanelHoverBackgroundImage.color.a);
    }

    private void AutoPrepareInfoPanelCharacterButtonReferences()
    {
        if (!IsInfoPanelPartyEditButton())
            return;

        if (infoPanelCharacterImage == null)
        {
            Transform charRoot = transform.Find("Char");
            Transform imageRoot = charRoot != null ? charRoot.Find("Image") : null;
            if (imageRoot == null)
                imageRoot = transform.Find("Image");

            if (imageRoot != null)
                infoPanelCharacterImage = imageRoot.GetComponent<Image>();
        }

        if (infoPanelRelicRoot == null)
        {
            Transform relic = transform.Find("Relic");
            if (relic != null)
                infoPanelRelicRoot = relic.gameObject;
        }

        if (infoPanelCompoundRoot == null)
        {
            Transform compound = transform.Find("Compound");
            if (compound != null)
                infoPanelCompoundRoot = compound.gameObject;
        }
    }

    private void CacheInfoPanelIdleImage()
    {
        // Idle / Battle Idle 프레임은 CharacterIconDatabase에서 CharacterId 기준으로 관리합니다.
        // CharBtn 인스펙터에는 프레임을 직접 보관하지 않습니다.
    }

    private void RefreshInfoPanelPartyEditVisualState(bool isSelectedForParty)
    {
        if (!IsInfoPanelPartyEditButton())
            return;

        AutoPrepareInfoPanelCharacterButtonReferences();
        CacheInfoPanelIdleImage();

        if (infoPanelRelicRoot != null)
            infoPanelRelicRoot.SetActive(isSelectedForParty);

        if (infoPanelCompoundRoot != null)
            infoPanelCompoundRoot.SetActive(isSelectedForParty);

        infoPanelSelectedForParty = isSelectedForParty;

        // 선택/해제 직후 Back 색상도 즉시 동기화합니다.
        // 선택 중이면 3C4476(기본 선택/호버 색), 해제되면 원래 색으로 복귀합니다.
        ApplyInfoPanelHoverBackground(false);

        RestartInfoPanelIdleAnimation();
    }

    private void RestartInfoPanelIdleAnimation()
    {
        StopInfoPanelIdleAnimation();

        if (infoPanelCharacterImage == null)
            return;

        // Inspector에 임시로 들어 있던 Sprite가 남지 않도록 매번 먼저 비웁니다.
        // 실제 CharacterDatabase에 캐릭터 데이터가 있고 Idle 프레임까지 확인된 경우에만 다시 켭니다.
        HideInfoPanelCharacterImage();

        if (!isActiveAndEnabled)
            return;

        Sprite[] frames = ResolveInfoPanelIdleFrames(infoPanelSelectedForParty);
        Sprite firstFrame = GetFirstValidFrame(frames);

        if (firstFrame == null && infoPanelSelectedForParty)
        {
            // Battle Idle이 비어 있으면 같은 캐릭터의 일반 Idle을 사용합니다.
            frames = ResolveInfoPanelIdleFrames(false);
            firstFrame = GetFirstValidFrame(frames);
        }

        if (firstFrame == null)
            return;

        if (!infoPanelCharacterImage.gameObject.activeSelf)
            infoPanelCharacterImage.gameObject.SetActive(true);

        infoPanelCharacterImage.sprite = firstFrame;
        infoPanelCharacterImage.enabled = true;

        if (CountValidFrames(frames) > 1)
            infoPanelIdleAnimationCoroutine = StartCoroutine(PlayInfoPanelIdleAnimation(frames));
    }

    private Sprite[] ResolveInfoPanelIdleFrames(bool battleIdle)
    {
        if (!useCharacterIconDatabaseIdleFrames ||
            !TryResolveInfoPanelCharacterId(out string resolvedCharacterId))
        {
            return null;
        }

        CharacterIconDatabase database = DataManager.Instance.CharacterIconDatabase;
        Sprite[] frames;
        bool found = battleIdle
            ? database.TryGetLobbyBattleIdleFrames(resolvedCharacterId, out frames)
            : database.TryGetLobbyIdleFrames(resolvedCharacterId, out frames);

        return found ? frames : null;
    }

    private bool TryResolveInfoPanelCharacterId(out string resolvedCharacterId)
    {
        resolvedCharacterId = null;

        if (string.IsNullOrWhiteSpace(characterId) || DataManager.Instance == null)
            return false;

        if (DataManager.Instance.CharacterDatabase == null ||
            !DataManager.Instance.CharacterDatabase.TryGet(characterId.Trim(), out CharacterMasterData master) ||
            master == null ||
            string.IsNullOrWhiteSpace(master.CharacterId))
        {
            return false;
        }

        if (DataManager.Instance.CharacterIconDatabase == null)
            return false;

        resolvedCharacterId = master.CharacterId.Trim();
        return true;
    }

    private void HideInfoPanelCharacterImage()
    {
        if (infoPanelCharacterImage == null)
            return;

        infoPanelCharacterImage.sprite = null;
        infoPanelCharacterImage.enabled = false;
        if (infoPanelCharacterImage.gameObject.activeSelf)
            infoPanelCharacterImage.gameObject.SetActive(false);
    }

    private IEnumerator PlayInfoPanelIdleAnimation(Sprite[] frames)
    {
        int frameIndex = 0;
        float interval = Mathf.Max(0.01f, infoPanelIdleFrameInterval);

        while (isActiveAndEnabled)
        {
            if (frames == null || frames.Length == 0 || infoPanelCharacterImage == null)
                yield break;

            Sprite frame = frames[frameIndex % frames.Length];
            if (frame != null)
            {
                if (!infoPanelCharacterImage.gameObject.activeSelf)
                    infoPanelCharacterImage.gameObject.SetActive(true);

                infoPanelCharacterImage.sprite = frame;
                infoPanelCharacterImage.enabled = true;
            }

            frameIndex = (frameIndex + 1) % frames.Length;
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    private void StopInfoPanelIdleAnimation()
    {
        if (infoPanelIdleAnimationCoroutine == null)
            return;

        StopCoroutine(infoPanelIdleAnimationCoroutine);
        infoPanelIdleAnimationCoroutine = null;
    }

    private static bool HasAnyFrame(Sprite[] frames)
    {
        return GetFirstValidFrame(frames) != null;
    }

    private static Sprite GetFirstValidFrame(Sprite[] frames)
    {
        if (frames == null)
            return null;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                return frames[i];
        }

        return null;
    }

    private static int CountValidFrames(Sprite[] frames)
    {
        if (frames == null)
            return 0;

        int count = 0;
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                count++;
        }

        return count;
    }

    private static Color WithPreservedAlpha(Color rgbSource, float alpha)
    {
        rgbSource.a = alpha;
        return rgbSource;
    }

    private void RefreshCharacterSelectScale(bool immediate)
    {
        CacheCharBtnOriginalScale();

        if (rect == null)
            return;

        if (charBtnHoverCoroutine != null)
        {
            StopCoroutine(charBtnHoverCoroutine);
            charBtnHoverCoroutine = null;
        }

        float scaleMultiplier;

        if (IsInfoPanelPartyEditButton())
            scaleMultiplier = 1f;
        else if (isViewedCharacter)
            scaleMultiplier = Mathf.Max(0f, viewedCharacterFixedScale);
        else if (isCharBtnHovered && !isLocked)
            scaleMultiplier = Mathf.Max(0f, charBtnHoverScale);
        else
            scaleMultiplier = 1f;

        Vector3 targetScale = charBtnOriginalScale * scaleMultiplier;

        if (immediate || !isActiveAndEnabled || charBtnHoverTransitionDuration <= 0f)
        {
            rect.localScale = targetScale;
            return;
        }

        charBtnHoverCoroutine = StartCoroutine(AnimateCharBtnHoverRoutine(targetScale));
    }

    public void RefreshNetworkViewedCharacterState(bool immediate = false)
    {
        SteamLobbyPartySynchronizer synchronizer = SteamLobbyPartySynchronizer.Instance;

        if (synchronizer == null || !synchronizer.IsNetworkPartyActive)
        {
            if (isRemoteViewedCharacter)
                SetNetworkViewedCharacterState(isViewedCharacter, false, immediate);

            return;
        }

        bool isLocalViewed = synchronizer.IsLocalViewingCharacter(characterId);
        bool isRemoteViewed = synchronizer.IsCharacterViewedByRemoteMember(characterId);
        SetNetworkViewedCharacterState(isLocalViewed, isRemoteViewed, immediate);
    }


    /// <summary>
    /// 현재 보고 있는 캐릭터 버튼의 선택 색상을 EventSystem과 별개로 유지한다.
    /// 선택된 버튼은 Normal Color를 기존 Selected Color로 사용하므로,
    /// 다른 UI를 눌러도 선택 색상이 꺼지지 않는다.
    /// </summary>
    private void ApplyViewedCharacterButtonColor(bool isViewed)
    {
        if (characterButton == null)
            characterButton = GetComponent<Button>();

        if (characterButton == null)
            return;

        if (!hasOriginalButtonColors)
        {
            originalButtonColors = characterButton.colors;
            hasOriginalButtonColors = true;
        }

        ColorBlock colors = originalButtonColors;

        if (isViewed)
            colors.normalColor = ViewedCharacterSelectedColor;

        characterButton.colors = colors;

        Graphic targetGraphic = characterButton.targetGraphic;

        if (targetGraphic == null)
            targetGraphic = characterButton.GetComponent<Graphic>();

        if (targetGraphic != null)
            targetGraphic.color = isViewed
                ? ViewedCharacterSelectedColor
                : originalButtonColors.normalColor;
    }

    private IEnumerator AnimateViewedCharacterRoutine(bool isViewed, Vector3 targetScale)
    {
        List<Quaternion> startRotations = new(viewedCharacterBorderTargets.Count);

        for (int i = 0; i < viewedCharacterBorderTargets.Count; i++)
        {
            RectTransform border = viewedCharacterBorderTargets[i];
            startRotations.Add(border != null ? border.localRotation : Quaternion.identity);
        }

        Vector3 startScale = rect.localScale;
        float duration = Mathf.Max(0.01f, viewedCharacterTransitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < viewedCharacterBorderTargets.Count; i++)
            {
                RectTransform border = viewedCharacterBorderTargets[i];

                if (border == null)
                    continue;

                Quaternion targetRotation = GetViewedCharacterTargetRotation(i, isViewed);
                border.localRotation = Quaternion.Slerp(startRotations[i], targetRotation, t);
            }

            rect.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        ApplyViewedCharacterBorderRotations(isViewed);
        rect.localScale = targetScale;
        viewedCharacterTransitionCoroutine = null;
    }

    private void ApplyViewedCharacterBorderRotations(bool isViewed)
    {
        for (int i = 0; i < viewedCharacterBorderTargets.Count; i++)
        {
            RectTransform border = viewedCharacterBorderTargets[i];

            if (border != null)
                border.localRotation = GetViewedCharacterTargetRotation(i, isViewed);
        }
    }

    private Quaternion GetViewedCharacterTargetRotation(int borderIndex, bool isViewed)
    {
        if (!hasViewedCharacterOriginalValues)
            return Quaternion.identity;

        Quaternion originalRotation =
            borderIndex >= 0 && borderIndex < viewedCharacterBorderOriginalRotations.Count
                ? viewedCharacterBorderOriginalRotations[borderIndex]
                : Quaternion.identity;

        if (!isViewed)
            return originalRotation;

        Vector3 originalEuler = originalRotation.eulerAngles;
        return Quaternion.Euler(originalEuler.x, originalEuler.y, viewedCharacterRotationZ);
    }

    private Vector3 GetViewedCharacterTargetScale(bool isViewed)
    {
        if (!hasViewedCharacterOriginalValues)
            return Vector3.one;

        if (!isViewed)
            return viewedCharacterOriginalScale;

        return new Vector3(
            viewedCharacterOriginalScale.x * viewedCharacterScale,
            viewedCharacterOriginalScale.y * viewedCharacterScale,
            viewedCharacterOriginalScale.z);
    }

    private void AutoPrepareViewedCharacterBorder()
    {
        if (viewedCharacterBorderTargets.Count > 0)
            return;

        string targetName = string.IsNullOrWhiteSpace(viewedCharacterBorderName)
            ? "BorderImg1"
            : viewedCharacterBorderName;

        if (viewedCharacterBorder != null)
            AddViewedCharacterBorderTarget(viewedCharacterBorder);

        if (viewedCharacterBorders != null)
        {
            for (int i = 0; i < viewedCharacterBorders.Length; i++)
                AddViewedCharacterBorderTarget(viewedCharacterBorders[i]);
        }

        AddViewedCharacterBorderTarget(FindViewedCharacterBorder(targetName));

        string[] targetNames = viewedCharacterBorderNames;

        if (targetNames == null || targetNames.Length <= 0)
            targetNames = new[] { "BorderImg1", "BorderImg2" };

        for (int i = 0; i < targetNames.Length; i++)
        {
            string name = targetNames[i];

            if (string.IsNullOrWhiteSpace(name))
                continue;

            AddViewedCharacterBorderTarget(FindViewedCharacterBorder(name));
        }

        if (viewedCharacterBorder == null && viewedCharacterBorderTargets.Count > 0)
            viewedCharacterBorder = viewedCharacterBorderTargets[0];
    }

    private RectTransform FindViewedCharacterBorder(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        Transform found = transform.Find(targetName);

        if (found != null)
            return found as RectTransform;

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == targetName)
            {
                found = children[i];
                break;
            }
        }

        return found as RectTransform;
    }

    private void AddViewedCharacterBorderTarget(RectTransform border)
    {
        if (border == null || viewedCharacterBorderTargets.Contains(border))
            return;

        viewedCharacterBorderTargets.Add(border);
    }

    private void CacheViewedCharacterOriginalValues()
    {
        if (hasViewedCharacterOriginalValues || viewedCharacterBorderTargets.Count <= 0 || rect == null)
            return;

        viewedCharacterBorderOriginalRotations.Clear();
        viewedCharacterBorderGraphics.Clear();
        viewedCharacterBorderOriginalColors.Clear();

        for (int i = 0; i < viewedCharacterBorderTargets.Count; i++)
        {
            RectTransform border = viewedCharacterBorderTargets[i];
            viewedCharacterBorderOriginalRotations.Add(border != null
                ? border.localRotation
                : Quaternion.identity);

            Graphic graphic = border != null
                ? border.GetComponent<Graphic>()
                : null;
            viewedCharacterBorderGraphics.Add(graphic);
            viewedCharacterBorderOriginalColors.Add(graphic != null
                ? graphic.color
                : Color.white);
        }

        viewedCharacterOriginalScale = rect.localScale;
        hasViewedCharacterOriginalValues = true;
    }

    private void ApplyViewedCharacterBorderAlpha(bool isRemoteViewed)
    {
        if (!hasViewedCharacterOriginalValues)
            return;

        float alphaMultiplier = isRemoteViewed
            ? remoteViewedCharacterAlpha
            : 1f;

        for (int i = 0; i < viewedCharacterBorderGraphics.Count; i++)
        {
            Graphic graphic = viewedCharacterBorderGraphics[i];

            if (graphic == null)
                continue;

            Color originalColor = i < viewedCharacterBorderOriginalColors.Count
                ? viewedCharacterBorderOriginalColors[i]
                : graphic.color;
            originalColor.a *= alphaMultiplier;
            graphic.color = originalColor;
        }
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;

        if (visible)
            RefreshNetworkAvailability();
    }

    public void RefreshNetworkAvailability()
    {
        if (canvasGroup == null)
            return;

        SteamLobbyPartySynchronizer synchronizer = SteamLobbyPartySynchronizer.Instance;

        if (synchronizer == null || !synchronizer.IsNetworkPartyActive)
            return;

        canvasGroup.alpha = 1f;
    }
}
