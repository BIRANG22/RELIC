using Relic.Gameplay.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterHUDSlot : MonoBehaviour
{
    [Header("Basic")]
    [SerializeField] private TMP_Text nameText;

    [Header("HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text hpValueText;

    [Header("Shield")]
    [SerializeField] private Image shieldFill;
    [SerializeField] private TMP_Text shieldValueText;

    [Header("Status Effects")]
    [SerializeField] private Transform statusIconRoot;
    [SerializeField] private StatusEffectIcon statusIconPrefab;
    [SerializeField] private float statusEffectIconSpacing = 4f;

    [Header("Follow")]
    [SerializeField] private bool useFollowPosition = true;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private float colliderTopScreenPadding = 4f;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rectTransform;
    private CharacterRuntimeData boundRuntime;
    private CharacterMasterData boundMaster;
    private Collider2D followCollider2D;
    private readonly List<StatusEffectIcon> spawnedStatusIcons = new();

    public CharacterRuntimeData BoundRuntime => boundRuntime;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        ApplyStatusEffectParentLayout();
    }

    private void LateUpdate()
    {
        if (useFollowPosition)
            UpdateFollowPosition();

        if (boundRuntime != null)
            Refresh();
    }

    public void SetUseFollowPosition(bool value)
    {
        useFollowPosition = value;
    }

    public void SetFollowTarget(Transform target)
    {
        SetFollowTarget(target, null);
    }

    public void SetFollowTarget(Transform target, Collider2D collider2D)
    {
        followTarget = target;
        followCollider2D = collider2D;

        if (useFollowPosition)
            UpdateFollowPosition();
    }

    private void UpdateFollowPosition()
    {
        if (!useFollowPosition || followTarget == null || rectTransform == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 followWorldPosition = GetFollowWorldPosition();
        Vector3 screenPos = cam.WorldToScreenPoint(followWorldPosition);

        if (followCollider2D != null)
        {
            float pivotOffset = rectTransform.rect.height * rectTransform.lossyScale.y * rectTransform.pivot.y;
            screenPos.y += pivotOffset + colliderTopScreenPadding;
        }

        rectTransform.position = screenPos;
    }

    private Vector3 GetFollowWorldPosition()
    {
        if (followCollider2D != null)
        {
            Bounds bounds = followCollider2D.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, followTarget.position.z);
        }

        return followTarget.position + worldOffset;
    }

    public void Bind(CharacterRuntimeData runtimeData)
    {
        boundRuntime = runtimeData;
        boundMaster = null;
        ApplyStatusEffectParentLayout();

        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        if (DataManager.Instance != null && DataManager.Instance.CharacterDatabase != null)
        {
            DataManager.Instance.CharacterDatabase.TryGet(
                boundRuntime.CharacterId,
                out boundMaster
            );
        }

        Refresh();
    }

    public void Show()
    {
        if (useFollowPosition)
            UpdateFollowPosition();

        Refresh();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Refresh()
    {
        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        if (nameText != null)
            nameText.text = GetDisplayName();

        int maxHP = boundRuntime.MaxHP > 0
            ? boundRuntime.MaxHP
            : boundMaster != null
                ? boundMaster.MaxHP
                : Mathf.Max(1, boundRuntime.CurrentHP);

        RefreshBar(hpFill, hpValueText, boundRuntime.CurrentHP, maxHP);
        RefreshShield(boundRuntime.CurrentShield, maxHP);
        RefreshStatusEffects(boundRuntime.StatusEffects);
    }

    public void AlignLeft()
    {
        AlignSelf(0f);
    }

    public void AlignRight()
    {
        AlignSelf(1f);
    }

    private void AlignSelf(float x)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.anchorMin = new Vector2(x, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2(x, rectTransform.anchorMax.y);
        rectTransform.pivot = new Vector2(x, rectTransform.pivot.y);
        rectTransform.anchoredPosition = new Vector2(0f, rectTransform.anchoredPosition.y);
    }

    private string GetDisplayName()
    {
        if (boundMaster != null && !string.IsNullOrWhiteSpace(boundMaster.Name))
            return boundMaster.Name;

        return boundRuntime != null ? boundRuntime.CharacterId : string.Empty;
    }

    private void RefreshBar(Image fill, TMP_Text valueText, int current, int max)
    {
        max = Mathf.Max(1, max);
        current = Mathf.Clamp(current, 0, max);

        if (fill != null)
            fill.fillAmount = (float)current / max;

        if (valueText != null)
            valueText.text = current.ToString();
    }

    private void RefreshShield(int shield, int maxHP)
    {
        shield = Mathf.Max(0, shield);
        maxHP = Mathf.Max(1, maxHP);

        if (shieldFill != null)
        {
            shieldFill.gameObject.SetActive(shield > 0);
            shieldFill.fillAmount = (float)shield / maxHP;
        }

        if (shieldValueText != null)
        {
            shieldValueText.gameObject.SetActive(shield > 0);
            shieldValueText.text = "+" + shield;
        }
    }

    private void RefreshStatusEffects(List<StatusEffectRuntimeData> statusEffects)
    {
        ClearStatusEffectIcons();
        ApplyStatusEffectParentLayout();

        if (statusIconRoot == null || statusIconPrefab == null || statusEffects == null)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i] == null)
                continue;

            StatusEffectIcon icon = Instantiate(statusIconPrefab, statusIconRoot);
            icon.SetTooltipEnabled(false);
            icon.Set(statusEffects[i]);
            spawnedStatusIcons.Add(icon);
        }
    }

    private void ClearStatusEffectIcons()
    {
        for (int i = spawnedStatusIcons.Count - 1; i >= 0; i--)
        {
            if (spawnedStatusIcons[i] != null)
                Destroy(spawnedStatusIcons[i].gameObject);
        }

        spawnedStatusIcons.Clear();

        if (statusIconRoot == null)
            return;

        for (int i = statusIconRoot.childCount - 1; i >= 0; i--)
            Destroy(statusIconRoot.GetChild(i).gameObject);
    }

    private void ApplyStatusEffectParentLayout()
    {
        if (statusIconRoot == null)
            return;

        HorizontalLayoutGroup layout = statusIconRoot.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
            layout.spacing = statusEffectIconSpacing;
    }

    private void Clear()
    {
        if (nameText != null)
            nameText.text = string.Empty;

        RefreshBar(hpFill, hpValueText, 0, 1);
        RefreshShield(0, 1);
        ClearStatusEffectIcons();
    }
}
