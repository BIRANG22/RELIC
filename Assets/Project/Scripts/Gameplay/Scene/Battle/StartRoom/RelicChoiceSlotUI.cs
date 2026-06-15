using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RelicChoiceSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image relicIconImage;
    [SerializeField] private Button button;

    [Header("Hover Scale Effect")]
    [SerializeField] private Transform scaleTarget;
    [SerializeField, Min(1f)] private float hoverBaseScale = 1.08f;
    [SerializeField, Min(0f)] private float breathAmount = 0.04f;
    [SerializeField, Min(0.1f)] private float breathSpeed = 4f;
    [SerializeField, Min(0.1f)] private float scaleLerpSpeed = 14f;

    private string relicId;
    private RelicChoiceAreaUI owner;
    private bool isSetup;
    private bool isPointerInside;
    private bool isClicked;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }

        if (scaleTarget == null)
            scaleTarget = transform;

        originalScale = scaleTarget.localScale;
    }

    private void OnEnable()
    {
        if (scaleTarget == null)
            scaleTarget = transform;

        originalScale = scaleTarget.localScale;

        isPointerInside = false;
        isClicked = false;
        ResetScaleImmediate();
    }

    private void Update()
    {
        if (scaleTarget == null)
            return;

        Vector3 targetScale = originalScale;

        if (isSetup && isPointerInside && !isClicked)
        {
            float breath = Mathf.Sin(Time.unscaledTime * breathSpeed) * breathAmount;
            float scale = hoverBaseScale + breath;
            targetScale = originalScale * scale;
        }

        scaleTarget.localScale = Vector3.Lerp(
            scaleTarget.localScale,
            targetScale,
            Time.unscaledDeltaTime * scaleLerpSpeed
        );
    }

    private void OnDisable()
    {
        isPointerInside = false;
        isClicked = false;
        ResetScaleImmediate();
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
        isSetup = false;
        isPointerInside = false;
        isClicked = false;
        ResetScaleImmediate();

        if (string.IsNullOrWhiteSpace(relicId))
        {
            ClearSlot();
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
        {
            Debug.LogWarning("[RelicChoiceSlotUI] DataManager or RelicDatabase is null.");
            ClearSlot();
            return;
        }

        RelicData relicData = DataManager.Instance.RelicDatabase.Get(relicId);

        if (relicData == null)
        {
            Debug.LogWarning($"[RelicChoiceSlotUI] Unknown relic id: {relicId}");
            ClearSlot();
            return;
        }

        SetupIcon();

        isSetup = true;

        if (button != null)
            button.interactable = true;

        gameObject.SetActive(true);
    }

    private void SetupIcon()
    {
        if (relicIconImage == null)
            return;

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

    public void ClearSlot()
    {
        relicId = string.Empty;
        owner = null;
        isSetup = false;
        isPointerInside = false;
        isClicked = false;
        ResetScaleImmediate();

        if (button != null)
            button.interactable = false;

        if (relicIconImage != null)
        {
            relicIconImage.sprite = null;
            relicIconImage.enabled = false;
            relicIconImage.raycastTarget = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSetup || owner == null || isClicked)
            return;

        isPointerInside = true;
        owner.ShowRelicHoverInfo(relicId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;

        if (owner != null)
            owner.HideRelicHoverInfo();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick();
    }

    public void OnClick()
    {
        if (!isSetup || isClicked || string.IsNullOrWhiteSpace(relicId))
            return;

        isClicked = true;
        isPointerInside = false;

        if (button != null)
            button.interactable = false;

        if (owner != null)
            owner.SelectRelic(relicId);
    }

    private void ResetScaleImmediate()
    {
        if (scaleTarget != null)
            scaleTarget.localScale = originalScale;
    }
}