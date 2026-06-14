using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Relic.Gameplay.Data;

public class CharBtn : MonoBehaviour,
    IPointerClickHandler,
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

    [Header("UI")]
    [SerializeField] private Image borderImg;

    private CharPick charPick;
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private int lastHandledClickFrame = -1;

    public CharacterType CharacterType => characterType;
    public string CharacterId => characterId;
    public RectTransform Rect => rect;
    public bool IsLocked => isLocked;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        AutoPrepareSelectedPartyMarkerReferences();
        RefreshSelectedPartyMarker();
    }

    private void OnEnable()
    {
        RefreshSelectedPartyMarker();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoPrepareSelectedPartyMarkerReferences();
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

    public void Execute()
    {
        NotifyClickToCharPickOrExecuteDirect();
    }

    private void NotifyClickToCharPickOrExecuteDirect()
    {
        if (lastHandledClickFrame == Time.frameCount)
            return;

        lastHandledClickFrame = Time.frameCount;

        if (charPick != null)
        {
            charPick.ClickBtn(this);
            return;
        }

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
        if (!PrepareCharacterForPartyAction(true))
            return;

        SaveCharacterToSelectedPartySlot();
        RefreshPartyViews();
    }

    private void PlayClickSound()
    {
        if (playClickSound && AudioManager.Instance != null)
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
            return;

        runtime = new CharacterRuntimeData
        {
            CharacterId = master.CharacterId,
            Level = 1,
            Exp = 0,

            CurrentHealth = master.MaxHealth,
            CurrentStamina = master.MaxStamina,
            CurrentResource = master.MaxResource,
            CurrentMoveLevel = 1,

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
            }
        };

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
        if (borderImg == null)
            return;

        borderImg.gameObject.SetActive(isCenter);
        borderImg.enabled = isCenter;
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
