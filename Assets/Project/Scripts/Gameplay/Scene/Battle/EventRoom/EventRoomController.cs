using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class EventRoomController : MonoBehaviour
{
    [Header("Chest")]
    [SerializeField] private ChestOpenButton chestOpenButton;

    [Header("Progression")]
    [SerializeField] private GameObject nextButtonRoot;

    [Header("Hover Info Panel")]
    [SerializeField] private GameObject relicHoverInfoPanel;
    [SerializeField] private TMP_Text relicHoverNameText;
    [SerializeField] private TMP_Text relicHoverDescText;

    [Header("Relic Acquire Animation")]
    [SerializeField] private RectTransform relicFlyRoot;
    [SerializeField] private Image relicFlyIconImage;
    [SerializeField] private GameObject relicFlyHighlight;
    [SerializeField] private RectTransform relicSettingButtonTarget;
    [SerializeField] private TMP_Text relicSettingGuideText;

    [SerializeField] private float relicScaleUpDuration = 0.18f;
    [SerializeField] private float relicHoldDuration = 0.15f;
    [SerializeField] private float relicFlyDuration = 0.45f;
    [SerializeField] private float relicStartScale = 1f;
    [SerializeField] private float relicBigScale = 1.35f;
    [SerializeField] private float relicEndScale = 0.25f;
    [SerializeField] private float relicCurveHeight = 180f;

    [Header("SFX")]
    [SerializeField] private bool playAcquireSfx = true;
    [SerializeField] private SfxType acquireSfxType = SfxType.RelicChoiceAcquire;

    [Header("Background Sorting")]
    [SerializeField] private Transform backgroundRoot;
    [SerializeField] private int backgroundSortingOrder = -100;

    private bool isChestOpened;
    private bool isRelicClaimed;
    private Button nextButton;
    private Coroutine relicAcquireRoutine;
    private bool hasRelicFlyRootOriginalState;
    private Vector2 relicFlyRootOriginalAnchoredPosition;
    private Vector3 relicFlyRootOriginalLocalScale;

    private void Awake()
    {
        EnsureReferences();
        ApplyBackgroundSorting();
        BindNextButton();
        CacheRelicFlyRootOriginalState();
        HideRelicHoverInfo();
        HideRelicFlyObjects();
    }

    private void OnEnable()
    {
        EnsureReferences();
        ApplyBackgroundSorting();
        BindNextButton();

        if (relicAcquireRoutine != null)
        {
            StopCoroutine(relicAcquireRoutine);
            relicAcquireRoutine = null;
        }

        CacheRelicFlyRootOriginalState();
        HideRelicHoverInfo();
        HideRelicFlyObjects();

        if (chestOpenButton != null)
            chestOpenButton.ResetForNewEventRoomEntry();

        isChestOpened = false;
        isRelicClaimed = false;
        SetNextButtonVisible(false);
        BindChestEvents();
    }

    private void OnDisable()
    {
        UnbindChestEvents();

        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextButtonClicked);

        if (relicAcquireRoutine != null)
        {
            StopCoroutine(relicAcquireRoutine);
            relicAcquireRoutine = null;
        }

        HideRelicHoverInfo();
        HideRelicFlyObjects();
    }

    public void NotifyChestOpened()
    {
        isChestOpened = true;

        if (chestOpenButton == null || !chestOpenButton.IsAwaitingRewardSelection)
            SetNextButtonVisible(true);
    }

    public void OnNextButtonClicked()
    {
        if (!isChestOpened)
            return;

        if (chestOpenButton != null && chestOpenButton.IsAwaitingRewardSelection && !isRelicClaimed)
            return;

        CompleteCurrentNode();

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ReturnToMap();
        else
            Debug.LogWarning("[EventRoomController] BattleSceneController not found");
    }

    public void ShowRelicHoverInfo(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return;

        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
        {
            Debug.LogWarning("[EventRoomController] DataManager or RelicDatabase is null.");
            return;
        }

        if (!DataManager.Instance.RelicDatabase.TryGet(relicId, out RelicData relicData) || relicData == null)
            return;

        if (relicHoverNameText != null)
            relicHoverNameText.text = relicData.Name;

        if (relicHoverDescText != null)
            relicHoverDescText.text = relicData.EffectDesc;

        if (relicHoverInfoPanel != null)
        {
            relicHoverInfoPanel.transform.SetAsLastSibling();
            relicHoverInfoPanel.SetActive(true);
        }
    }

    public void HideRelicHoverInfo()
    {
        if (relicHoverInfoPanel != null)
            relicHoverInfoPanel.SetActive(false);
    }

    private void OnRelicRewardClaimed(string relicId)
    {
        isRelicClaimed = true;
        HideRelicHoverInfo();
        PlayAcquireSfx();

        if (relicAcquireRoutine != null)
            StopCoroutine(relicAcquireRoutine);

        relicAcquireRoutine = StartCoroutine(PlayRelicAcquireRoutine(relicId));
    }

    private IEnumerator PlayRelicAcquireRoutine(string relicId)
    {
        Sprite relicSprite = GetRelicSprite(relicId);

        if (relicFlyIconImage != null)
        {
            relicFlyIconImage.sprite = relicSprite;
            relicFlyIconImage.enabled = relicSprite != null;
        }

        if (relicFlyRoot != null)
        {
            ResetRelicFlyRootTransform();
            relicFlyRoot.gameObject.SetActive(true);
            relicFlyRoot.localScale = Vector3.one * relicStartScale;
        }

        if (relicFlyHighlight != null)
            relicFlyHighlight.SetActive(true);

        yield return ScaleRelicRoutine(relicStartScale, relicBigScale, relicScaleUpDuration);
        yield return new WaitForSecondsRealtime(relicHoldDuration);

        if (relicFlyHighlight != null)
            relicFlyHighlight.SetActive(false);

        yield return FlyRelicToSettingButtonRoutine();

        HideRelicFlyObjects();

        if (relicSettingGuideText != null)
            relicSettingGuideText.gameObject.SetActive(true);

        SetNextButtonVisible(true);
        relicAcquireRoutine = null;
    }

    private void EnsureReferences()
    {
        if (chestOpenButton == null)
            chestOpenButton = GetComponentInChildren<ChestOpenButton>(true);

        if (relicHoverInfoPanel == null)
        {
            Transform hoverPanel = FindChildRecursive(transform, "RelicHoverInfoPanel");
            if (hoverPanel != null)
                relicHoverInfoPanel = hoverPanel.gameObject;
        }

        if (relicHoverInfoPanel != null)
        {
            TMP_Text[] texts = relicHoverInfoPanel.GetComponentsInChildren<TMP_Text>(true);
            if (relicHoverNameText == null && texts.Length > 0)
                relicHoverNameText = texts[0];
            if (relicHoverDescText == null && texts.Length > 1)
                relicHoverDescText = texts[1];
        }

        if (relicFlyRoot == null)
        {
            Transform flyRoot = FindChildRecursive(transform, "RelicFlyRoot");
            if (flyRoot != null)
                relicFlyRoot = flyRoot as RectTransform;
        }

        if (relicFlyRoot != null && relicFlyIconImage == null)
            relicFlyIconImage = relicFlyRoot.GetComponentInChildren<Image>(true);

        if (relicSettingButtonTarget == null)
        {
            Transform settingTarget = FindChildRecursive(null, "RelicSettingButton");
            if (settingTarget != null)
                relicSettingButtonTarget = settingTarget as RectTransform;
        }

        if (backgroundRoot == null)
        {
            Transform backgroundTransform = FindChildRecursive(transform, "background");

            if (backgroundTransform != null)
                backgroundRoot = backgroundTransform;
        }

        EnsureNextButtonRoot();
    }

    private void EnsureNextButtonRoot()
    {
        if (nextButtonRoot == null)
        {
            Transform nextButtonTransform = FindChildRecursive(transform, "NextButton");

            if (nextButtonTransform != null)
                nextButtonRoot = nextButtonTransform.gameObject;
        }

        if (nextButtonRoot == null)
            return;

        if (nextButton == null || nextButton.gameObject != nextButtonRoot)
            nextButton = nextButtonRoot.GetComponent<Button>();
    }

    private void BindChestEvents()
    {
        if (chestOpenButton == null)
            return;

        chestOpenButton.Opened -= NotifyChestOpened;
        chestOpenButton.RewardPointerEntered -= ShowRelicHoverInfo;
        chestOpenButton.RewardPointerExited -= HideRelicHoverInfo;
        chestOpenButton.RewardClaimed -= OnRelicRewardClaimed;

        chestOpenButton.Opened += NotifyChestOpened;
        chestOpenButton.RewardPointerEntered += ShowRelicHoverInfo;
        chestOpenButton.RewardPointerExited += HideRelicHoverInfo;
        chestOpenButton.RewardClaimed += OnRelicRewardClaimed;
    }

    private void UnbindChestEvents()
    {
        if (chestOpenButton == null)
            return;

        chestOpenButton.Opened -= NotifyChestOpened;
        chestOpenButton.RewardPointerEntered -= ShowRelicHoverInfo;
        chestOpenButton.RewardPointerExited -= HideRelicHoverInfo;
        chestOpenButton.RewardClaimed -= OnRelicRewardClaimed;
    }

    private void BindNextButton()
    {
        EnsureNextButtonRoot();

        if (nextButton == null)
            return;

        nextButton.onClick.RemoveListener(OnNextButtonClicked);
        nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void SetNextButtonVisible(bool visible)
    {
        EnsureNextButtonRoot();

        if (nextButtonRoot != null)
            nextButtonRoot.SetActive(visible);
    }

    private void HideRelicFlyObjects()
    {
        ResetRelicFlyRootTransform();

        if (relicFlyRoot != null)
            relicFlyRoot.gameObject.SetActive(false);

        if (relicFlyHighlight != null)
            relicFlyHighlight.SetActive(false);

        if (relicSettingGuideText != null)
            relicSettingGuideText.gameObject.SetActive(false);
    }


    private void CacheRelicFlyRootOriginalState()
    {
        if (relicFlyRoot == null || hasRelicFlyRootOriginalState)
            return;

        relicFlyRootOriginalAnchoredPosition = relicFlyRoot.anchoredPosition;
        relicFlyRootOriginalLocalScale = relicFlyRoot.localScale;
        hasRelicFlyRootOriginalState = true;
    }

    private void ResetRelicFlyRootTransform()
    {
        if (relicFlyRoot == null)
            return;

        CacheRelicFlyRootOriginalState();

        if (!hasRelicFlyRootOriginalState)
            return;

        relicFlyRoot.anchoredPosition = relicFlyRootOriginalAnchoredPosition;
        relicFlyRoot.localScale = relicFlyRootOriginalLocalScale;
    }

    private Sprite GetRelicSprite(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return null;

        if (DataManager.Instance == null || DataManager.Instance.RelicIconDatabase == null)
            return null;

        if (!DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon))
            return null;

        return icon;
    }

    private void PlayAcquireSfx()
    {
        if (!playAcquireSfx || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(acquireSfxType);
    }

    private IEnumerator ScaleRelicRoutine(float from, float to, float duration)
    {
        if (relicFlyRoot == null)
            yield break;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float scale = Mathf.Lerp(from, to, EaseOutCubic(t));
            relicFlyRoot.localScale = Vector3.one * scale;
            yield return null;
        }

        relicFlyRoot.localScale = Vector3.one * to;
    }

    private IEnumerator FlyRelicToSettingButtonRoutine()
    {
        if (relicFlyRoot == null || relicSettingButtonTarget == null)
            yield break;

        Vector2 start = relicFlyRoot.anchoredPosition;
        Vector2 end = GetTargetLocalPosition(relicFlyRoot, relicSettingButtonTarget);
        Vector2 control = (start + end) * 0.5f + Vector2.up * relicCurveHeight;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, relicFlyDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float eased = EaseInOutCubic(t);

            Vector2 p1 = Vector2.Lerp(start, control, eased);
            Vector2 p2 = Vector2.Lerp(control, end, eased);

            relicFlyRoot.anchoredPosition = Vector2.Lerp(p1, p2, eased);
            relicFlyRoot.localScale = Vector3.one * Mathf.Lerp(relicBigScale, relicEndScale, eased);

            yield return null;
        }

        relicFlyRoot.anchoredPosition = end;
        relicFlyRoot.localScale = Vector3.one * relicEndScale;
    }

    private Vector2 GetTargetLocalPosition(RectTransform movingRect, RectTransform targetRect)
    {
        RectTransform parentRect = movingRect.parent as RectTransform;

        if (parentRect == null || targetRect == null)
            return movingRect.anchoredPosition;

        Canvas canvas = movingRect.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPoint,
            uiCamera,
            out Vector2 localPoint))
        {
            return localPoint;
        }

        return movingRect.anchoredPosition;
    }

    private void ApplyBackgroundSorting()
    {
        if (backgroundRoot == null)
            return;

        Renderer[] renderers = backgroundRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].sortingOrder = backgroundSortingOrder;
        }
    }

    private Transform FindChildRecursive(Transform root, string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        if (root == null)
        {
            Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                if (allTransforms[i] != null && string.Equals(allTransforms[i].name, targetName, System.StringComparison.Ordinal))
                    return allTransforms[i];
            }

            return null;
        }

        if (string.Equals(root.name, targetName, System.StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), targetName);

            if (result != null)
                return result;
        }

        return null;
    }

    private void CompleteCurrentNode()
    {
        if (DataManager.Instance == null)
            return;

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();

        if (runtime == null)
            return;

        string nodeKey = runtime.CurrentNodeIndex.ToString();

        if (!runtime.ClearedMapIds.Contains(nodeKey))
            runtime.ClearedMapIds.Add(nodeKey);

        if (!runtime.VisitedMapIds.Contains(nodeKey))
            runtime.VisitedMapIds.Add(nodeKey);

        DataManager.Instance.MapRuntimeStore.Set(runtime);

        Debug.Log(
            $"[EventRoomController] Complete Node / Node:{runtime.CurrentNodeIndex} / Map:{runtime.CurrentMapId}"
        );
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }
}
