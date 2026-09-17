using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 로비 PartPanel의 Character1 / Character2 / Character3 슬롯에 사용합니다.
/// 클릭한 파티 슬롯의 캐릭터로 CharacterSettingPanel을 열고,
/// 호버 및 파티 등록 상태에 맞춰 슬롯 시각 효과를 갱신합니다.
/// </summary>
[DisallowMultipleComponent]
public class LobbyPartyCharacterSettingOpenButton : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
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

    [Header("Visual")]
    [Tooltip("비워 두면 자식 Back 이미지를 자동으로 찾습니다.")]
    [SerializeField] private Image backImage;
    [Tooltip("비워 두면 자식 Select_Line 또는 SelectLine 오브젝트를 자동으로 찾습니다.")]
    [SerializeField] private GameObject selectLineRoot;
    [SerializeField] private Color hoverBackColor = new Color32(0x3C, 0x44, 0x76, 0xFF);
    [Min(1f)]
    [SerializeField] private float hoverScale = 1.1f;
    [Min(0.01f)]
    [SerializeField] private float scaleDuration = 0.15f;

    private Vector3 baseLocalScale;
    private Color baseBackColor;
    private bool pointerInside;
    private bool isRegistered;
    private int cachedPartyIndex = -1;

    private void Awake()
    {
        ResolveVisualReferences();
        baseLocalScale = transform.localScale;
        if (backImage != null)
            baseBackColor = backImage.color;
    }

    private void OnEnable()
    {
        pointerInside = false;
        ResolveVisualReferences();

        if (baseLocalScale == Vector3.zero)
            baseLocalScale = transform.localScale;

        if (backImage != null && baseBackColor == default)
            baseBackColor = backImage.color;

        cachedPartyIndex = ResolvePartyIndex();
        RefreshRegisteredState(forceVisualRefresh: true);
        ApplyBackVisual();
    }

    private void Update()
    {
        RefreshRegisteredState(forceVisualRefresh: false);
        UpdateScale();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ApplyBackVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        ApplyBackVisual();
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
        // 활성화 후 Setting.OnEnable에서 BackgroundPanel을 열고 다음 프레임에 캐릭터를 적용합니다.
        setting.OpenPartySettingWhenActive(partyIndex);
        characterSettingPanel.SetActive(true);
    }

    private void RefreshRegisteredState(bool forceVisualRefresh)
    {
        if (cachedPartyIndex < 0)
            cachedPartyIndex = ResolvePartyIndex();

        bool nextRegistered = false;
        DataManager dataManager = DataManager.Instance;
        if (cachedPartyIndex >= 0 && dataManager != null && dataManager.PartyRuntimeStore != null)
        {
            string characterId = dataManager.PartyRuntimeStore.GetCharacterId(cachedPartyIndex);
            nextRegistered = !string.IsNullOrWhiteSpace(characterId);
        }

        if (!forceVisualRefresh && nextRegistered == isRegistered)
            return;

        isRegistered = nextRegistered;

        if (selectLineRoot != null)
            selectLineRoot.SetActive(isRegistered);
    }

    private void UpdateScale()
    {
        Vector3 targetScale = (pointerInside || isRegistered)
            ? baseLocalScale * hoverScale
            : baseLocalScale;

        if ((transform.localScale - targetScale).sqrMagnitude <= 0.000001f)
        {
            transform.localScale = targetScale;
            return;
        }

        float duration = Mathf.Max(0.01f, scaleDuration);
        float t = Mathf.Clamp01(Time.unscaledDeltaTime / duration);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
    }

    private void ApplyBackVisual()
    {
        if (backImage == null)
            return;

        Color target = pointerInside ? hoverBackColor : baseBackColor;
        target.a = backImage.color.a;
        backImage.color = target;
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

    private void ResolveVisualReferences()
    {
        if (backImage == null)
        {
            Transform back = transform.Find("Back");
            if (back != null)
                backImage = back.GetComponent<Image>();
        }

        if (selectLineRoot == null)
        {
            Transform selectLine = transform.Find("Select_Line");
            if (selectLine == null)
                selectLine = transform.Find("SelectLine");

            if (selectLine != null)
                selectLineRoot = selectLine.gameObject;
        }
    }
}
