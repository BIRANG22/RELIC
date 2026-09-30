using Relic.Gameplay.Data;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// BattleCharacterPanel의 스킬 버튼 호버/선택 시각 효과를 적용합니다.
/// 호버 시에는 Skill_Background 색상을 변경하고, Skill_Background2는 그리드 선택 중인 스킬에만 표시합니다.
/// </summary>
public enum BattleCharacterSkillLineFeedbackMode
{
    AutoSkill,
    Instant,
    PersistentExternal
}

public class BattleCharacterSkillHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Target")]
    [SerializeField] private Image normalBackgroundImage;
    [SerializeField] private Image hoverBackgroundImage;
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private Image lineImage;

    [Header("Hover Color")]
    [SerializeField] private Color hoverNormalBackgroundColor = new Color32(0x4E, 0x66, 0xDF, 0xFF);

    [Header("Selection Line")]
    [SerializeField] private Color normalLineColor = new Color32(0xA9, 0xB1, 0xBE, 0xFF);
    [SerializeField] private Color selectedLineColor = new Color32(0x4E, 0x66, 0xDF, 0xFF);
    [SerializeField, Min(0f)] private float skillLineClickFeedbackDuration = 0.15f;
    [SerializeField] private BattleCharacterSkillLineFeedbackMode lineFeedbackMode = BattleCharacterSkillLineFeedbackMode.AutoSkill;

    [Header("Hover Scale")]
    [SerializeField, Min(1f)] private float hoverScale = 1.05f;
    [SerializeField, Min(0f)] private float scaleLerpSpeed = 14f;

    [Header("Hover Alpha Breath")]
    [SerializeField, Range(0, 255)] private byte minimumAlpha = 150;
    [SerializeField, Range(0, 255)] private byte maximumAlpha = 255;
    [SerializeField, Min(0f)] private float alphaBreathSpeed = 3f;

    [Header("Direction Click Feedback")]
    [SerializeField, Min(0f)] private float directionClickFeedbackDuration = 0.15f;

    [Header("Tooltip Click Feedback")]
    [Tooltip("스킬 클릭 시 툴팁을 잠깐 숨겼다가 다시 표시하기까지의 시간입니다.")]
    [SerializeField, Min(0f)] private float tooltipClickRestartDelay = 0.06f;

    [Header("Auto Find")]
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private string hoverBackgroundObjectName = "Skill_Background2";

    private Vector3 normalScale = Vector3.one;
    private bool scaleCaptured;
    private bool isPointerOver;
    private bool isSelected;
    private float directionClickFeedbackUntil = -1f;
    private float skillLineClickFeedbackUntil = -1f;
    private PlayerSkillReservationController reservationController;
    private Color normalBackgroundOriginalColor = Color.white;
    private bool normalBackgroundColorCaptured;
    private BattleTimelineController battleTimelineController;
    private SkillMasterData skillData;
    private CharacterRuntimeData previewRuntime;
    private Action<SkillMasterData> skillInfoHandler;
    private Action skillInfoExitHandler;
    private Func<bool> persistentSelectionProvider;
    private Coroutine tooltipClickFeedbackRoutine;

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
        ApplySelectionLineVisual();
        ApplyScale(false);

        if (isSelected)
            ApplyBreathingAlpha();
    }

    private void OnDisable()
    {
        directionClickFeedbackUntil = -1f;
        skillLineClickFeedbackUntil = -1f;
        if (tooltipClickFeedbackRoutine != null)
        {
            StopCoroutine(tooltipClickFeedbackRoutine);
            tooltipClickFeedbackRoutine = null;
        }
        ClearSkillRangePreview();
        ResetVisual(true);
        ApplySelectionLineVisual();
        skillInfoExitHandler?.Invoke();
    }

    public void Configure(
        Image normalBackground,
        Image hoverBackground,
        RectTransform target,
        SkillMasterData previewSkillData = null,
        CharacterRuntimeData runtimeData = null,
        Action<SkillMasterData> onSkillHovered = null,
        Action onSkillHoverExited = null)
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
        skillInfoExitHandler = onSkillHoverExited;

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
        PlayTooltipClickFeedback();

        // Flip / ResetButton처럼 클릭 즉시 처리되는 버튼은
        // 일반 스킬과 동일하게 잠깐 선택색을 보여준 뒤 원래 Line 색으로 돌아옵니다.
        if (lineFeedbackMode == BattleCharacterSkillLineFeedbackMode.Instant)
        {
            skillLineClickFeedbackUntil = Time.unscaledTime + skillLineClickFeedbackDuration;
            ApplySelectionLineVisual();
            return;
        }

        // Compound처럼 별도의 그리드 대상 선택 상태를 갖는 버튼은
        // 선택이 끝날 때까지 Line 선택색을 유지합니다.
        if (lineFeedbackMode == BattleCharacterSkillLineFeedbackMode.PersistentExternal)
        {
            isSelected = true;
            skillLineClickFeedbackUntil = -1f;
            ApplySelectionLineVisual();
            return;
        }

        if (skillData == null)
            return;

        // 이동은 그리드 선택이 끝날 때까지 Line 선택색을 유지합니다.
        if (skillData.Category == Category.Move)
        {
            isSelected = true;
            skillLineClickFeedbackUntil = -1f;
            ApplySelectionLineVisual();
            ApplyHighlightVisual();
            return;
        }

        // 일반 스킬은 클릭 즉시 타임라인에 등록되는 느낌만 짧게 보여주고
        // 원래 Line 색으로 돌아옵니다.
        skillLineClickFeedbackUntil = Time.unscaledTime + skillLineClickFeedbackDuration;
        ApplySelectionLineVisual();

        if (skillData.RangeType == RangeType.Direction)
        {
            directionClickFeedbackUntil = Time.unscaledTime + directionClickFeedbackDuration;
            ApplyHighlightVisual();
            return;
        }

        ApplyHighlightVisual();
    }

    public void SetLineFeedbackMode(
        BattleCharacterSkillLineFeedbackMode mode,
        Func<bool> selectionProvider = null)
    {
        lineFeedbackMode = mode;
        persistentSelectionProvider = selectionProvider;
        skillLineClickFeedbackUntil = -1f;
        RefreshSelectedState();
        ApplySelectionLineVisual();
    }

    private void PlayTooltipClickFeedback()
    {
        if (skillData == null || skillInfoHandler == null || skillInfoExitHandler == null)
            return;

        if (tooltipClickFeedbackRoutine != null)
            StopCoroutine(tooltipClickFeedbackRoutine);

        tooltipClickFeedbackRoutine = StartCoroutine(TooltipClickFeedbackRoutine());
    }

    private System.Collections.IEnumerator TooltipClickFeedbackRoutine()
    {
        // 등록 클릭이 들어왔다는 느낌이 나도록 현재 툴팁을 먼저 페이드아웃합니다.
        skillInfoExitHandler?.Invoke();

        float delay = Mathf.Max(0f, tooltipClickRestartDelay);
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 클릭 후에도 같은 스킬 위에 커서가 남아 있을 때만 다시 페이드인합니다.
        if (isPointerOver && isActiveAndEnabled && IsInteractable() && skillData != null)
            skillInfoHandler?.Invoke(skillData);

        tooltipClickFeedbackRoutine = null;
    }

    public void SetSkillInfoHandler(Action<SkillMasterData> onSkillHovered, Action onSkillHoverExited = null)
    {
        skillInfoHandler = onSkillHovered;
        skillInfoExitHandler = onSkillHoverExited;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        isPointerOver = true;
        EnsureNormalBackgroundVisible();
        ApplyNormalBackgroundHoverColor();

        ApplyHighlightVisual();
        ApplySelectionLineVisual();
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
        ApplySelectionLineVisual();
        ApplyScale(false);
        skillInfoExitHandler?.Invoke();
    }

    private void ResetVisual(bool instant)
    {
        isPointerOver = false;
        EnsureNormalBackgroundVisible();
        RestoreNormalBackgroundColor();

        if (hoverBackgroundImage != null)
            hoverBackgroundImage.gameObject.SetActive(false);

        ApplySelectionLineVisual();
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
        if (lineFeedbackMode == BattleCharacterSkillLineFeedbackMode.PersistentExternal)
        {
            isSelected = persistentSelectionProvider != null && persistentSelectionProvider();
            return;
        }

        if (lineFeedbackMode == BattleCharacterSkillLineFeedbackMode.Instant)
        {
            isSelected = false;
            return;
        }

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

    private bool IsSkillLineClickFeedbackActive()
    {
        return skillLineClickFeedbackUntil >= 0f &&
               Time.unscaledTime < skillLineClickFeedbackUntil;
    }

    private void ApplySelectionLineVisual()
    {
        ResolveLineReference();

        if (lineImage == null)
            return;

        // 데이터가 없거나 사용할 수 없는 슬롯의 Line 색은
        // BattleCharacterPanelUI가 777777 / A9B1BE 규칙에 맞게 관리합니다.
        if (!IsInteractable())
            return;

        if (lineFeedbackMode == BattleCharacterSkillLineFeedbackMode.AutoSkill && skillData == null)
            return;

        bool showSelectedLine;

        if (lineFeedbackMode == BattleCharacterSkillLineFeedbackMode.Instant)
        {
            showSelectedLine = IsSkillLineClickFeedbackActive();
        }
        else if (lineFeedbackMode == BattleCharacterSkillLineFeedbackMode.PersistentExternal)
        {
            showSelectedLine = isSelected;
        }
        else
        {
            bool isMoveSkill = skillData != null && skillData.Category == Category.Move;
            showSelectedLine = isMoveSkill
                ? isSelected
                : IsSkillLineClickFeedbackActive();
        }

        Color target = showSelectedLine ? selectedLineColor : normalLineColor;
        Color current = lineImage.color;
        current.r = target.r;
        current.g = target.g;
        current.b = target.b;
        lineImage.color = current;
    }

    private void ResolveLineReference()
    {
        if (lineImage != null)
            return;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child.name != "Line")
                continue;

            lineImage = child.GetComponent<Image>();
            if (lineImage != null)
                return;
        }
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

            if (lineImage == null && child.name == "Line")
                lineImage = child.GetComponent<Image>();
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
