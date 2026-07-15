using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillIconButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text lockText;
    [SerializeField] private GameObject lockObject;

    [Header("Hover Scale Effect")]
    [SerializeField] private Transform scaleTarget;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float breathMaxScale = 1.16f;
    [SerializeField] private float scaleInDuration = 0.08f;
    [SerializeField] private float breathSpeed = 4f;
    [SerializeField] private bool useUnscaledTime = true;

    private SkillSettingPanel owner;
    private SkillMasterData currentSkillData;

    private bool isLocked;
    private int requiredLevel;

    private Vector3 originalScale = Vector3.one;
    private bool isScaleCached;
    private Coroutine hoverScaleCoroutine;
    private int shownInfoVersion = -1;
    private bool isPointerInside;

    public SkillMasterData CurrentSkillData => currentSkillData;

    private void Awake()
    {
        CacheOriginalScale();
    }

    private void OnEnable()
    {
        CacheOriginalScale();
    }

    private void OnDisable()
    {
        if (isPointerInside)
        {
            LobbyInfoHoverState.EndSkillHover();
            isPointerInside = false;
        }

        StopHoverScaleEffect(true);
    }

    public void Init(SkillSettingPanel panel)
    {
        owner = panel;

        if (button != null)
        {
            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }
    }

    public void SetSkillData(
        SkillMasterData skillData,
        bool locked,
        int requiredLv
    )
    {
        StopHoverScaleEffect(true);

        currentSkillData = skillData;
        isLocked = locked;
        requiredLevel = requiredLv;

        bool hasSkill = currentSkillData != null;

        gameObject.SetActive(hasSkill);

        if (!hasSkill)
            return;

        if (nameText != null)
            nameText.text = currentSkillData.Name;

        if (iconImage != null)
        {
            Sprite icon = SkillIconUtility.GetSkillIcon(currentSkillData.SkillId);

            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
            iconImage.color = SkillRarityUtility.GetSkillIconColor(currentSkillData.SkillId);
        }

        if (lockObject != null)
            lockObject.SetActive(isLocked);

        if (lockText != null)
            lockText.text = isLocked
                ? $"LV.{requiredLevel}"
                : "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isPointerInside)
        {
            LobbyInfoHoverState.BeginSkillHover();
            isPointerInside = true;
        }

        // 룬 버튼 위에 마우스가 있으면 룬 정보가 항상 우선입니다.
        if (!LobbyInfoHoverState.IsRuneHovered &&
            owner != null && currentSkillData != null && owner.CanPreviewSkillIconHover)
        {
            owner.ShowSkillInfo(currentSkillData);
            shownInfoVersion = LobbyInfoHoverState.CurrentVersion;
        }

        StartHoverScaleEffect();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPointerInside)
        {
            LobbyInfoHoverState.EndSkillHover();
            isPointerInside = false;
        }

        StopHoverScaleEffect(true);

        // 프리뷰에서는 호버가 끝나면 기본 안내 정보로 돌아갑니다.
        // 스킬 세팅에서는 마지막으로 확인한 정보를 유지합니다.
        if (owner != null && owner.ShouldClearInfoOnHoverExit && shownInfoVersion >= 0)
            owner.ClearSkillInfoFromHover(shownInfoVersion);

        shownInfoVersion = -1;
    }

    public void Execute()
    {
        if (owner == null)
            return;

        if (currentSkillData == null)
            return;

        owner.ShowSkillInfo(currentSkillData);

        if (isLocked)
        {
            Debug.Log(
                $"[SkillIconButton] Locked. Required Level: {requiredLevel}"
            );
            return;
        }

        owner.SelectSkill(currentSkillData);
    }

    private void CacheOriginalScale()
    {
        if (scaleTarget == null)
            scaleTarget = transform;

        if (scaleTarget == null)
            return;

        if (isScaleCached)
            return;

        originalScale = scaleTarget.localScale;
        isScaleCached = true;
    }

    private void StartHoverScaleEffect()
    {
        if (!isActiveAndEnabled)
            return;

        if (currentSkillData == null)
            return;

        CacheOriginalScale();

        if (scaleTarget == null)
            return;

        StopHoverScaleEffect(false);
        hoverScaleCoroutine = StartCoroutine(HoverScaleRoutine());
    }

    private void StopHoverScaleEffect(bool resetScale)
    {
        if (hoverScaleCoroutine != null)
        {
            StopCoroutine(hoverScaleCoroutine);
            hoverScaleCoroutine = null;
        }

        if (resetScale && scaleTarget != null && isScaleCached)
            scaleTarget.localScale = originalScale;
    }

    private IEnumerator HoverScaleRoutine()
    {
        float safeScaleInDuration = Mathf.Max(0.01f, scaleInDuration);
        float elapsed = 0f;

        Vector3 startScale = scaleTarget.localScale;
        Vector3 firstTargetScale = originalScale * hoverScale;

        while (elapsed < safeScaleInDuration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / safeScaleInDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            scaleTarget.localScale = Vector3.LerpUnclamped(startScale, firstTargetScale, t);
            yield return null;
        }

        float time = 0f;
        float minScale = hoverScale;
        float maxScale = Mathf.Max(hoverScale, breathMaxScale);

        while (true)
        {
            time += GetDeltaTime() * breathSpeed;

            float pingPong = (Mathf.Sin(time) + 1f) * 0.5f;
            float currentScale = Mathf.Lerp(minScale, maxScale, pingPong);

            scaleTarget.localScale = originalScale * currentScale;
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
