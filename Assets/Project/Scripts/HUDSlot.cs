using Relic.Gameplay.Data;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDSlot : MonoBehaviour
{
    [Header("Basic")]
    [SerializeField] private TMP_Text nameText;

    [Header("HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text hpValueText;

    [Header("Shield")]
    [SerializeField] private Image shieldFill;
    [SerializeField] private TMP_Text shieldValueText;

    [Header("Value Animation")]
    [SerializeField, Min(0f)] private float hpValueChangeDuration = 0.35f;
    [SerializeField, Min(0f)] private float shieldValueChangeDuration = 0.35f;

    [Header("Status Effects")]
    [SerializeField] private Transform statusIconRoot;
    [SerializeField] private StatusEffectIcon statusIconPrefab;

    [Header("Follow")]
    [SerializeField] private bool useFollowPosition = true;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private float colliderTopScreenPadding = 4f;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rectTransform;
    private MonsterRuntimeData boundMonsterRuntime;
    private CharacterRuntimeData boundCharacterRuntime;
    private bool characterHovering;
    private bool characterTimelineHovering;
    private bool characterSelected;
    private float characterTemporaryVisibleUntil;
    private int lastCharacterHP;
    private int lastCharacterShield;
    private int lastCharacterStatusHash;
    private bool hasCharacterSnapshot;

    public MonsterRuntimeData BoundMonsterRuntime => boundMonsterRuntime;
    public CharacterRuntimeData BoundCharacterRuntime => boundCharacterRuntime;
    private Collider2D followCollider2D;
    private readonly List<StatusEffectIcon> spawnedStatusIcons = new();

    private Coroutine hpValueRoutine;
    private Coroutine shieldValueRoutine;
    private float displayedHP;
    private float displayedShield;
    private int displayedMaxHP = 1;
    private int targetHP;
    private int targetShield;
    private bool hasDisplayedValues;
    private bool isVisible;
    private Camera cachedMainCamera;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.ignoreParentGroups = true;
        ProtectDynamicNameText();
    }

    private void OnEnable()
    {
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady -= OnLocaleTableReady;
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady += OnLocaleTableReady;
    }

    private void OnDisable()
    {
        LocalizationRuntimeRefreshCoordinator.LocaleTableReady -= OnLocaleTableReady;
        StopValueAnimations();
        isVisible = false;
        hasDisplayedValues = false;
    }

    private void LateUpdate()
    {
        if (useFollowPosition)
            UpdateFollowPosition();

        MonitorCharacterRuntime();
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

        Camera cam = GetMainCamera();
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

    private Camera GetMainCamera()
    {
        if (cachedMainCamera == null)
            cachedMainCamera = Camera.main;

        return cachedMainCamera;
    }

    public void Bind(MonsterRuntimeData runtimeData)
    {
        bool runtimeChanged = !ReferenceEquals(boundMonsterRuntime, runtimeData);
        boundMonsterRuntime = runtimeData;
        boundCharacterRuntime = null;
        hasCharacterSnapshot = false;

        if (boundMonsterRuntime == null)
        {
            Clear();
            return;
        }

        if (runtimeChanged)
        {
            StopValueAnimations();
            hasDisplayedValues = false;
        }

        Refresh();
    }

    public void Show()
    {
        if (useFollowPosition)
            UpdateFollowPosition();

        isVisible = true;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Refresh();
        StartPendingValueAnimations();
    }

    public void Hide()
    {
        isVisible = false;
        StopValueAnimations();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Refresh()
    {
        // 몬스터와 캐릭터가 공용 HUDSlot을 사용하므로, 둘 다 바인딩되지 않은 경우에만 비웁니다.
        if (boundMonsterRuntime == null && boundCharacterRuntime == null)
        {
            Clear();
            return;
        }

        if (nameText != null)
            nameText.text = GetDisplayName();

        int maxHP = Mathf.Max(1, GetMaxHP());
        int newTargetHP = Mathf.Clamp(GetCurrentHP(), 0, maxHP);
        int newTargetShield = Mathf.Max(0, GetCurrentShield());

        if (!hasDisplayedValues)
        {
            InitializeDisplayedValues(newTargetHP, newTargetShield, maxHP);
        }
        else
        {
            displayedMaxHP = maxHP;

            if (targetHP != newTargetHP)
            {
                targetHP = newTargetHP;
                if (isVisible)
                    StartHPAnimation();
            }
            else
            {
                ApplyHPDisplay(displayedHP, displayedMaxHP);
            }

            if (targetShield != newTargetShield)
            {
                targetShield = newTargetShield;
                if (isVisible)
                    StartShieldAnimation();
            }
            else
            {
                ApplyShieldDisplay(displayedShield, displayedMaxHP);
            }
        }

        RefreshStatusEffects(GetStatusEffects());
    }

    public void Bind(CharacterRuntimeData runtimeData)
    {
        bool runtimeChanged = !ReferenceEquals(boundCharacterRuntime, runtimeData);
        boundCharacterRuntime = runtimeData;
        boundMonsterRuntime = null;
        characterTemporaryVisibleUntil = 0f;
        characterHovering = false;
        characterTimelineHovering = false;
        characterSelected = false;

        if (boundCharacterRuntime == null)
        {
            hasCharacterSnapshot = false;
            Clear();
            return;
        }

        if (runtimeChanged)
        {
            StopValueAnimations();
            hasDisplayedValues = false;
            hasCharacterSnapshot = false;
        }

        Refresh();
        CaptureCharacterSnapshot();
    }

    public void SetCharacterHover(bool active)
    {
        if (boundCharacterRuntime == null)
            return;

        characterHovering = active;
        RefreshCharacterVisibility();
    }

    public void SetCharacterTimelineHover(bool active)
    {
        if (boundCharacterRuntime == null)
            return;

        characterTimelineHovering = active;
        RefreshCharacterVisibility();
    }

    public void SetCharacterSelected(bool selected)
    {
        if (boundCharacterRuntime == null)
            return;

        characterSelected = selected;
        RefreshCharacterVisibility();
    }

    private void RefreshCharacterVisibility()
    {
        if (boundCharacterRuntime == null)
            return;

        bool temporaryVisible = characterTemporaryVisibleUntil > 0f && Time.unscaledTime < characterTemporaryVisibleUntil;
        bool shouldShow = characterSelected || characterHovering || characterTimelineHovering || temporaryVisible;

        if (shouldShow)
            Show();
        else
            Hide();
    }

    public void ShowCharacterTemporary(float duration = 1.5f)
    {
        if (boundCharacterRuntime == null)
            return;

        characterTemporaryVisibleUntil = Mathf.Max(characterTemporaryVisibleUntil, Time.unscaledTime + Mathf.Max(0f, duration));
        RefreshCharacterVisibility();
    }

    private void MonitorCharacterRuntime()
    {
        if (boundCharacterRuntime == null)
            return;

        int hp = boundCharacterRuntime.CurrentHP;
        int shield = boundCharacterRuntime.CurrentShield;
        int statusHash = CalculateStatusHash(boundCharacterRuntime.StatusEffects);

        if (!hasCharacterSnapshot)
        {
            CaptureCharacterSnapshot();
            return;
        }

        if (hp != lastCharacterHP || shield != lastCharacterShield || statusHash != lastCharacterStatusHash)
        {
            lastCharacterHP = hp;
            lastCharacterShield = shield;
            lastCharacterStatusHash = statusHash;
            Refresh();
            ShowCharacterTemporary();
        }

        if (characterTemporaryVisibleUntil > 0f && Time.unscaledTime >= characterTemporaryVisibleUntil)
        {
            characterTemporaryVisibleUntil = 0f;
            RefreshCharacterVisibility();
        }
    }

    private void CaptureCharacterSnapshot()
    {
        if (boundCharacterRuntime == null)
            return;

        lastCharacterHP = boundCharacterRuntime.CurrentHP;
        lastCharacterShield = boundCharacterRuntime.CurrentShield;
        lastCharacterStatusHash = CalculateStatusHash(boundCharacterRuntime.StatusEffects);
        hasCharacterSnapshot = true;
    }

    private static int CalculateStatusHash(List<StatusEffectRuntimeData> statusEffects)
    {
        unchecked
        {
            int hash = 17;
            if (statusEffects == null)
                return hash;

            hash = hash * 31 + statusEffects.Count;
            for (int i = 0; i < statusEffects.Count; i++)
            {
                StatusEffectRuntimeData effect = statusEffects[i];
                if (effect == null)
                    continue;

                hash = hash * 31 + (effect.EffectId != null ? effect.EffectId.GetHashCode() : 0);
                hash = hash * 31 + effect.Stack;
                hash = hash * 31 + effect.TurnCount;
            }

            return hash;
        }
    }

    private int GetMaxHP()
    {
        if (boundMonsterRuntime != null) return boundMonsterRuntime.MaxHP;
        if (boundCharacterRuntime != null) return boundCharacterRuntime.MaxHP;
        return 1;
    }

    private int GetCurrentHP()
    {
        if (boundMonsterRuntime != null) return boundMonsterRuntime.CurrentHP;
        if (boundCharacterRuntime != null) return boundCharacterRuntime.CurrentHP;
        return 0;
    }

    private int GetCurrentShield()
    {
        if (boundMonsterRuntime != null) return boundMonsterRuntime.CurrentShield;
        if (boundCharacterRuntime != null) return boundCharacterRuntime.CurrentShield;
        return 0;
    }

    private List<StatusEffectRuntimeData> GetStatusEffects()
    {
        if (boundMonsterRuntime != null) return boundMonsterRuntime.StatusEffects;
        if (boundCharacterRuntime != null) return boundCharacterRuntime.StatusEffects;
        return null;
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
        if (boundMonsterRuntime != null)
            return boundMonsterRuntime.GetDisplayName();

        if (boundCharacterRuntime != null)
        {
            if (DataManager.Instance != null && DataManager.Instance.CharacterDatabase != null)
            {
                CharacterMasterData data = DataManager.Instance.CharacterDatabase.Get(boundCharacterRuntime.CharacterId);
                if (data != null && !string.IsNullOrWhiteSpace(data.Name))
                    return GameDataLocalization.CharacterName(data);
            }

            return boundCharacterRuntime.CharacterId ?? string.Empty;
        }

        return string.Empty;
    }

    private void OnLocaleTableReady(UnityEngine.Localization.Locale _)
    {
        if (nameText != null && (boundMonsterRuntime != null || boundCharacterRuntime != null))
            nameText.text = GetDisplayName();
    }

    private void ProtectDynamicNameText()
    {
        if (nameText == null)
            return;

        if (nameText.GetComponent<LocalizationIgnore>() == null)
            nameText.gameObject.AddComponent<LocalizationIgnore>();
        if (nameText.GetComponent<LocalizationAutoBindingIgnore>() == null)
            nameText.gameObject.AddComponent<LocalizationAutoBindingIgnore>();

        LocalizedTMPText staticWriter = nameText.GetComponent<LocalizedTMPText>();
        if (staticWriter != null)
            staticWriter.enabled = false;

        DynamicLocalizedTMPText dynamicWriter = nameText.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicWriter != null)
            dynamicWriter.enabled = false;
    }

    private void InitializeDisplayedValues(int hp, int shield, int maxHP)
    {
        displayedMaxHP = Mathf.Max(1, maxHP);
        targetHP = hp;
        targetShield = shield;
        displayedHP = hp;
        displayedShield = shield;
        hasDisplayedValues = true;

        ApplyHPDisplay(displayedHP, displayedMaxHP);
        ApplyShieldDisplay(displayedShield, displayedMaxHP);
    }

    private void StartPendingValueAnimations()
    {
        if (!hasDisplayedValues)
            return;

        if (Mathf.RoundToInt(displayedHP) != targetHP)
            StartHPAnimation();

        if (Mathf.RoundToInt(displayedShield) != targetShield)
            StartShieldAnimation();
    }

    private void StartHPAnimation()
    {
        if (hpValueRoutine != null)
            StopCoroutine(hpValueRoutine);

        if (hpValueChangeDuration <= 0f || Mathf.Approximately(displayedHP, targetHP))
        {
            displayedHP = targetHP;
            ApplyHPDisplay(displayedHP, displayedMaxHP);
            hpValueRoutine = null;
            return;
        }

        hpValueRoutine = StartCoroutine(AnimateHPValue(displayedHP, targetHP));
    }

    private IEnumerator AnimateHPValue(float startValue, int endValue)
    {
        float duration = Mathf.Max(0.0001f, hpValueChangeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            displayedHP = Mathf.Lerp(startValue, endValue, t);
            ApplyHPDisplay(displayedHP, displayedMaxHP);
            yield return null;
        }

        displayedHP = endValue;
        ApplyHPDisplay(displayedHP, displayedMaxHP);
        hpValueRoutine = null;
    }

    private void StartShieldAnimation()
    {
        if (shieldValueRoutine != null)
            StopCoroutine(shieldValueRoutine);

        if (shieldValueChangeDuration <= 0f || Mathf.Approximately(displayedShield, targetShield))
        {
            displayedShield = targetShield;
            ApplyShieldDisplay(displayedShield, displayedMaxHP);
            shieldValueRoutine = null;
            return;
        }

        shieldValueRoutine = StartCoroutine(AnimateShieldValue(displayedShield, targetShield));
    }

    private IEnumerator AnimateShieldValue(float startValue, int endValue)
    {
        float duration = Mathf.Max(0.0001f, shieldValueChangeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            displayedShield = Mathf.Lerp(startValue, endValue, t);
            ApplyShieldDisplay(displayedShield, displayedMaxHP);
            yield return null;
        }

        displayedShield = endValue;
        ApplyShieldDisplay(displayedShield, displayedMaxHP);
        shieldValueRoutine = null;
    }

    private void StopValueAnimations()
    {
        if (hpValueRoutine != null)
        {
            StopCoroutine(hpValueRoutine);
            hpValueRoutine = null;
        }

        if (shieldValueRoutine != null)
        {
            StopCoroutine(shieldValueRoutine);
            shieldValueRoutine = null;
        }
    }

    private void ApplyHPDisplay(float value, int max)
    {
        max = Mathf.Max(1, max);
        int rounded = Mathf.Clamp(Mathf.RoundToInt(value), 0, max);

        if (hpFill != null)
            hpFill.fillAmount = Mathf.Clamp01(value / max);

        if (hpValueText != null)
            hpValueText.text = rounded.ToString();
    }

    private void ApplyShieldDisplay(float value, int maxHP)
    {
        maxHP = Mathf.Max(1, maxHP);
        int rounded = Mathf.Max(0, Mathf.RoundToInt(value));
        bool shouldShow = rounded > 0 || targetShield > 0 || shieldValueRoutine != null;

        if (shieldFill != null)
        {
            shieldFill.gameObject.SetActive(shouldShow);
            shieldFill.fillAmount = Mathf.Max(0f, value) / maxHP;
        }

        if (shieldValueText != null)
        {
            shieldValueText.gameObject.SetActive(shouldShow);
            shieldValueText.text = "+" + rounded;
        }
    }

    private void RefreshStatusEffects(List<StatusEffectRuntimeData> statusEffects)
    {
        ClearStatusEffectIcons();

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

    private void Clear()
    {
        StopValueAnimations();
        hasDisplayedValues = false;
        targetHP = 0;
        targetShield = 0;
        displayedHP = 0f;
        displayedShield = 0f;
        displayedMaxHP = 1;

        if (nameText != null)
            nameText.text = string.Empty;

        ApplyHPDisplay(0f, 1);
        ApplyShieldDisplay(0f, 1);
        ClearStatusEffectIcons();
    }
}
