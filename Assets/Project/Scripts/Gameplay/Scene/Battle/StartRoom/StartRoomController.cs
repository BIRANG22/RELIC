using UnityEngine;
using Relic.Gameplay.Data;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class StartRoomController : MonoBehaviour
{
    [Header("Ally Spawn")]
    [SerializeField] private Transform[] allySpawnPoints;

    [Header("UI")]
    [SerializeField] private StartRoomChatWindow chatWindow;
    [SerializeField] private RelicChoiceAreaUI relicChoiceArea;

    [Header("Dialog")]
    [TextArea]
    [SerializeField] private string[] npcDialogLines;
    [SerializeField] private bool openDialogOnEnter = true;
    [SerializeField] private float openDialogDelay = 0.05f;
    [SerializeField] private bool waitForIntroTextToFinish = true;
    [SerializeField] private float maxWaitForIntroTextStart = 2f;

    [Header("Room")]
    [SerializeField] private GameObject startRoomRoot;
    [SerializeField] private GameObject mapPanel;

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

    private bool isDialogPlaying;
    private bool isRelicChoiceOpened;
    private bool isRelicSelected;
    private Coroutine openDialogOnEnterRoutine;

    private void Awake()
    {
        if (chatWindow == null)
            chatWindow = GetComponentInChildren<StartRoomChatWindow>(true);

        if (relicChoiceArea == null)
            relicChoiceArea = GetComponentInChildren<RelicChoiceAreaUI>(true);
    }

    private void OnEnable()
    {
        isDialogPlaying = false;
        isRelicChoiceOpened = false;

        SpawnPartyAllies();

        if (chatWindow != null)
            chatWindow.Close();

        if (relicChoiceArea != null)
            relicChoiceArea.Close();

        if (relicFlyRoot != null)
            relicFlyRoot.gameObject.SetActive(false);

        if (relicFlyHighlight != null)
            relicFlyHighlight.SetActive(false);

        if (relicSettingGuideText != null)
            relicSettingGuideText.gameObject.SetActive(false);

        if (openDialogOnEnter && !isRelicSelected)
        {
            if (openDialogOnEnterRoutine != null)
                StopCoroutine(openDialogOnEnterRoutine);

            openDialogOnEnterRoutine = StartCoroutine(OpenDialogOnEnterRoutine());
        }
    }

    private void OnDisable()
    {
        if (openDialogOnEnterRoutine != null)
        {
            StopCoroutine(openDialogOnEnterRoutine);
            openDialogOnEnterRoutine = null;
        }
    }

    private IEnumerator OpenDialogOnEnterRoutine()
    {
        yield return null;

        if (openDialogDelay > 0f)
            yield return new WaitForSecondsRealtime(openDialogDelay);

        if (waitForIntroTextToFinish)
            yield return WaitForBattleMapIntroTextFinished();

        openDialogOnEnterRoutine = null;

        if (!isActiveAndEnabled)
            yield break;

        if (isDialogPlaying || isRelicChoiceOpened || isRelicSelected)
            yield break;

        OnNpcClicked();
    }

    private IEnumerator WaitForBattleMapIntroTextFinished()
    {
        int startPlayCounter = BattleMapIntroText.CurrentPlayCounter;
        float elapsed = 0f;

        while (isActiveAndEnabled &&
               elapsed < Mathf.Max(0f, maxWaitForIntroTextStart) &&
               BattleMapIntroText.CurrentPlayCounter == startPlayCounter &&
               !BattleMapIntroText.IsAnyPlayingOrVisible())
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        while (isActiveAndEnabled && BattleMapIntroText.IsAnyPlayingOrVisible())
            yield return null;
    }

    public void CompleteStartRoom()
    {
        if (startRoomRoot != null)
            startRoomRoot.SetActive(false);

        if (mapPanel != null)
            mapPanel.SetActive(true);
    }

    private void SpawnPartyAllies()
    {
        if (DataManager.Instance == null)
            return;

        if (allySpawnPoints == null || allySpawnPoints.Length == 0)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterPrefabDatabase prefabDatabase = DataManager.Instance.CharacterPrefabDatabase;

        if (partyStore == null || prefabDatabase == null)
            return;

        for (int i = 0; i < allySpawnPoints.Length; i++)
        {
            Transform point = allySpawnPoints[i];

            if (point == null)
                continue;

            ClearPoint(point);

            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!prefabDatabase.TryGetBattleEventWorldPrefab(characterId, out GameObject battleEventPrefab))
            {
                Debug.LogWarning($"[StartRoomController] Battle event world prefab not found: {characterId}");
                continue;
            }

            GameObject ally = Instantiate(battleEventPrefab, point);
            ally.transform.localPosition = Vector3.zero;
            ally.transform.localRotation = Quaternion.identity;
            ally.transform.localScale = Vector3.one;

            if (ally.GetComponent<BattleMapSelectionCharacterMarker>() == null)
                ally.AddComponent<BattleMapSelectionCharacterMarker>();
        }
    }

    private void ClearPoint(Transform point)
    {
        for (int i = point.childCount - 1; i >= 0; i--)
            Destroy(point.GetChild(i).gameObject);
    }

    public void OnNpcClicked()
    {
        if (isDialogPlaying || isRelicChoiceOpened || isRelicSelected)
            return;

        isDialogPlaying = true;

        if (chatWindow != null)
            chatWindow.Open(npcDialogLines, OnDialogFinished);
        else
            OnDialogFinished();
    }

    private void OnDialogFinished()
    {
        if (isRelicSelected)
            return;

        isDialogPlaying = false;
        isRelicChoiceOpened = true;

        if (chatWindow != null)
            chatWindow.Close();

        if (relicChoiceArea != null)
            relicChoiceArea.Open();
        else
            Debug.LogWarning("[StartRoomController] RelicChoiceAreaUI is not connected.");
    }

    public void OnRelicChoiceFinished(string relicId)
    {
        if (isRelicSelected)
            return;

        isDialogPlaying = false;
        isRelicChoiceOpened = false;
        isRelicSelected = true;

        StartCoroutine(PlayRelicAcquireRoutine(relicId));
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
            $"[StartRoomController] Complete Node / Node:{runtime.CurrentNodeIndex} / Map:{runtime.CurrentMapId}"
        );
    }

    private IEnumerator PlayRelicAcquireRoutine(string relicId)
    {
        if (relicChoiceArea != null)
            relicChoiceArea.Close();

        Sprite relicSprite = GetRelicSprite(relicId);

        if (relicFlyIconImage != null)
        {
            relicFlyIconImage.sprite = relicSprite;
            relicFlyIconImage.enabled = relicSprite != null;
        }

        if (relicFlyRoot != null)
        {
            relicFlyRoot.gameObject.SetActive(true);
            relicFlyRoot.localScale = Vector3.one * relicStartScale;
        }

        if (relicFlyHighlight != null)
            relicFlyHighlight.SetActive(true);

        yield return ScaleRelicRoutine(relicStartScale, relicBigScale, relicScaleUpDuration);
        yield return new WaitForSeconds(relicHoldDuration);

        if (relicFlyHighlight != null)
            relicFlyHighlight.SetActive(false);

        yield return FlyRelicToSettingButtonRoutine();

        if (relicFlyRoot != null)
            relicFlyRoot.gameObject.SetActive(false);

        if (relicSettingGuideText != null)
            relicSettingGuideText.gameObject.SetActive(true);

        CompleteCurrentNode();

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ReturnToMap();
        else
            Debug.LogWarning("[StartRoomController] BattleSceneController ¾øÀ½");
    }

    public void HideRelicSettingGuideText()
    {
        if (relicSettingGuideText != null)
            relicSettingGuideText.gameObject.SetActive(false);
    }

    private Sprite GetRelicSprite(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.RelicIconDatabase == null)
            return null;

        if (!DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon))
            return null;

        return icon;
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
