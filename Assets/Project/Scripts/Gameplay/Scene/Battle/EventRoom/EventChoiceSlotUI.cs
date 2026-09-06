using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class EventChoiceSlotUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button button;
    [SerializeField] private ButtonAnimationCoroutine buttonAnimation;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject disabledRoot;
    [SerializeField] private TMP_Text choiceNameText;
    [SerializeField] private TMP_Text choiceDescText;
    [SerializeField] private TMP_Text unavailableReasonText;
    [SerializeField] private Color enabledColor = new(0.05f, 0.07f, 0.08f, 0.9f);
    [SerializeField] private Color disabledColor = new(0.28f, 0.28f, 0.28f, 0.65f);

    private bool boundSelectable;

    private void Awake()
    {
        EnsureReferences();
    }

    public void Bind(
        EventData choice,
        bool selectable,
        string unavailableReason,
        UnityAction onClick)
    {
        EnsureReferences();

        if (choice == null)
        {
            Clear();
            return;
        }

        boundSelectable = selectable;

        if (root != null)
            root.SetActive(true);

        if (choiceNameText != null)
        {
            string order = choice.ChoiceOrder > 0 ? $"{choice.ChoiceOrder}. " : string.Empty;
            choiceNameText.text = order + (choice.ChoiceName ?? string.Empty);
        }

        string displayedChoiceDesc = selectable
            ? choice.ChoiceDesc
            : (!string.IsNullOrWhiteSpace(choice.UnavailableChoiceDesc)
                ? choice.UnavailableChoiceDesc
                : unavailableReason);

        if (choiceDescText != null)
            choiceDescText.text = displayedChoiceDesc ?? string.Empty;

        // 선택 불가 안내는 ChoiceDescText에 표시합니다.
        // 기존 UnavailableReasonText가 씬에 남아 있어도 문구가 중복되지 않게 비웁니다.
        if (unavailableReasonText != null)
            unavailableReasonText.text = string.Empty;

        if (disabledRoot != null)
            disabledRoot.SetActive(!selectable);

        if (backgroundImage != null)
            backgroundImage.color = selectable ? enabledColor : disabledColor;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(onClick);
            button.interactable = selectable;
        }

        if (buttonAnimation != null)
            buttonAnimation.SetInteractionEnabled(selectable);
    }

    public void SetInteractable(bool interactable)
    {
        EnsureReferences();

        bool effectiveInteractable = interactable && boundSelectable;

        if (button != null)
            button.interactable = effectiveInteractable;

        if (buttonAnimation != null)
            buttonAnimation.SetInteractionEnabled(effectiveInteractable);
    }

    public void Clear()
    {
        EnsureReferences();
        boundSelectable = false;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }

        if (buttonAnimation != null)
            buttonAnimation.SetInteractionEnabled(false);

        if (choiceNameText != null)
            choiceNameText.text = string.Empty;

        if (choiceDescText != null)
            choiceDescText.text = string.Empty;

        if (unavailableReasonText != null)
            unavailableReasonText.text = string.Empty;

        if (disabledRoot != null)
            disabledRoot.SetActive(false);

        if (root != null)
            root.SetActive(false);
    }

    private void EnsureReferences()
    {
        if (root == null)
            root = gameObject;

        if (button == null)
            button = GetComponent<Button>();

        if (buttonAnimation == null)
            buttonAnimation = GetComponent<ButtonAnimationCoroutine>();

        if (buttonAnimation == null)
            buttonAnimation = GetComponentInChildren<ButtonAnimationCoroutine>(true);

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (choiceNameText == null)
            choiceNameText = FindText("ChoiceNameText");

        if (choiceDescText == null)
            choiceDescText = FindText("ChoiceDescText");

        if (unavailableReasonText == null)
            unavailableReasonText = FindText("UnavailableReasonText");

        if (disabledRoot == null)
        {
            Transform disabledTransform = FindChildRecursive(transform, "DisabledRoot");
            if (disabledTransform != null)
                disabledRoot = disabledTransform.gameObject;
        }
    }

    private TMP_Text FindText(string targetName)
    {
        Transform target = FindChildRecursive(transform, targetName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private Transform FindChildRecursive(Transform current, string targetName)
    {
        if (current == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (string.Equals(current.name, targetName, System.StringComparison.Ordinal))
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform result = FindChildRecursive(current.GetChild(i), targetName);
            if (result != null)
                return result;
        }

        return null;
    }
}
