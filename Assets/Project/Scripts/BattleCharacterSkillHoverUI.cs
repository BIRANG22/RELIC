using Relic.Gameplay.Data;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// BattleCharacterPanel의 스킬 버튼 호버/선택 시각 효과를 적용합니다.
/// 호버 시에는 Skill_Background 색상만 변경하고, Skill_Background2는 그리드 선택 중인 스킬에만 표시합니다.
/// </summary>
public class BattleCharacterSkillHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Target")]
    [SerializeField] private Image normalBackgroundImage;
    [SerializeField] private Image hoverBackgroundImage;
    [SerializeField] private RectTransform scaleTarget;

    [Header("Hover Color")]
    [SerializeField] private Color hoverNormalBackgroundColor = new Color32(0x4E, 0x66, 0xDF, 0xFF);

    [Header("Hover Scale")]
    [SerializeField, Min(1f)] private float hoverScale = 1.05f;
    [SerializeField, Min(0f)] private float scaleLerpSpeed = 14f;

    [Header("Hover Alpha Breath")]
    [SerializeField, Range(0, 255)] private byte minimumAlpha = 150;
    [SerializeField, Range(0, 255)] private byte maximumAlpha = 255;
    [SerializeField, Min(0f)] private float alphaBreathSpeed = 3f;

    [Header("Direction Click Feedback")]
    [SerializeField, Min(0f)] private float directionClickFeedbackDuration = 0.15f;

    [Header("Auto Find")]
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private string hoverBackgroundObjectName = "Skill_Background2";

    private Vector3 normalScale = Vector3.one;
    private bool scaleCaptured;
    private bool isPointerOver;
    private bool isSelected;
    private float directionClickFeedbackUntil = -1f;
    private PlayerSkillReservationController reservationController;
    private Color normalBackgroundOriginalColor = Color.white;
    private bool normalBackgroundColorCaptured;
    private BattleTimelineController battleTimelineController;
    private SkillMasterData skillData;
    private CharacterRuntimeData previewRuntime;
    private Action<SkillMasterData> skillInfoHandler;

    private void Awake()
    {
        ResolveReferences();
        CaptureNormalScale();
        ResetVisual(true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureNormalScale();
        ResetVisual(true);
    }

    private void Update()
    {
        RefreshSelectedState();
        ApplyHighlightVisual();
        ApplyScale(false);

        if (isSelected)
            ApplyBreathingAlpha();
    }

    private void OnDisable()
    {
        directionClickFeedbackUntil = -1f;
        ClearSkillRangePreview();
        ResetVisual(true);
    }

    public void Configure(
        Image normalBackground,
        Image hoverBackground,
        RectTransform target,
        SkillMasterData previewSkillData = null,
        CharacterRuntimeData runtimeData = null,
        Action<SkillMasterData> onSkillHovered = null)
    {
        if (normalBackground != null)
            normalBackgroundImage = normalBackground;

        if (hoverBackground != null)
            hoverBackgroundImage = hoverBackground;

        if (target != null)
            scaleTarget = target;

        skillData = previewSkillData;
        previewRuntime = runtimeData;
        skillInfoHandler = onSkillHovered;

        scaleCaptured = false;
        normalBackgroundColorCaptured = false;
        ResolveReferences();
        CaptureNormalScale();
        ResetVisual(true);
    }


    public void SetSkillRangePreview(SkillMasterData previewSkillData)
    {
        skillData = previewSkillData;
        CaptureCurrentNormalBackgroundColor();

        if (skillData == null && isPointerOver)
        {
            ClearSkillRangePreview();
            ResetVisual(false);
        }
    }

    public void SetPreviewCharacter(CharacterRuntimeData runtimeData)
    {
        previewRuntime = runtimeData;
    }



    public void ShowClickSelectionFeedback()
    {
        if (skillData != null && skillData.RangeType == RangeType.Direction)
        {
            directionClickFeedbackUntil = Time.unscaledTime + directionClickFeedbackDuration;
            ApplyHighlightVisual();
            return;
        }

        isSelected = true;
        ApplyHighlightVisual();
    }

    public void SetSkillInfoHandler(Action<SkillMasterData> onSkillHovered)
    {
        skillInfoHandler = onSkillHovered;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        isPointerOver = true;
        EnsureNormalBackgroundVisible();
        ApplyNormalBackgroundHoverColor();

        ApplyHighlightVisual();
        ApplyScale(false);
        ShowSkillRangePreview();
        skillInfoHandler?.Invoke(skillData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ClearSkillRangePreview();
        RestoreNormalBackgroundColor();
        ApplyHighlightVisual();
        ApplyScale(false);
    }

    private void ResetVisual(bool instant)
    {
        isPointerOver = false;
        EnsureNormalBackgroundVisible();
        RestoreNormalBackgroundColor();

        if (hoverBackgroundImage != null)
            hoverBackgroundImage.gameObject.SetActive(false);

        ApplyScale(instant);
    }

    private void EnsureNormalBackgroundVisible()
    {
        if (normalBackgroundImage == null)
            return;

        normalBackgroundImage.gameObject.SetActive(true);
        normalBackgroundImage.enabled = true;

        Button button = GetComponent<Button>();
        if (button != null)
            button.transition = Selectable.Transition.None;
    }

    private void ApplyBreathingAlpha()
    {
        if (hoverBackgroundImage == null)
            return;

        float minAlpha = Mathf.Min(minimumAlpha, maximumAlpha);
        float maxAlpha = Mathf.Max(minimumAlpha, maximumAlpha);
        float wave = (Mathf.Sin(Time.unscaledTime * alphaBreathSpeed) + 1f) * 0.5f;
        byte alpha = (byte)Mathf.RoundToInt(Mathf.Lerp(minAlpha, maxAlpha, wave));

        SetHoverAlpha(alpha);
    }

    private void SetHoverAlpha(byte alpha)
    {
        if (hoverBackgroundImage == null)
            return;

        Color32 color = hoverBackgroundImage.color;
        color.a = alpha;
        hoverBackgroundImage.color = color;
    }

    private void ApplyScale(bool instant)
    {
        if (scaleTarget == null)
            return;

        CaptureNormalScale();

        Vector3 targetScale = normalScale * (isPointerOver ? hoverScale : 1f);

        if (instant || scaleLerpSpeed <= 0f)
        {
            scaleTarget.localScale = targetScale;
            return;
        }

        float t = 1f - Mathf.Exp(-scaleLerpSpeed * Time.unscaledDeltaTime);
        scaleTarget.localScale = Vector3.Lerp(scaleTarget.localScale, targetScale, t);
    }


    private void RefreshSelectedState()
    {
        EnsureReservationController();
        isSelected = reservationController != null &&
                     reservationController.IsGridSelectionActiveFor(previewRuntime, skillData);
    }

    private void ApplyHighlightVisual()
    {
        if (hoverBackgroundImage == null)
            return;

        bool showHighlight = skillData != null &&
                             (isSelected || IsDirectionClickFeedbackActive());
        hoverBackgroundImage.gameObject.SetActive(showHighlight);

        if (showHighlight)
            SetHoverAlpha(maximumAlpha);
    }

    private bool IsDirectionClickFeedbackActive()
    {
        return directionClickFeedbackUntil >= 0f &&
               Time.unscaledTime < directionClickFeedbackUntil;
    }

    private void EnsureReservationController()
    {
        if (reservationController != null)
            return;

        reservationController = FindFirstObjectByType<PlayerSkillReservationController>(
            FindObjectsInactive.Include
        );
    }

    private void ShowSkillRangePreview()
    {
        if (skillData == null)
            return;

        EnsureBattleTimelineController();
        battleTimelineController?.ShowSkillHoverRangePreview(previewRuntime, skillData);
    }

    private void ClearSkillRangePreview()
    {
        EnsureBattleTimelineController();
        battleTimelineController?.ClearSkillHoverRangePreview();
    }

    private void EnsureBattleTimelineController()
    {
        if (battleTimelineController != null)
            return;

        battleTimelineController = FindFirstObjectByType<BattleTimelineController>(
            FindObjectsInactive.Include
        );
    }

    private bool IsInteractable()
    {
        Button button = GetComponent<Button>();
        return button == null || button.interactable;
    }

    private void ResolveReferences()
    {
        if (scaleTarget == null)
            scaleTarget = GetComponent<RectTransform>();

        if (!autoFindReferences)
        {
            CaptureNormalBackgroundColor();
            return;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null)
                continue;

            if (normalBackgroundImage == null && child.name == "Skill_Background")
                normalBackgroundImage = child.GetComponent<Image>();

            if (hoverBackgroundImage == null && child.name == hoverBackgroundObjectName)
                hoverBackgroundImage = child.GetComponent<Image>();
        }

        CaptureNormalBackgroundColor();
    }

    private void CaptureNormalBackgroundColor()
    {
        if (normalBackgroundColorCaptured || normalBackgroundImage == null)
            return;

        normalBackgroundOriginalColor = normalBackgroundImage.color;
        normalBackgroundColorCaptured = true;
    }

    private void CaptureCurrentNormalBackgroundColor()
    {
        if (normalBackgroundImage == null)
            return;

        normalBackgroundOriginalColor = normalBackgroundImage.color;
        normalBackgroundColorCaptured = true;
    }

    private void ApplyNormalBackgroundHoverColor()
    {
        if (normalBackgroundImage == null)
            return;

        CaptureNormalBackgroundColor();

        Color color = hoverNormalBackgroundColor;
        color.a = normalBackgroundOriginalColor.a;
        normalBackgroundImage.color = color;
    }

    private void RestoreNormalBackgroundColor()
    {
        if (normalBackgroundImage == null)
            return;

        CaptureNormalBackgroundColor();
        normalBackgroundImage.color = normalBackgroundOriginalColor;
    }

    private void CaptureNormalScale()
    {
        if (scaleCaptured || scaleTarget == null)
            return;

        normalScale = scaleTarget.localScale;
        scaleCaptured = true;
    }
}
