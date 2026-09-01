using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public readonly struct EventSkillAwakenSelectionPanelEntry
{
    public EventSkillAwakenSelectionPanelEntry(
        EventChoiceSkillAwakenTarget target,
        string characterName,
        string slotName,
        string skillName,
        string upgradeSkillName,
        Sprite icon = null)
    {
        Target = target;
        CharacterName = Normalize(characterName);
        SlotName = Normalize(slotName);
        SkillName = Normalize(skillName);
        UpgradeSkillName = Normalize(upgradeSkillName);
        Icon = icon;
    }

    public EventChoiceSkillAwakenTarget Target { get; }
    public string CharacterName { get; }
    public string SlotName { get; }
    public string SkillName { get; }
    public string UpgradeSkillName { get; }
    public Sprite Icon { get; }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

public sealed class EventSkillAwakenSelectionPanelUI : MonoBehaviour
{
    private const string BeforeCostIconName = "BeforeCostIcon";
    private const string AfterCostIconName = "AfterCostIcon";
    [Header("Scene References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject optionTemplate;
    [SerializeField] private int columnCount = 3;
    [SerializeField] private Vector2 optionSpacing = new(12f, 12f);


    [Header("Resource Icons")]
    [SerializeField] private Sprite costResourceIcon;
    [SerializeField] private Sprite hpResourceIcon;
    [SerializeField] private Sprite uniqueResourceIcon;
    [SerializeField] private Sprite moveResourceIcon;

    [Header("After Overlay")]
    [SerializeField] private int afterOverlaySortingOrder = 200;

    [Header("Panel Fade")]
    [SerializeField, Min(0.01f)] private float panelFadeDuration = 0.25f;

    [Header("Result Skill")]
    [SerializeField] private GameObject resultSkillRoot;
    [SerializeField] private float resultStartScale = 2f;
    [SerializeField] private float resultPunchScale = 2.2f;
    [SerializeField, Min(0.01f)] private float resultScaleDuration = 0.18f;
    [SerializeField, Min(0f)] private float resultHoldDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float resultFadeDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float resultFailureWipeDuration = 0.45f;
    [SerializeField, Min(0f)] private float resultFailureWipeSoftness = 36f;
    [SerializeField] private Button checkButton;

    private readonly List<EventSkillAwakenSelectionPanelEntry> entries = new();
    private readonly List<GameObject> optionObjects = new();
    private Func<EventChoiceSkillAwakenTarget, bool> selectedCallback;
    private Action cancelCallback;
    private Action closedCallback;
    private CanvasGroup panelCanvasGroup;
    private Coroutine panelFadeRoutine;
    private CanvasGroup resultSkillCanvasGroup;
    private CanvasGroup checkButtonCanvasGroup;
    private bool resultCheckRequested;
    private GameObject resultSkillWipeMaskRoot;
    private RectMask2D resultSkillWipeMask;
    private Transform resultSkillOriginalParent;
    private int resultSkillOriginalSiblingIndex = -1;
    private Vector2 resultSkillOriginalAnchorMin;
    private Vector2 resultSkillOriginalAnchorMax;
    private Vector2 resultSkillOriginalPivot;
    private Vector2 resultSkillOriginalAnchoredPosition;
    private Vector2 resultSkillOriginalSizeDelta;
    private Vector3 resultSkillOriginalLocalScale;
    private Quaternion resultSkillOriginalLocalRotation;
    private bool resultSkillLayoutCached;
    private bool isClosing;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public int VisibleOptionCount { get; private set; }

    private void Awake()
    {
        RegisterCancelButton();
        RegisterCheckButton();
        CloseImmediate(false);
    }

    private void OnDestroy()
    {
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(CancelSelection);
        if (checkButton != null)
            checkButton.onClick.RemoveListener(OnCheckButtonClicked);
    }

    public bool Open(
        IEnumerable<EventSkillAwakenSelectionPanelEntry> candidates,
        Func<EventChoiceSkillAwakenTarget, bool> onSelected,
        Action onCancelled,
        Action onClosed = null)
    {
        if (!HasRequiredSceneReferences())
        {
            Close();
            return false;
        }

        gameObject.SetActive(true);
        RegisterCancelButton();

        entries.Clear();
        if (candidates != null)
        {
            foreach (EventSkillAwakenSelectionPanelEntry entry in candidates)
            {
                if (entry.Target.IsValid)
                    entries.Add(entry);
            }
        }

        selectedCallback = onSelected;
        cancelCallback = onCancelled;
        closedCallback = onClosed;
        isClosing = false;

        optionTemplate.SetActive(false);
        SetSelectionVisualsActive(true);
        HideResultSkillImmediate();
        panelRoot.SetActive(true);
        transform.SetAsLastSibling();
        RefreshOptions();
        StartPanelFadeIn();
        return true;
    }

    public bool TrySelect(EventChoiceSkillAwakenTarget target)
    {
        if (!IsOpen || !ContainsTarget(target))
            return false;

        Func<EventChoiceSkillAwakenTarget, bool> callback = selectedCallback;
        bool accepted = callback == null || callback.Invoke(target);
        if (accepted && IsOpen)
            Close();

        return accepted;
    }

    public System.Collections.IEnumerator PlayResultSkill(
        EventChoiceSkillAwakenTarget target,
        bool succeeded)
    {
        EnsureResultSkillReference();
        if (resultSkillRoot == null || panelRoot == null || !target.IsValid)
            yield break;

        MoveResultSkillToOverlay();
        EnsureResultSkillComponents();
        EnsureCheckButtonReference();
        ResetResultSkillVisualState();
        HideCheckButtonImmediate();
        BindResultSkill(target.SkillId);
        resultSkillRoot.SetActive(true);

        RectTransform resultRect = resultSkillRoot.GetComponent<RectTransform>();
        if (resultRect != null)
            resultRect.localScale = Vector3.one * resultStartScale;

        yield return ScaleResultSkill(resultRect, resultStartScale, resultPunchScale, resultScaleDuration);

        if (succeeded)
        {
            BindResultSkill(target.UpgradeSkillId);

            if (resultHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(resultHoldDuration);

            resultCheckRequested = false;
            ShowCheckButton();
            yield return new WaitUntil(() => resultCheckRequested);
            yield return FadeSuccessResultAndCheckButton(resultFadeDuration);
        }
        else
        {
            if (resultHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(resultHoldDuration);

            yield return WipeResultSkillLeftToRight(resultFailureWipeDuration * 0.5f);
        }

        HideCheckButtonImmediate();
        HideResultSkillImmediate();
    }

    public void CancelSelection()
    {
        if (!IsOpen)
            return;

        Action callback = cancelCallback;
        Close();
        callback?.Invoke();
    }

    public void Close()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
        {
            CloseImmediate(false);
            return;
        }

        if (isClosing)
            return;

        isClosing = true;
        EnsurePanelCanvasGroup();

        if (panelFadeRoutine != null)
            StopCoroutine(panelFadeRoutine);

        panelFadeRoutine = StartCoroutine(FadeOutAndCloseRoutine());
    }

    private void StartPanelFadeIn()
    {
        EnsurePanelCanvasGroup();
        if (panelCanvasGroup == null)
            return;

        if (panelFadeRoutine != null)
            StopCoroutine(panelFadeRoutine);

        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        panelFadeRoutine = StartCoroutine(FadePanelRoutine(0f, 1f, true));
    }

    private System.Collections.IEnumerator FadeOutAndCloseRoutine()
    {
        float from = panelCanvasGroup != null ? panelCanvasGroup.alpha : 1f;
        yield return FadePanelRoutine(from, 0f, false);
        CloseImmediate(true);
    }

    private System.Collections.IEnumerator FadePanelRoutine(float from, float to, bool enableInteractionAtEnd)
    {
        if (panelCanvasGroup == null)
            yield break;

        float duration = Mathf.Max(0.01f, panelFadeDuration);
        float elapsed = 0f;
        panelCanvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            panelCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        panelCanvasGroup.alpha = to;
        panelCanvasGroup.interactable = enableInteractionAtEnd;
        panelCanvasGroup.blocksRaycasts = enableInteractionAtEnd;
        panelFadeRoutine = null;
    }

    private void CloseImmediate(bool notifyClosed)
    {
        if (panelFadeRoutine != null)
        {
            StopCoroutine(panelFadeRoutine);
            panelFadeRoutine = null;
        }

        Action notify = notifyClosed ? closedCallback : null;

        ClearOptionObjects();
        entries.Clear();
        selectedCallback = null;
        cancelCallback = null;
        closedCallback = null;
        VisibleOptionCount = 0;
        isClosing = false;

        if (emptyText != null)
            emptyText.gameObject.SetActive(false);

        if (optionTemplate != null)
            optionTemplate.SetActive(false);

        HideCheckButtonImmediate();
        HideResultSkillImmediate();
        SetSelectionVisualsActive(true);
        EnsurePanelCanvasGroup();
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);

        notify?.Invoke();
    }

    private void EnsureCheckButtonReference()
    {
        if (checkButton == null)
        {
            Transform searchRoot = panelRoot != null ? panelRoot.transform.parent : transform.parent;
            Transform found = FindChildRecursive(searchRoot, "CheckButton");
            if (found != null)
                checkButton = found.GetComponent<Button>();
        }

        RegisterCheckButton();

        if (checkButton != null &&
            (checkButtonCanvasGroup == null || checkButtonCanvasGroup.gameObject != checkButton.gameObject))
        {
            checkButtonCanvasGroup = checkButton.GetComponent<CanvasGroup>();
            if (checkButtonCanvasGroup == null)
                checkButtonCanvasGroup = checkButton.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void RegisterCheckButton()
    {
        if (checkButton == null)
            return;

        checkButton.onClick.RemoveListener(OnCheckButtonClicked);
        checkButton.onClick.AddListener(OnCheckButtonClicked);
    }

    private void OnCheckButtonClicked()
    {
        if (checkButton != null)
            checkButton.interactable = false;

        resultCheckRequested = true;
    }

    private void ShowCheckButton()
    {
        EnsureCheckButtonReference();
        if (checkButton == null)
        {
            // 버튼을 찾지 못해 결과 연출이 영구 대기하지 않도록 자동 진행합니다.
            resultCheckRequested = true;
            return;
        }

        checkButton.gameObject.SetActive(true);
        checkButton.interactable = true;
        if (checkButtonCanvasGroup != null)
        {
            checkButtonCanvasGroup.alpha = 1f;
            checkButtonCanvasGroup.interactable = true;
            checkButtonCanvasGroup.blocksRaycasts = true;
        }
    }

    private void HideCheckButtonImmediate()
    {
        EnsureCheckButtonReference();
        resultCheckRequested = false;

        if (checkButton == null)
            return;

        checkButton.interactable = false;
        if (checkButtonCanvasGroup != null)
        {
            checkButtonCanvasGroup.alpha = 0f;
            checkButtonCanvasGroup.interactable = false;
            checkButtonCanvasGroup.blocksRaycasts = false;
        }
        checkButton.gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator FadeSuccessResultAndCheckButton(float duration)
    {
        EnsureResultSkillComponents();
        EnsureCheckButtonReference();

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        float resultFrom = resultSkillCanvasGroup != null ? resultSkillCanvasGroup.alpha : 1f;
        float buttonFrom = checkButtonCanvasGroup != null ? checkButtonCanvasGroup.alpha : 1f;

        if (checkButton != null)
            checkButton.interactable = false;
        if (checkButtonCanvasGroup != null)
        {
            checkButtonCanvasGroup.interactable = false;
            checkButtonCanvasGroup.blocksRaycasts = false;
        }

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (resultSkillCanvasGroup != null)
                resultSkillCanvasGroup.alpha = Mathf.Lerp(resultFrom, 0f, eased);
            if (checkButtonCanvasGroup != null)
                checkButtonCanvasGroup.alpha = Mathf.Lerp(buttonFrom, 0f, eased);

            yield return null;
        }

        if (resultSkillCanvasGroup != null)
            resultSkillCanvasGroup.alpha = 0f;
        if (checkButtonCanvasGroup != null)
            checkButtonCanvasGroup.alpha = 0f;
    }

    private void EnsureResultSkillReference()
    {
        if (resultSkillRoot == null)
        {
            Transform root = panelRoot != null ? panelRoot.transform : transform;
            Transform found = FindChildRecursive(root, "Result_Skill");
            if (found != null)
                resultSkillRoot = found.gameObject;
        }

        CacheResultSkillOriginalLayout();
    }

    private void CacheResultSkillOriginalLayout()
    {
        if (resultSkillLayoutCached || resultSkillRoot == null)
            return;

        RectTransform rect = resultSkillRoot.GetComponent<RectTransform>();
        if (rect == null)
            return;

        resultSkillOriginalParent = rect.parent;
        resultSkillOriginalSiblingIndex = rect.GetSiblingIndex();
        resultSkillOriginalAnchorMin = rect.anchorMin;
        resultSkillOriginalAnchorMax = rect.anchorMax;
        resultSkillOriginalPivot = rect.pivot;
        resultSkillOriginalAnchoredPosition = rect.anchoredPosition;
        resultSkillOriginalSizeDelta = rect.sizeDelta;
        resultSkillOriginalLocalScale = rect.localScale;
        resultSkillOriginalLocalRotation = rect.localRotation;
        resultSkillLayoutCached = true;
    }

    private void MoveResultSkillToOverlay()
    {
        EnsureResultSkillReference();
        if (resultSkillRoot == null)
            return;

        Transform overlayParent = panelRoot != null ? panelRoot.transform.parent : transform.parent;
        if (overlayParent == null)
            return;

        RectTransform rect = resultSkillRoot.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.SetParent(overlayParent, true);
        rect.SetAsLastSibling();
    }

    private void RestoreResultSkillParent()
    {
        if (!resultSkillLayoutCached || resultSkillRoot == null || resultSkillOriginalParent == null)
            return;

        RectTransform rect = resultSkillRoot.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.SetParent(resultSkillOriginalParent, false);
        rect.anchorMin = resultSkillOriginalAnchorMin;
        rect.anchorMax = resultSkillOriginalAnchorMax;
        rect.pivot = resultSkillOriginalPivot;
        rect.anchoredPosition = resultSkillOriginalAnchoredPosition;
        rect.sizeDelta = resultSkillOriginalSizeDelta;
        rect.localScale = resultSkillOriginalLocalScale;
        rect.localRotation = resultSkillOriginalLocalRotation;

        int maxIndex = Mathf.Max(0, rect.parent.childCount - 1);
        rect.SetSiblingIndex(Mathf.Clamp(resultSkillOriginalSiblingIndex, 0, maxIndex));
    }

    private void EnsureResultSkillComponents()
    {
        EnsureResultSkillReference();
        if (resultSkillRoot == null)
            return;

        if (resultSkillCanvasGroup == null || resultSkillCanvasGroup.gameObject != resultSkillRoot)
        {
            resultSkillCanvasGroup = resultSkillRoot.GetComponent<CanvasGroup>();
            if (resultSkillCanvasGroup == null)
                resultSkillCanvasGroup = resultSkillRoot.AddComponent<CanvasGroup>();
        }

    }

    private void SetSelectionVisualsActive(bool active)
    {
        if (titleText != null)
            titleText.gameObject.SetActive(active);
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(active);
        if (contentRoot != null)
            contentRoot.gameObject.SetActive(active);
        if (emptyText != null && !active)
            emptyText.gameObject.SetActive(false);
    }

    private void BindResultSkill(string skillId)
    {
        EnsureResultSkillReference();
        if (resultSkillRoot == null)
            return;

        SkillMasterData skill = ResolveSkill(skillId);
        Transform root = resultSkillRoot.transform;
        TMP_Text skillNameText = FindText(root, "SkillNameText");
        TMP_Text rarityText = FindText(root, "RarityText");
        Image iconImage = FindImage(root, "Icon");

        if (skillNameText != null)
            skillNameText.text = GetSkillDisplayName(skill, string.Empty, skillId);

        if (rarityText != null)
        {
            rarityText.text = skill != null ? SkillRarityUtility.GetMemoryTypeDisplayName(skill) : string.Empty;
            rarityText.color = ResolveRarityColor(skill);
        }

        SetImage(iconImage, SkillIconUtility.GetSkillIcon(skillId));
        if (iconImage != null)
            SkillUpgradeMarkStyle.ApplyShared(iconImage, skill);

        BindSkillState(root, "Before", skill);
    }

    private void ResetResultSkillVisualState()
    {
        EnsureResultSkillComponents();
        if (resultSkillCanvasGroup != null)
        {
            resultSkillCanvasGroup.alpha = 1f;
            resultSkillCanvasGroup.interactable = false;
            resultSkillCanvasGroup.blocksRaycasts = false;
        }

    }

    private void HideResultSkillImmediate()
    {
        EnsureResultSkillReference();
        if (resultSkillRoot == null)
            return;

        ResetResultSkillVisualState();
        RectTransform rect = resultSkillRoot.GetComponent<RectTransform>();
        if (rect != null)
            rect.localScale = Vector3.one * resultStartScale;
        resultSkillRoot.SetActive(false);
        DestroyResultSkillWipeMask();
        RestoreResultSkillParent();
    }

    private static System.Collections.IEnumerator ScaleResultSkill(
        RectTransform rect,
        float from,
        float to,
        float duration)
    {
        if (rect == null)
            yield break;

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float scale = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            rect.localScale = Vector3.one * scale;
            yield return null;
        }

        rect.localScale = Vector3.one * to;
    }

    private System.Collections.IEnumerator FadeResultSkill(float from, float to, float duration)
    {
        EnsureResultSkillComponents();
        if (resultSkillCanvasGroup == null)
            yield break;

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        resultSkillCanvasGroup.alpha = from;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            resultSkillCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        resultSkillCanvasGroup.alpha = to;
    }

    private System.Collections.IEnumerator WipeResultSkillLeftToRight(float duration)
    {
        if (resultSkillRoot == null)
            yield break;

        RectTransform resultRect = resultSkillRoot.GetComponent<RectTransform>();
        if (resultRect == null)
            yield break;

        CreateResultSkillWipeMask(resultRect);
        if (resultSkillWipeMask == null || resultSkillWipeMaskRoot == null)
            yield break;

        RectTransform maskRect = resultSkillWipeMaskRoot.GetComponent<RectTransform>();
        if (maskRect == null)
            yield break;

        Canvas.ForceUpdateCanvases();
        float width = Mathf.Max(1f, maskRect.rect.width);
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        resultSkillWipeMask.padding = Vector4.zero;
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float left = Mathf.Lerp(0f, width + resultFailureWipeSoftness, Mathf.SmoothStep(0f, 1f, t));
            resultSkillWipeMask.padding = new Vector4(left, 0f, 0f, 0f);
            yield return null;
        }

        resultSkillWipeMask.padding = new Vector4(width + resultFailureWipeSoftness, 0f, 0f, 0f);
    }

    private void CreateResultSkillWipeMask(RectTransform resultRect)
    {
        DestroyResultSkillWipeMask();
        if (resultRect == null || resultRect.parent == null)
            return;

        Transform parent = resultRect.parent;
        GameObject maskObject = new GameObject("Result_Skill_WipeMask", typeof(RectTransform), typeof(RectMask2D));
        RectTransform maskRect = maskObject.GetComponent<RectTransform>();
        maskRect.SetParent(parent, false);
        maskRect.anchorMin = resultRect.anchorMin;
        maskRect.anchorMax = resultRect.anchorMax;
        maskRect.pivot = resultRect.pivot;
        maskRect.anchoredPosition = resultRect.anchoredPosition;
        maskRect.sizeDelta = resultRect.sizeDelta;
        maskRect.localRotation = resultRect.localRotation;
        maskRect.localScale = resultRect.localScale;
        maskRect.SetSiblingIndex(resultRect.GetSiblingIndex());

        Vector2 originalPivot = resultRect.pivot;
        resultRect.SetParent(maskRect, false);
        resultRect.anchorMin = Vector2.zero;
        resultRect.anchorMax = Vector2.one;
        resultRect.pivot = originalPivot;
        resultRect.anchoredPosition = Vector2.zero;
        resultRect.sizeDelta = Vector2.zero;
        resultRect.localRotation = Quaternion.identity;
        resultRect.localScale = Vector3.one;

        resultSkillWipeMaskRoot = maskObject;
        resultSkillWipeMask = maskObject.GetComponent<RectMask2D>();
        int softness = Mathf.RoundToInt(Mathf.Max(0f, resultFailureWipeSoftness));
        resultSkillWipeMask.softness = new Vector2Int(softness, 0);
        resultSkillWipeMask.padding = Vector4.zero;
    }

    private void DestroyResultSkillWipeMask()
    {
        if (resultSkillWipeMaskRoot == null)
        {
            resultSkillWipeMask = null;
            return;
        }

        if (resultSkillRoot != null && resultSkillLayoutCached)
        {
            RectTransform resultRect = resultSkillRoot.GetComponent<RectTransform>();
            if (resultRect != null && resultRect.parent == resultSkillWipeMaskRoot.transform)
            {
                Transform overlayParent = panelRoot != null ? panelRoot.transform.parent : transform.parent;
                if (overlayParent != null)
                    resultRect.SetParent(overlayParent, true);
            }
        }

        Destroy(resultSkillWipeMaskRoot);
        resultSkillWipeMaskRoot = null;
        resultSkillWipeMask = null;
    }

    private void EnsurePanelCanvasGroup()
    {
        if (panelRoot == null)
            return;

        if (panelCanvasGroup == null || panelCanvasGroup.gameObject != panelRoot)
        {
            panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
        }
    }

    private bool HasRequiredSceneReferences()
    {
        return panelRoot != null &&
               contentRoot != null &&
               optionTemplate != null;
    }

    private void RefreshOptions()
    {
        ClearOptionObjects();
        VisibleOptionCount = 0;

        if (titleText != null)
            titleText.text = "강화할 기억 선택";

        if (emptyText != null)
            emptyText.gameObject.SetActive(entries.Count == 0);

        for (int i = 0; i < entries.Count; i++)
        {
            GameObject optionObject = CreateOptionObject(entries[i], i);
            optionObjects.Add(optionObject);
            VisibleOptionCount++;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    private GameObject CreateOptionObject(EventSkillAwakenSelectionPanelEntry entry, int optionIndex)
    {
        GameObject optionObject = Instantiate(optionTemplate, contentRoot);
        optionObject.name = "SkillAwakenOption";
        optionObject.SetActive(true);
        PositionOptionObject(optionObject, optionIndex);

        SkillMasterData beforeSkill = ResolveSkill(entry.Target.SkillId);
        SkillMasterData afterSkill = ResolveSkill(entry.Target.UpgradeSkillId);

        TMP_Text skillNameText = FindText(optionObject.transform, "SkillNameText");
        if (skillNameText == null)
            skillNameText = FindText(optionObject.transform, "RelicNameText");

        TMP_Text rarityText = FindText(optionObject.transform, "RarityText");
        Image iconImage = FindImage(optionObject.transform, "Icon");

        if (skillNameText != null)
        {
            skillNameText.text = beforeSkill != null
                ? GetSkillDisplayName(beforeSkill, entry.SkillName, entry.Target.SkillId)
                : (string.IsNullOrWhiteSpace(entry.SkillName) ? entry.Target.SkillId : entry.SkillName);
        }

        if (rarityText != null)
        {
            rarityText.text = beforeSkill != null
                ? SkillRarityUtility.GetMemoryTypeDisplayName(beforeSkill)
                : string.Empty;
            rarityText.color = ResolveRarityColor(beforeSkill);
        }

        Sprite skillIcon = entry.Icon != null
            ? entry.Icon
            : SkillIconUtility.GetSkillIcon(entry.Target.SkillId);
        SetImage(iconImage, skillIcon);
        SkillUpgradeMarkStyle.ApplyShared(iconImage, entry.Target.SkillId);

        BindSkillState(optionObject.transform, "Before", beforeSkill);
        BindSkillState(optionObject.transform, "After", afterSkill);

        Transform afterRoot = FindChildRecursive(optionObject.transform, "After");
        if (afterRoot != null)
        {
            afterRoot.gameObject.SetActive(false);
            EventSkillAwakenOptionHoverUI hover = optionObject.GetComponent<EventSkillAwakenOptionHoverUI>();
            if (hover == null)
                hover = optionObject.AddComponent<EventSkillAwakenOptionHoverUI>();
            hover.Configure(afterRoot.gameObject, panelRoot.transform, afterOverlaySortingOrder);
        }

        Button button = optionObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            EventChoiceSkillAwakenTarget captured = entry.Target;
            button.onClick.AddListener(() => TrySelect(captured));
        }

        return optionObject;
    }

    private void BindSkillState(Transform optionRoot, string prefix, SkillMasterData skill)
    {
        Image costIcon = FindImage(optionRoot, prefix + "CostIcon");
        TMP_Text costText = FindText(optionRoot, prefix + "CostText");
        Image rangeImage = FindImage(optionRoot, prefix + "Range");
        TMP_Text effectText = FindText(optionRoot, prefix + "Text");

        if (skill == null)
        {
            SetImage(costIcon, null);
            SetImage(rangeImage, null);
            if (costText != null)
                costText.text = string.Empty;
            if (effectText != null)
                effectText.text = string.Empty;
            return;
        }

        Sprite resourceIcon = ResolveResourceIcon(skill.ReferenceResource);
        if (resourceIcon != null)
            SetImage(costIcon, resourceIcon);
        else if (costIcon != null)
            costIcon.enabled = costIcon.sprite != null;

        if (costText != null)
            costText.text = Mathf.Max(0, skill.ResourceCostValue).ToString();

        SetImage(rangeImage, ResolveRangeIcon(skill.RangeId));

        if (effectText != null)
            effectText.text = BuildEffectText(skill);
    }

    private static SkillMasterData ResolveSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData skill)
            ? skill
            : null;
    }

    private static string GetSkillDisplayName(SkillMasterData skill, string fallbackName, string fallbackId)
    {
        if (skill != null)
        {
            string localized = GameDataLocalization.SkillName(skill);
            if (!string.IsNullOrWhiteSpace(localized))
                return localized;
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
            return fallbackName;

        return fallbackId ?? string.Empty;
    }

    private static string BuildEffectText(SkillMasterData skill)
    {
        if (skill == null)
            return string.Empty;

        List<SkillEffectEntry> effects = skill.EffectEntries;
        if ((effects == null || effects.Count == 0) && DataManager.Instance != null)
        {
            effects = SkillEffectParser.Parse(skill, DataManager.Instance.EffectDatabase);
        }

        if (effects == null || effects.Count == 0)
            return string.Empty;

        List<string> lines = new(effects.Count);
        for (int i = 0; i < effects.Count; i++)
        {
            SkillEffectEntry effect = effects[i];
            if (effect == null || string.IsNullOrWhiteSpace(effect.EffectId))
                continue;

            string effectName = GetEffectDisplayName(effect);
            int value = effect.ValueAmount != 0 ? effect.ValueAmount : effect.CountAmount;
            lines.Add(value != 0 ? $"{effectName} {value}" : effectName);
        }

        return string.Join("\n", lines);
    }

    private static string GetEffectDisplayName(SkillEffectEntry effect)
    {
        if (effect == null)
            return string.Empty;

        string effectName = effect.EffectData != null && !string.IsNullOrWhiteSpace(effect.EffectData.Name)
            ? GameDataLocalization.EffectName(effect.EffectData)
            : effect.EffectId;

        string normalized = effectName.Replace(" ", string.Empty).ToLowerInvariant();
        if (normalized.Contains("타격") || normalized.Contains("strike"))
            return GameLocalization.Get("common.damage", "피해");

        return effectName;
    }

    private Sprite ResolveResourceIcon(ReferenceResource resource)
    {
        return resource switch
        {
            ReferenceResource.HP => hpResourceIcon,
            ReferenceResource.UniqueResource => uniqueResourceIcon,
            ReferenceResource.MovePoint => moveResourceIcon,
            _ => costResourceIcon
        };
    }

    private static Sprite ResolveRangeIcon(string rangeId)
    {
        if (string.IsNullOrWhiteSpace(rangeId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillRangeIconDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(rangeId.Trim(), out Sprite icon)
            ? icon
            : null;
    }

    private static Color ResolveRarityColor(SkillMasterData skill)
    {
        if (skill == null)
            return Color.white;

        string canonical = SkillRarityUtility.GetCanonicalName(skill.Rarity);
        if (RecordPanelUI.TryGetCachedRarityDisplayColor(canonical, out Color cachedColor))
            return cachedColor;

        RecordPanelUI[] recordPanels = Resources.FindObjectsOfTypeAll<RecordPanelUI>();
        for (int i = 0; i < recordPanels.Length; i++)
        {
            RecordPanelUI panel = recordPanels[i];
            if (panel != null)
                return panel.GetRarityDisplayColor(canonical);
        }

        return Color.white;
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

    private void PositionOptionObject(GameObject optionObject, int optionIndex)
    {
        RectTransform templateRect = optionTemplate.GetComponent<RectTransform>();
        RectTransform optionRect = optionObject.GetComponent<RectTransform>();
        if (templateRect == null || optionRect == null)
            return;

        int columns = Mathf.Max(1, columnCount);
        int row = optionIndex / columns;
        int column = optionIndex % columns;
        Vector2 cellSize = templateRect.sizeDelta;
        optionRect.anchorMin = templateRect.anchorMin;
        optionRect.anchorMax = templateRect.anchorMax;
        optionRect.pivot = templateRect.pivot;
        optionRect.sizeDelta = cellSize;
        optionRect.anchoredPosition = templateRect.anchoredPosition +
                                      new Vector2(
                                          column * (cellSize.x + optionSpacing.x),
                                          -row * (cellSize.y + optionSpacing.y));

        int requiredRows = row + 1;
        float requiredHeight = requiredRows * cellSize.y +
                               Mathf.Max(0, requiredRows - 1) * optionSpacing.y;
        Vector2 contentSize = contentRoot.sizeDelta;
        contentRoot.sizeDelta = new Vector2(
            contentSize.x,
            Mathf.Max(contentSize.y, requiredHeight));
    }

    private void ClearOptionObjects()
    {
        for (int i = optionObjects.Count - 1; i >= 0; i--)
        {
            GameObject optionObject = optionObjects[i];
            if (optionObject == null)
                continue;

            if (Application.isPlaying)
                Destroy(optionObject);
            else
                DestroyImmediate(optionObject);
        }

        optionObjects.Clear();
    }

    private bool ContainsTarget(EventChoiceSkillAwakenTarget target)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            EventChoiceSkillAwakenTarget option = entries[i].Target;
            if (string.Equals(option.CharacterId, target.CharacterId, StringComparison.Ordinal) &&
                option.SlotKind == target.SlotKind &&
                option.SlotIndex == target.SlotIndex &&
                string.Equals(option.SkillId, target.SkillId, StringComparison.Ordinal) &&
                string.Equals(option.UpgradeSkillId, target.UpgradeSkillId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void RegisterCancelButton()
    {
        if (cancelButton == null)
            return;

        cancelButton.onClick.RemoveListener(CancelSelection);
        cancelButton.onClick.AddListener(CancelSelection);
    }

    private static TMP_Text FindText(Transform root, string targetName)
    {
        Transform target = FindChildRecursive(root, targetName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static Image FindImage(Transform root, string targetName)
    {
        Transform target = FindChildRecursive(root, targetName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (string.Equals(root.name, targetName, StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }
}

public sealed class EventSkillAwakenOptionHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject afterRoot;
    private Transform overlayParent;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Canvas afterCanvas;
    private CanvasGroup afterCanvasGroup;
    private int sortingOrder;

    public void Configure(GameObject targetAfterRoot, Transform targetOverlayParent, int targetSortingOrder)
    {
        afterRoot = targetAfterRoot;
        overlayParent = targetOverlayParent;
        sortingOrder = targetSortingOrder;

        if (afterRoot == null)
            return;

        originalParent = afterRoot.transform.parent;
        originalSiblingIndex = afterRoot.transform.GetSiblingIndex();

        afterCanvas = afterRoot.GetComponent<Canvas>();
        if (afterCanvas == null)
            afterCanvas = afterRoot.AddComponent<Canvas>();

        afterCanvas.overrideSorting = true;
        afterCanvas.sortingOrder = sortingOrder;

        afterCanvasGroup = afterRoot.GetComponent<CanvasGroup>();
        if (afterCanvasGroup == null)
            afterCanvasGroup = afterRoot.AddComponent<CanvasGroup>();
        afterCanvasGroup.interactable = false;
        afterCanvasGroup.blocksRaycasts = false;

        afterRoot.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (afterRoot == null)
            return;

        if (afterCanvas != null)
        {
            afterCanvas.overrideSorting = true;
            afterCanvas.sortingOrder = sortingOrder;
        }

        if (overlayParent != null && afterRoot.transform.parent != overlayParent)
        {
            afterRoot.transform.SetParent(overlayParent, true);
            afterRoot.transform.SetAsLastSibling();
        }

        afterRoot.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideAfter();
    }

    private void OnDisable()
    {
        HideAfter();
    }

    private void HideAfter()
    {
        if (afterRoot == null)
            return;

        afterRoot.SetActive(false);

        if (originalParent != null && afterRoot.transform.parent != originalParent)
        {
            afterRoot.transform.SetParent(originalParent, true);
            int maxIndex = Mathf.Max(0, originalParent.childCount - 1);
            afterRoot.transform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, maxIndex));
        }
    }
}
