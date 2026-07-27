using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;

    [Header("Legacy Direct Register")]
    [SerializeField] private int firstPartyDefaultDeployCellNumber = 7;
    [SerializeField] private int maxDeployGridCount = 15;

    [Header("Selected Party Marker")]
    [SerializeField] private bool showSelectedPartyMarker = true;
    [SerializeField] private GameObject selectedPartyMarkerRoot;
    [SerializeField] private Image selectedPartyMarkerImage;
    [SerializeField] private TMP_Text selectedPartyMarkerText;
    [SerializeField] private string selectedPartyTextFormat = "{0}";

    [Header("현재 보고 있는 캐릭터 표시")]
    [SerializeField] private RectTransform viewedCharacterBorder;
    [SerializeField] private string viewedCharacterBorderName = "BorderImg1";
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
    private int lastHandledClickFrame = -1;

    private Quaternion viewedCharacterBorderOriginalRotation = Quaternion.identity;
    private Vector3 viewedCharacterOriginalScale = Vector3.one;
    private bool hasViewedCharacterOriginalValues;
    private Coroutine viewedCharacterTransitionCoroutine;

    public CharacterType CharacterType => characterType;
    public string CharacterId => characterId;
    public RectTransform Rect => rect;
    public bool IsLocked => isLocked;

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
        AutoPrepareViewedCharacterBorder();
        CacheViewedCharacterOriginalValues();
        RefreshSelectedPartyMarker();
    }

    private void OnEnable()
    {
        AutoPrepareViewedCharacterBorder();
        CacheViewedCharacterOriginalValues();
        RefreshSelectedPartyMarker();
        ApplyViewedCharacterButtonColor(isViewedCharacter);
    }

    private void OnDisable()
    {
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
        AutoPrepareViewedCharacterBorder();
    }
#endif

    public void Init(CharPick pick)
    {
        charPick = pick;

        SetCenter(false);
        SetVisible(false);
        RefreshSelectedPartyMarker();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        NotifyClickToCharPickOrExecuteDirect();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (charPick == null)
            return;

        charPick.PointerEnterButton(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (charPick == null)
            return;

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
                master.CommonSkill1,
                ""
            },

            EquippedRuneIds = new string[12],
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

        bool success = partyStore.SetCharacter(selectedSlot, characterId);

        if (!success)
            return;

        int defaultGridIndex = FindDefaultDeployGridIndex(selectedSlot);

        if (defaultGridIndex >= 0)
            partyStore.SetSpawnGridIndex(selectedSlot, defaultGridIndex);

        Debug.Log(
            $"[Party] Set Slot / CharacterId:{characterId} / " +
            $"Slot:{selectedSlot} / Grid:{partyStore.GetSpawnGridIndex(selectedSlot)}"
        );
    }

    private int FindDefaultDeployGridIndex(int partySlotIndex)
    {
        if (DataManager.Instance == null)
            return -1;

        int preferredGridIndex = Mathf.Max(1, firstPartyDefaultDeployCellNumber) - 1 + partySlotIndex;

        if (IsAvailableDeployGridForSlot(preferredGridIndex, partySlotIndex))
            return preferredGridIndex;

        for (int i = 0; i < maxDeployGridCount; i++)
        {
            if (IsAvailableDeployGridForSlot(i, partySlotIndex))
                return i;
        }

        return -1;
    }

    private bool IsAvailableDeployGridForSlot(int gridIndex, int partySlotIndex)
    {
        if (gridIndex < 0 || gridIndex >= maxDeployGridCount)
            return false;

        if (DataManager.Instance == null)
            return false;

        var partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            if (i == partySlotIndex)
                continue;

            if (partyStore.GetSpawnGridIndex(i) == gridIndex)
                return false;
        }

        return true;
    }

    private void RefreshPartyViews()
    {
        PartySlot[] partySlots = FindObjectsByType<PartySlot>(FindObjectsSortMode.None);

        for (int i = 0; i < partySlots.Length; i++)
        {
            if (partySlots[i] != null)
                partySlots[i].RefreshFromRuntime();
        }

        SpawnGridPanel[] spawnGridPanels = FindObjectsByType<SpawnGridPanel>(FindObjectsSortMode.None);

        for (int i = 0; i < spawnGridPanels.Length; i++)
        {
            if (spawnGridPanels[i] == null)
                continue;

            spawnGridPanels[i].AutoPlacePartyIfNeeded();
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

        if (!showSelectedPartyMarker)
        {
            SetSelectedPartyMarkerActive(false);
            return;
        }

        int registeredSlot = FindDisplayedPartySlot();
        bool isRegistered = registeredSlot >= 0;

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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (charPick == null)
            return;

        charPick.BeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (charPick == null)
            return;

        charPick.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (charPick == null)
            return;

        charPick.EndDrag(eventData);
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
        isViewedCharacter = isViewed;
        ApplyViewedCharacterButtonColor(isViewed);

        AutoPrepareViewedCharacterBorder();
        CacheViewedCharacterOriginalValues();

        if (viewedCharacterBorder == null || rect == null)
            return;

        Quaternion targetRotation = GetViewedCharacterTargetRotation(isViewed);
        Vector3 targetScale = GetViewedCharacterTargetScale(isViewed);

        if (viewedCharacterTransitionCoroutine != null)
        {
            StopCoroutine(viewedCharacterTransitionCoroutine);
            viewedCharacterTransitionCoroutine = null;
        }

        if (immediate || !isActiveAndEnabled || !gameObject.activeInHierarchy || viewedCharacterTransitionDuration <= 0f)
        {
            viewedCharacterBorder.localRotation = targetRotation;
            rect.localScale = targetScale;
            return;
        }

        viewedCharacterTransitionCoroutine = StartCoroutine(
            AnimateViewedCharacterRoutine(targetRotation, targetScale));
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

    private IEnumerator AnimateViewedCharacterRoutine(Quaternion targetRotation, Vector3 targetScale)
    {
        Quaternion startRotation = viewedCharacterBorder.localRotation;
        Vector3 startScale = rect.localScale;
        float duration = Mathf.Max(0.01f, viewedCharacterTransitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            viewedCharacterBorder.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            rect.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        viewedCharacterBorder.localRotation = targetRotation;
        rect.localScale = targetScale;
        viewedCharacterTransitionCoroutine = null;
    }

    private Quaternion GetViewedCharacterTargetRotation(bool isViewed)
    {
        if (!hasViewedCharacterOriginalValues)
            return Quaternion.identity;

        if (!isViewed)
            return viewedCharacterBorderOriginalRotation;

        Vector3 originalEuler = viewedCharacterBorderOriginalRotation.eulerAngles;
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
        if (viewedCharacterBorder != null)
            return;

        string targetName = string.IsNullOrWhiteSpace(viewedCharacterBorderName)
            ? "BorderImg1"
            : viewedCharacterBorderName;

        Transform found = transform.Find(targetName);

        if (found == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == targetName)
                {
                    found = children[i];
                    break;
                }
            }
        }

        if (found != null)
            viewedCharacterBorder = found as RectTransform;
    }

    private void CacheViewedCharacterOriginalValues()
    {
        if (hasViewedCharacterOriginalValues || viewedCharacterBorder == null || rect == null)
            return;

        viewedCharacterBorderOriginalRotation = viewedCharacterBorder.localRotation;
        viewedCharacterOriginalScale = rect.localScale;
        hasViewedCharacterOriginalValues = true;
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

        canvasGroup.alpha = synchronizer.CanLocalPlayerSelectCharacter(characterId)
            ? 1f
            : 0.55f;
    }
}
