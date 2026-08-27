using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryPanelCloseButton : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private RectTransform inventoryPanelRect;
    [SerializeField] private string inventoryPanelObjectName = "InventoryPanel";
    [SerializeField] private bool autoFindInventoryPanel = true;

    [Header("Move")]
    [SerializeField] private float closedY = 1080f;
    [SerializeField] private float closeDuration = 0.2f;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;

    private Coroutine closeCoroutine;
    private int lastClickSoundFrame = -1;

    private void Awake()
    {
        FindInventoryPanelIfNeeded();
    }

    private void OnEnable()
    {
        FindInventoryPanelIfNeeded();
    }

    public void CloseInventoryPanel()
    {
        PlayClickSound();
        FindInventoryPanelIfNeeded();

        if (inventoryPanelRect == null)
        {
            Debug.LogWarning("[InventoryPanelCloseButton] InventoryPanel RectTransform is missing.");
            return;
        }

        InventoryPanelSelectionResetter.ResetAllSelectionsExcept(null);
        ClearSelectedObjectIfChildOf(inventoryPanelRect.gameObject);

        if (closeCoroutine != null)
            StopCoroutine(closeCoroutine);

        closeCoroutine = StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        Vector2 startPosition = inventoryPanelRect.anchoredPosition;
        Vector2 targetPosition = new Vector2(0f, closedY);

        float time = 0f;
        float duration = Mathf.Max(0.01f, closeDuration);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            inventoryPanelRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        inventoryPanelRect.anchoredPosition = targetPosition;
        closeCoroutine = null;
    }

    private void FindInventoryPanelIfNeeded()
    {
        if (inventoryPanel != null && inventoryPanelRect == null)
            inventoryPanelRect = inventoryPanel.GetComponent<RectTransform>();

        if (!autoFindInventoryPanel || inventoryPanelRect != null)
            return;

        GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate == null)
                continue;

            if (candidate.name != inventoryPanelObjectName)
                continue;

            inventoryPanel = candidate;
            inventoryPanelRect = candidate.GetComponent<RectTransform>();
            return;
        }
    }

    private void ClearSelectedObjectIfChildOf(GameObject root)
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null || eventSystem.currentSelectedGameObject == null || root == null)
            return;

        if (eventSystem.currentSelectedGameObject.transform.IsChildOf(root.transform))
            eventSystem.SetSelectedGameObject(null);
    }

    private void PlayClickSound()
    {
        if (!playClickSound)
            return;

        if (Time.frameCount == lastClickSoundFrame)
            return;

        if (AudioManager.Instance == null)
            return;

        lastClickSoundFrame = Time.frameCount;
        AudioManager.Instance.PlaySfx(clickSfx);
    }
}
