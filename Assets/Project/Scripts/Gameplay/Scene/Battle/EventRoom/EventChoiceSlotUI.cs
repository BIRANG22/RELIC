using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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

    [Header("Choice Description Rarity Colors")]
    [SerializeField] private Color commonTextColor = new Color32(220, 220, 220, 255);
    [SerializeField] private Color rareTextColor = new Color32(83, 156, 233, 255);
    [SerializeField] private Color epicTextColor = new Color32(181, 103, 229, 255);
    [SerializeField] private Color uniqueTextColor = new Color32(244, 185, 75, 255);

    [Header("Choice Description Number Color")]
    [SerializeField] private Color numberTextColor = new Color32(255, 201, 120, 255);

    private static readonly Regex ChoiceDescriptionColorPattern = new Regex(
        @"<[^>]*>|[0-9]+(?:[.,][0-9]+)*|일반|레어|에픽|유니크", RegexOptions.Compiled);

    private bool boundSelectable;
    private EventData boundChoice;
    private int selectedPartySlot;
    private string boundUnavailableReason;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady -= OnLocaleChanged;
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady += OnLocaleChanged;
        RefreshDisplayedText();
    }

    private void OnDisable()
    {
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady -= OnLocaleChanged;
    }

    public void Bind(
        EventData choice,
        bool selectable,
        string unavailableReason,
        UnityAction onClick,
        int selectedPartySlot = 0)
    {
        EnsureReferences();

        if (choice == null)
        {
            Clear();
            return;
        }

        boundSelectable = selectable;
        boundChoice = choice;
        this.selectedPartySlot = selectedPartySlot;
        boundUnavailableReason = unavailableReason;

        // Prepare text while the slot is still hidden so the previous choice cannot flash.
        RefreshDisplayedText();

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

        // Reveal only after every label and interaction state has been updated.
        if (root != null && !root.activeSelf)
            root.SetActive(true);
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

    // Trigger the same validated Button.onClick path used by a mouse click.
    public void InvokeShortcut()
    {
        EnsureReferences();
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy ||
            !boundSelectable || boundChoice == null ||
            button == null || !button.IsActive() || !button.IsInteractable())
            return;

        button.onClick.Invoke();
    }

    public void Clear()
    {
        EnsureReferences();
        boundSelectable = false;
        boundChoice = null;
        selectedPartySlot = 0;
        boundUnavailableReason = null;

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

        ProtectRuntimeManagedText(choiceNameText);
        ProtectRuntimeManagedText(choiceDescText);
        ProtectRuntimeManagedText(unavailableReasonText);

        if (disabledRoot == null)
        {
            Transform disabledTransform = FindChildRecursive(transform, "DisabledRoot");
            if (disabledTransform != null)
                disabledRoot = disabledTransform.gameObject;
        }
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (isActiveAndEnabled)
            RefreshDisplayedText();
    }

    private string ResolvePartyNames(string text)
    {
        if (string.IsNullOrEmpty(text) || DataManager.Instance?.PartyRuntimeStore == null)
            return text ?? string.Empty;
        // Event_08_A uses the party member selected in the previous event step.
        // Session slot is 1-based, while PartyRuntimeStore is 0-based.
        if (text.Contains("{PartyName}") && selectedPartySlot >= 1 && selectedPartySlot <= 3)
        {
            string selectedId = DataManager.Instance.PartyRuntimeStore.GetCharacterId(selectedPartySlot - 1);
            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                string selectedName = selectedId;
                if (DataManager.Instance.CharacterDatabase != null &&
                    DataManager.Instance.CharacterDatabase.TryGet(selectedId, out CharacterMasterData selectedMaster) &&
                    selectedMaster != null)
                    selectedName = GameDataLocalization.CharacterName(selectedMaster);
                text = text.Replace("{PartyName}", selectedName);
            }
        }
        for (int i = 1; i <= 3; i++)
        {
            string token = "{Party" + i + "Name}";
            if (!text.Contains(token)) continue;
            string id = DataManager.Instance.PartyRuntimeStore.GetCharacterId(i - 1);
            string name = id ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(id) && DataManager.Instance.CharacterDatabase != null &&
                DataManager.Instance.CharacterDatabase.TryGet(id, out CharacterMasterData master))
                name = GameDataLocalization.CharacterName(master);
            bool korean = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale?.Identifier.Code
                ?.StartsWith("ko", System.StringComparison.OrdinalIgnoreCase) == true;
            string suffix = string.Empty;
            if (korean && !string.IsNullOrEmpty(name))
            {
                char last = name[name.Length - 1];
                suffix = last >= 0xAC00 && last <= 0xD7A3 && (last - 0xAC00) % 28 != 0 ? "이" : "가";
            }
            text = text.Replace(token + "{이가}", name + suffix).Replace(token, name);
        }
        return text;
    }

    private void RefreshDisplayedText()
    {
        if (boundChoice == null)
            return;

        if (choiceNameText != null)
        {
            choiceNameText.text = ResolvePartyNames(GameDataLocalization.EventChoiceName(boundChoice));
        }

        string displayedChoiceDesc = boundSelectable
            ? ResolvePartyNames(GameDataLocalization.EventChoiceDescription(boundChoice))
            : (!string.IsNullOrWhiteSpace(boundChoice.UnavailableChoiceDesc)
                ? GameDataLocalization.EventUnavailableChoiceDescription(boundChoice)
                : boundUnavailableReason);

        if (choiceDescText != null)
        {
            // Preserve actual line breaks from the Event sheet and support literal \n from CSV.
            choiceDescText.text = ApplyChoiceDescriptionColors(NormalizeEventLineBreaks(displayedChoiceDesc));
            choiceDescText.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    private string ApplyChoiceDescriptionColors(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return ChoiceDescriptionColorPattern.Replace(text, match =>
        {
            // Keep TMP rich-text tags (including pre-existing color tags) intact.
            if (match.Value[0] == '<')
                return match.Value;

            if (match.Value[0] >= '0' && match.Value[0] <= '9')
                return "<color=#" + ColorUtility.ToHtmlStringRGB(numberTextColor) + ">" +
                    match.Value + "</color>";

            string rarity;
            Color fallback;
            switch (match.Value)
            {
                case "일반": rarity = "Common"; fallback = commonTextColor; break;
                case "레어": rarity = "Rare"; fallback = rareTextColor; break;
                case "에픽": rarity = "Epic"; fallback = epicTextColor; break;
                case "유니크": rarity = "Unique"; fallback = uniqueTextColor; break;
                default: return match.Value;
            }

            // Prefer the game's existing rarity palette when it is available.
            Color color = RecordPanelUI.TryGetCachedRarityDisplayColor(rarity, out Color shared)
                ? shared : fallback;
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" +
                match.Value + "</color>";
        });
    }

    private static string NormalizeEventLineBreaks(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Excel cell line feeds and escaped CSV newlines should render identically.
        return value.Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace("\\r\\n", "\n")
            .Replace("\\n", "\n");
    }

    private static void ProtectRuntimeManagedText(TMP_Text text)
    {
        if (text == null)
            return;

        // Choice labels are driven by the bound EventData, never by a prefab's
        // static localization binding. A stale async refresh must not overwrite them.
        LocalizedTMPText.ProtectDynamicOwnership(text);

        DynamicLocalizedTMPText dynamicWriter = text.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicWriter != null)
            dynamicWriter.enabled = false;
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
