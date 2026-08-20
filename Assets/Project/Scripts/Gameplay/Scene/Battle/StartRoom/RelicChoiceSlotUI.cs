using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RelicChoiceSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image relicIconImage;
    [SerializeField] private TMP_Text relicNameText;
    [SerializeField] private TMP_Text relicEffectText;
    [SerializeField] private Button button;
    [SerializeField] private GameObject hoverImage;
    [SerializeField] private GameObject clickImage;

    private string relicId;
    private RelicChoiceAreaUI owner;
    private StartRoomSkillRewardChoice skillRewardChoice;
    private bool isSkillRewardChoice;
    private bool isSetup;
    private bool isPointerInside;
    private bool isSelected;

    private void Awake()
    {
        EnsureReferences();

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }

        RefreshStateImages();
    }

    private void OnEnable()
    {
        isPointerInside = false;
        isSelected = false;
        RefreshStateImages();
    }

    private void OnDisable()
    {
        isPointerInside = false;
        isSelected = false;
        RefreshStateImages();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    public void Setup(string id, RelicChoiceAreaUI choiceArea)
    {
        relicId = id;
        owner = choiceArea;
        skillRewardChoice = default;
        isSkillRewardChoice = false;
        isSetup = false;
        isPointerInside = false;
        isSelected = false;
        RefreshStateImages();

        if (string.IsNullOrWhiteSpace(relicId))
        {
            ClearSlot();
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.CompoundDatabase == null)
        {
            Debug.LogWarning("[RelicChoiceSlotUI] DataManager or CompoundDatabase is null.");
            ClearSlot();
            return;
        }

        RelicData relicData = DataManager.Instance.CompoundDatabase.Get(relicId);

        if (relicData == null)
        {
            Debug.LogWarning($"[RelicChoiceSlotUI] Unknown relic id: {relicId}");
            ClearSlot();
            return;
        }

        SetupDisplay(relicData);

        isSetup = true;
        RefreshStateImages();

        if (button != null)
            button.interactable = true;

        gameObject.SetActive(true);
    }

    public void SetupSkillRewardChoice(StartRoomSkillRewardChoice choice, RelicChoiceAreaUI choiceArea)
    {
        relicId = string.Empty;
        owner = choiceArea;
        skillRewardChoice = choice;
        isSkillRewardChoice = true;
        isSetup = false;
        isPointerInside = false;
        isSelected = false;
        RefreshStateImages();

        if (!choice.IsValid)
        {
            ClearSlot();
            return;
        }

        SetupSkillRewardChoiceDisplay(choice);

        isSetup = true;
        RefreshStateImages();

        if (button != null)
            button.interactable = true;

        gameObject.SetActive(true);
    }

    private void SetupDisplay(RelicData relicData)
    {
        if (relicIconImage != null)
        {
            if (DataManager.Instance != null &&
                DataManager.Instance.RelicIconDatabase != null &&
                DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon))
            {
                relicIconImage.sprite = icon;
                relicIconImage.enabled = true;
                relicIconImage.raycastTarget = true;
            }
            else
            {
                relicIconImage.sprite = null;
                relicIconImage.enabled = false;
                relicIconImage.raycastTarget = false;
            }
        }

        CompoundData compoundData = relicData as CompoundData;

        if (relicNameText != null)
        {
            relicNameText.text = compoundData != null && !string.IsNullOrWhiteSpace(compoundData.Name)
                ? compoundData.Name
                : GameDataLocalization.RelicName(relicData);
        }

        if (relicEffectText != null)
        {
            relicEffectText.text = compoundData != null && !string.IsNullOrWhiteSpace(compoundData.EffectDesc)
                ? compoundData.EffectDesc
                : GameDataLocalization.RelicDescription(relicData);
        }
    }

    public void ClearSlot()
    {
        relicId = string.Empty;
        owner = null;
        skillRewardChoice = default;
        isSkillRewardChoice = false;
        isSetup = false;
        isPointerInside = false;
        isSelected = false;
        RefreshStateImages();

        if (button != null)
            button.interactable = false;

        if (relicIconImage != null)
        {
            relicIconImage.sprite = null;
            relicIconImage.enabled = false;
            relicIconImage.raycastTarget = false;
        }

        if (relicNameText != null)
            relicNameText.text = string.Empty;

        if (relicEffectText != null)
            relicEffectText.text = string.Empty;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsMenuPanelOpen())
            return;

        if (!isSetup || owner == null)
            return;

        isPointerInside = true;
        RefreshStateImages();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        RefreshStateImages();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsMenuPanelOpen())
            return;

        OnClick();
    }

    public void OnClick()
    {
        if (IsMenuPanelOpen())
            return;

        if (!isSetup || owner == null)
            return;

        if (isSkillRewardChoice)
        {
            owner.SelectSkillRewardChoice(this, skillRewardChoice);
            return;
        }

        if (string.IsNullOrWhiteSpace(relicId))
            return;

        owner.SelectSlot(this, relicId);
    }

    public void SetSelected(bool selected)
    {
        isSelected = isSetup && selected;
        RefreshStateImages();
    }

    private void RefreshStateImages()
    {
        if (hoverImage != null)
            hoverImage.SetActive(isSetup && isPointerInside);

        if (clickImage != null)
            clickImage.SetActive(isSetup && isSelected);
    }

    private void EnsureReferences()
    {
        if (relicIconImage == null)
        {
            Transform iconTransform = FindChildRecursive(transform, "Relic_Icon");
            if (iconTransform != null)
                relicIconImage = iconTransform.GetComponent<Image>();
        }

        if (relicNameText == null)
        {
            Transform nameTransform = FindChildRecursive(transform, "RelicNameText");
            if (nameTransform != null)
                relicNameText = nameTransform.GetComponent<TMP_Text>();
        }

        if (relicEffectText == null)
        {
            Transform effectTransform = FindChildRecursive(transform, "RelicEffectText");
            if (effectTransform != null)
                relicEffectText = effectTransform.GetComponent<TMP_Text>();
        }

        if (hoverImage == null)
        {
            Transform hoverTransform = FindChildRecursive(transform, "HoverImage");
            if (hoverTransform != null)
                hoverImage = hoverTransform.gameObject;
        }

        if (clickImage == null)
        {
            Transform clickTransform = FindChildRecursive(transform, "ClickImage");
            if (clickTransform != null)
                clickImage = clickTransform.gameObject;
        }
    }

    private void SetupSkillRewardChoiceDisplay(StartRoomSkillRewardChoice choice)
    {
        if (relicIconImage != null)
        {
            if (DataManager.Instance != null &&
                DataManager.Instance.ActionTypeIconDatabase != null &&
                DataManager.Instance.ActionTypeIconDatabase.TryGetIcon(choice.SkillType.ToString(), out Sprite icon))
            {
                relicIconImage.sprite = icon;
                relicIconImage.enabled = true;
                relicIconImage.raycastTarget = true;
            }
            else
            {
                relicIconImage.sprite = null;
                relicIconImage.enabled = false;
                relicIconImage.raycastTarget = false;
            }
        }

        if (relicNameText != null)
            relicNameText.text = choice.Title;

        if (relicEffectText != null)
            relicEffectText.text = choice.Description;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static bool IsMenuPanelOpen()
    {
        GameObject menuPanel = GameObject.Find("MenuPanel");
        return menuPanel != null && menuPanel.activeInHierarchy;
    }
}
