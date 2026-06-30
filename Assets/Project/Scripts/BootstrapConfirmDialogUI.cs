using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BootstrapConfirmDialogUI : MonoBehaviour
{
    [Header("Message")]
    [SerializeField] private TMP_Text tmpMessageText;
    [SerializeField] private Text legacyMessageText;

    [Header("Buttons")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private TMP_Text tmpYesButtonText;
    [SerializeField] private TMP_Text tmpNoButtonText;
    [SerializeField] private Text legacyYesButtonText;
    [SerializeField] private Text legacyNoButtonText;

    [Header("Auto Bind")]
    [SerializeField] private bool autoBindReferences = true;

    private Action onYes;
    private Action onNo;

    private void Awake()
    {
        ResolveReferencesIfNeeded();
        RegisterButtonEvents();
    }

    private void OnEnable()
    {
        ResolveReferencesIfNeeded();
        RegisterButtonEvents();
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
    }

    public void Configure(string message, string yesText, string noText, Action yesAction, Action noAction)
    {
        ResolveReferencesIfNeeded();
        RegisterButtonEvents();

        onYes = yesAction;
        onNo = noAction;

        SetMessage(message);
        SetButtonTexts(yesText, noText);
    }

    public void OnClickYes()
    {
        Action callback = onYes;
        callback?.Invoke();
    }

    public void OnClickNo()
    {
        Action callback = onNo;
        callback?.Invoke();
    }

    public void ClearButtonAnimationState()
    {
        ButtonAnimationCoroutine[] buttonAnimations = GetComponentsInChildren<ButtonAnimationCoroutine>(true);

        for (int i = 0; i < buttonAnimations.Length; i++)
        {
            if (buttonAnimations[i] != null)
                buttonAnimations[i].ForceClearState(false);
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null &&
            eventSystem.currentSelectedGameObject != null &&
            eventSystem.currentSelectedGameObject.transform.IsChildOf(transform))
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    private void SetMessage(string message)
    {
        if (tmpMessageText != null)
            tmpMessageText.text = message;

        if (legacyMessageText != null)
            legacyMessageText.text = message;
    }

    private void SetButtonTexts(string yesText, string noText)
    {
        if (tmpYesButtonText != null)
            tmpYesButtonText.text = yesText;

        if (legacyYesButtonText != null)
            legacyYesButtonText.text = yesText;

        if (tmpNoButtonText != null)
            tmpNoButtonText.text = noText;

        if (legacyNoButtonText != null)
            legacyNoButtonText.text = noText;
    }

    private void RegisterButtonEvents()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnClickYes);
            yesButton.onClick.AddListener(OnClickYes);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(OnClickNo);
            noButton.onClick.AddListener(OnClickNo);
        }
    }

    private void UnregisterButtonEvents()
    {
        if (yesButton != null)
            yesButton.onClick.RemoveListener(OnClickYes);

        if (noButton != null)
            noButton.onClick.RemoveListener(OnClickNo);
    }

    private void ResolveReferencesIfNeeded()
    {
        if (!autoBindReferences)
            return;

        ResolveMessageTextIfNeeded();
        ResolveButtonsIfNeeded();
        ResolveButtonTextsIfNeeded();
    }

    private void ResolveMessageTextIfNeeded()
    {
        if (tmpMessageText == null)
        {
            TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < tmpTexts.Length; i++)
            {
                if (tmpTexts[i] == null)
                    continue;

                if (IsButtonText(tmpTexts[i].transform))
                    continue;

                tmpMessageText = tmpTexts[i];
                break;
            }
        }

        if (legacyMessageText == null)
        {
            Text[] legacyTexts = GetComponentsInChildren<Text>(true);

            for (int i = 0; i < legacyTexts.Length; i++)
            {
                if (legacyTexts[i] == null)
                    continue;

                if (IsButtonText(legacyTexts[i].transform))
                    continue;

                legacyMessageText = legacyTexts[i];
                break;
            }
        }
    }

    private void ResolveButtonsIfNeeded()
    {
        if (yesButton != null && noButton != null)
            return;

        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            string lowerName = button.gameObject.name.ToLowerInvariant();

            if (yesButton == null &&
                (lowerName.Contains("yes") || lowerName.Contains("ok") || lowerName.Contains("confirm") || lowerName.Contains("accept")))
            {
                yesButton = button;
                continue;
            }

            if (noButton == null &&
                (lowerName.Contains("no") || lowerName.Contains("cancel") || lowerName.Contains("close") || lowerName.Contains("back")))
            {
                noButton = button;
                continue;
            }
        }

        if (yesButton == null && buttons.Length > 0)
            yesButton = buttons[0];

        if (noButton == null && buttons.Length > 1)
            noButton = buttons[1];
    }

    private void ResolveButtonTextsIfNeeded()
    {
        if (yesButton != null)
        {
            if (tmpYesButtonText == null)
                tmpYesButtonText = yesButton.GetComponentInChildren<TMP_Text>(true);

            if (legacyYesButtonText == null)
                legacyYesButtonText = yesButton.GetComponentInChildren<Text>(true);
        }

        if (noButton != null)
        {
            if (tmpNoButtonText == null)
                tmpNoButtonText = noButton.GetComponentInChildren<TMP_Text>(true);

            if (legacyNoButtonText == null)
                legacyNoButtonText = noButton.GetComponentInChildren<Text>(true);
        }
    }

    private bool IsButtonText(Transform textTransform)
    {
        if (textTransform == null)
            return false;

        return textTransform.GetComponentInParent<Button>() != null;
    }
}
