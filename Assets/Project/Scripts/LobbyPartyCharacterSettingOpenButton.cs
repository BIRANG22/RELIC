using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 로비의 Character1 / Character2 / Character3 이미지 오브젝트에 직접 붙여서 사용합니다.
/// 클릭한 파티 슬롯의 캐릭터로 CharacterSettingPanel을 일반 모달 방식으로 엽니다.
/// </summary>
[DisallowMultipleComponent]
public class LobbyPartyCharacterSettingOpenButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum PartySlot
    {
        Auto = 0,
        Character1 = 1,
        Character2 = 2,
        Character3 = 3
    }

    [Header("Party Slot")]
    [Tooltip("Auto이면 이 오브젝트 또는 부모의 이름(Character1~3)으로 슬롯을 자동 판별합니다.")]
    [SerializeField] private PartySlot partySlot = PartySlot.Auto;

    [Header("Optional References")]
    [Tooltip("비워 두면 씬의 Setting 컴포넌트를 자동으로 찾습니다.")]
    [SerializeField] private Setting setting;

    [Header("Hover Visual")]
    [SerializeField] private Color hoverBackColor = new Color32(0x3C, 0x44, 0x76, 0xFF);
    [SerializeField] private float hoverScale = 1.1f;

    private Image backImage;
    private Color normalBackColor = Color.white;
    private GameObject selectLineObject;
    private Vector3 normalScale = Vector3.one;
    private bool isPointerOver;
    private bool hoverReferencesInitialized;

    private void Awake()
    {
        ResolveHoverReferences();
        RefreshVisualState();
    }

    private void OnEnable()
    {
        ResolveHoverReferences();
        RefreshVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        RefreshVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        RefreshVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        Open();
    }

    /// <summary>
    /// Unity Button OnClick에서도 직접 호출할 수 있습니다.
    /// </summary>
    public void Open()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        int partyIndex = ResolvePartyIndex();
        if (partyIndex < 0)
        {
            Debug.LogWarning(
                "[LobbyPartyCharacterSettingOpenButton] 파티 슬롯을 판별할 수 없습니다. " +
                "Character1~3 오브젝트에 붙이거나 Party Slot을 직접 지정해주세요.",
                this);
            return;
        }

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.PartyRuntimeStore == null)
        {
            Debug.LogWarning(
                "[LobbyPartyCharacterSettingOpenButton] 파티 데이터를 찾을 수 없습니다.",
                this);
            return;
        }

        ResolveReferences();
        if (setting == null)
        {
            Debug.LogWarning(
                "[LobbyPartyCharacterSettingOpenButton] Setting 컴포넌트를 찾을 수 없습니다.",
                this);
            return;
        }

        GameObject characterSettingPanel = setting.gameObject;

        // 이미 CharacterSettingPanel이 열려 있다면 패널 전환 없이 선택 캐릭터만 갱신합니다.
        if (characterSettingPanel.activeInHierarchy)
        {
            setting.OpenPartySetting(partyIndex);
            return;
        }

        // 다른 PositionPanel 모달이 열려 있다면 기존 공용 Close 경로로 먼저 정리합니다.
        if (!LobbyPositionSharedModalBackground.PrepareForPanelSwitch(characterSettingPanel))
            return;

        TitleManager.CloseTitleModePanelsExceptInScene(characterSettingPanel);

        // 패널이 비활성 상태이므로 먼저 슬롯 선택을 예약합니다.
        // 활성화 후 Setting.OnEnable에서 BackgroundPanel을 열고, 다음 프레임에 캐릭터를 적용합니다.
        setting.OpenPartySettingWhenActive(partyIndex);
        characterSettingPanel.SetActive(true);
    }

    public static void RefreshAll()
    {
        LobbyPartyCharacterSettingOpenButton[] buttons = FindObjectsByType<LobbyPartyCharacterSettingOpenButton>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].RefreshVisualState();
        }
    }

    private void ResolveHoverReferences()
    {
        if (hoverReferencesInitialized)
            return;

        normalScale = transform.localScale;

        Transform back = transform.Find("Back");
        if (back != null)
        {
            backImage = back.GetComponent<Image>();
            if (backImage != null)
                normalBackColor = backImage.color;
        }

        Transform selectLine = transform.Find("Select_Line");
        selectLineObject = selectLine != null ? selectLine.gameObject : null;
        hoverReferencesInitialized = true;
    }

    private void RefreshVisualState()
    {
        ResolveHoverReferencesIfNeeded();

        int partyIndex = ResolvePartyIndex();
        bool hasCharacter = false;
        DataManager dataManager = DataManager.Instance;
        if (partyIndex >= 0 && dataManager != null && dataManager.PartyRuntimeStore != null)
        {
            string characterId = dataManager.PartyRuntimeStore.GetCharacterId(partyIndex);
            hasCharacter = !string.IsNullOrWhiteSpace(characterId);
        }

        if (selectLineObject != null)
            selectLineObject.SetActive(hasCharacter);

        bool enlarged = hasCharacter || isPointerOver;
        transform.localScale = normalScale * (enlarged ? Mathf.Max(1f, hoverScale) : 1f);

        if (backImage != null)
            backImage.color = isPointerOver ? hoverBackColor : normalBackColor;
    }

    private void ResolveHoverReferencesIfNeeded()
    {
        if (!hoverReferencesInitialized)
        {
            ResolveHoverReferences();
            return;
        }

        if (backImage == null)
        {
            Transform back = transform.Find("Back");
            if (back != null)
            {
                backImage = back.GetComponent<Image>();
                if (backImage != null)
                    normalBackColor = backImage.color;
            }
        }

        if (selectLineObject == null)
        {
            Transform selectLine = transform.Find("Select_Line");
            if (selectLine != null)
                selectLineObject = selectLine.gameObject;
        }
    }

    private int ResolvePartyIndex()
    {
        switch (partySlot)
        {
            case PartySlot.Character1:
                return 0;
            case PartySlot.Character2:
                return 1;
            case PartySlot.Character3:
                return 2;
        }

        Transform current = transform;
        while (current != null)
        {
            if (TryParseCharacterObjectName(current.name, out int index))
                return index;

            current = current.parent;
        }

        return -1;
    }

    private static bool TryParseCharacterObjectName(string objectName, out int partyIndex)
    {
        partyIndex = -1;

        if (string.Equals(objectName, "Character1", System.StringComparison.OrdinalIgnoreCase))
        {
            partyIndex = 0;
            return true;
        }

        if (string.Equals(objectName, "Character2", System.StringComparison.OrdinalIgnoreCase))
        {
            partyIndex = 1;
            return true;
        }

        if (string.Equals(objectName, "Character3", System.StringComparison.OrdinalIgnoreCase))
        {
            partyIndex = 2;
            return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (setting == null)
            setting = FindFirstObjectByType<Setting>(FindObjectsInactive.Include);
    }
}
