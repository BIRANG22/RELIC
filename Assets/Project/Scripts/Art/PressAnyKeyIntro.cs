using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// 아무 키를 누르면 효과음을 재생하고,
/// 배경 머티리얼의 테두리를 순서대로 제거합니다.
/// 동시에 타이틀의 위치와 크기를 변경하며,
/// 모든 연출이 종료되면 이 스크립트가 붙어 있는 StartImage를 비활성화합니다.
/// </summary>
public class PressAnyKeyIntro : MonoBehaviour
{
    [Header("오브젝트 연결")]

    [Tooltip("circle_canvas 머티리얼을 사용 중인 Background Image입니다.")]
    [SerializeField] private Graphic backgroundGraphic;

    [Tooltip("위치와 크기를 변경할 Title 오브젝트입니다.")]
    [SerializeField] private RectTransform titleRect;

    [Tooltip("입력 후 숨길 PRESS ANY KEY 오브젝트입니다.")]
    [SerializeField] private GameObject pressAnyKeyObject;

    [Tooltip("연출이 끝난 뒤 활성화할 메인 메뉴 패널입니다.")]
    [SerializeField] private GameObject mainMenuPanel;


    [Header("입력 설정")]

    [Tooltip("씬 진입 직후 입력을 무시할 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float inputDelay = 0.5f;

    [Tooltip("마우스 클릭도 아무 키 입력으로 처리합니다.")]
    [SerializeField] private bool allowMouseInput = true;

    [Tooltip("게임패드 버튼도 아무 키 입력으로 처리합니다.")]
    [SerializeField] private bool allowGamepadInput = true;


    [Header("효과음 설정")]

    [Tooltip("Any Key 입력 시 효과음을 재생합니다.")]
    [SerializeField] private bool playAnyKeySound = true;

    [Tooltip("Any Key 입력 시 재생할 효과음입니다.")]
    [SerializeField] private SfxType anyKeySfx = SfxType.NormalButtonClick;


    [Header("테두리 연출")]

    [Tooltip("첫 번째 boder가 0이 되는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float borderDuration = 0.25f;

    [Tooltip("두 번째 boder가 0이 되는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float border1Duration = 0.25f;

    [Tooltip("세 번째 boder가 0이 되는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float border2Duration = 0.25f;

    [Tooltip("각 테두리 연출 사이의 대기시간입니다.")]
    [Min(0f)]
    [SerializeField] private float borderInterval = 0f;

    [Tooltip("테두리 값이 줄어드는 움직임 곡선입니다.")]
    [SerializeField]
    private AnimationCurve borderCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    [Header("타이틀 연출")]

    [Tooltip("타이틀의 최종 Anchored Position입니다.")]
    [SerializeField] private Vector2 targetTitlePosition;

    [Tooltip("타이틀의 최종 Local Scale입니다.")]
    [SerializeField] private Vector3 targetTitleScale = Vector3.one;

    [Tooltip("타이틀 이동 시간입니다. 0이면 테두리 전체 연출 시간에 맞춰집니다.")]
    [Min(0f)]
    [SerializeField] private float titleDuration = 0f;

    [Tooltip("타이틀 이동과 크기 변경에 사용할 움직임 곡선입니다.")]
    [SerializeField]
    private AnimationCurve titleCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    [Header("DUS / IUM 시작 연출")]

    [Tooltip("게임 시작 시 이동시킬 dus 오브젝트입니다.")]
    [SerializeField] private RectTransform dusRect;

    [Tooltip("게임 시작 시 이동시킬 ium 오브젝트입니다.")]
    [SerializeField] private RectTransform iumRect;

    [Tooltip("게임 시작 시 dus가 출발할 Anchored Position입니다.")]
    [SerializeField] private Vector2 dusStartPosition;

    [Tooltip("게임 시작 시 dus가 도착할 Anchored Position입니다.")]
    [SerializeField] private Vector2 dusTargetPosition;

    [Tooltip("게임 시작 시 ium이 출발할 Anchored Position입니다.")]
    [SerializeField] private Vector2 iumStartPosition;

    [Tooltip("게임 시작 시 ium이 도착할 Anchored Position입니다.")]
    [SerializeField] private Vector2 iumTargetPosition;

    [Tooltip("씬이 켜진 뒤 DUS / IUM 이동을 시작하기 전 대기시간입니다.")]
    [Min(0f)]
    [SerializeField] private float logoPartStartDelay = 0f;

    [Tooltip("DUS / IUM이 지정된 위치로 이동하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float logoPartMoveDuration = 0.6f;

    [Tooltip("DUS / IUM 이동에 사용할 움직임 곡선입니다.")]
    [SerializeField]
    private AnimationCurve logoPartMoveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    [Header("쉐이더 프로퍼티")]

    [Tooltip("첫 번째 boder 프로퍼티 이름입니다.")]
    [SerializeField] private string borderProperty = "_boder";

    [Tooltip("두 번째 boder 프로퍼티 이름입니다.")]
    [SerializeField] private string border1Property = "_boder_1";

    [Tooltip("세 번째 boder 프로퍼티 이름입니다.")]
    [SerializeField] private string border2Property = "_boder_2";

    [Tooltip("Alpha Clip Threshold 프로퍼티 이름입니다.")]
    [SerializeField]
    private string alphaClipThresholdProperty = "_Alpha_Clip_Threshold";


    // 실행 중 한 번 시작 연출을 완료했다면, 이후 타이틀 진입에서는 StartImage를 다시 표시하지 않습니다.
    private static bool introCompletedInThisRun;
    private static bool skipIntroOnNextTitleLoad;

    private static readonly Vector2 completedTitlePosition =
        new Vector2(-526f, 258f);

    private static readonly Vector3 completedTitleScale =
        new Vector3(0.7f, 0.7f, 1f);

    private Material runtimeMaterial;

    private bool canReceiveInput;
    private bool isPlaying;


    /// <summary>
    /// 게임 중 타이틀로 돌아갈 때 다음 타이틀 진입의 시작 연출을 생략합니다.
    /// </summary>
    public static void SkipIntroOnNextTitleLoad()
    {
        introCompletedInThisRun = true;
        skipIntroOnNextTitleLoad = true;
    }


    private void Awake()
    {
        // 타이틀의 최종 위치와 크기는 항상 동일한 값으로 사용합니다.
        targetTitlePosition = completedTitlePosition;
        targetTitleScale = completedTitleScale;

        if (introCompletedInThisRun || skipIntroOnNextTitleLoad)
        {
            skipIntroOnNextTitleLoad = false;
            ApplyCompletedIntroState();
            return;
        }

        CreateRuntimeMaterial();

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }


    /// <summary>
    /// 시작 연출이 완료된 상태를 즉시 적용합니다.
    /// </summary>
    private void ApplyCompletedIntroState()
    {
        if (titleRect != null)
        {
            titleRect.anchoredPosition = completedTitlePosition;
            titleRect.localScale = completedTitleScale;
        }

        ApplyLogoPartCompletedState();

        if (pressAnyKeyObject != null)
        {
            pressAnyKeyObject.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        gameObject.SetActive(false);
    }


    private void OnEnable()
    {
        canReceiveInput = false;
        isPlaying = false;

        StartCoroutine(EnableInputAfterDelay());
        StartCoroutine(PlayLogoPartStartAnimation());
    }


    private void Update()
    {
        if (!canReceiveInput || isPlaying)
        {
            return;
        }

        if (WasAnyInputPressed())
        {
            isPlaying = true;
            canReceiveInput = false;

            // Any Key 입력과 동시에 효과음을 재생합니다.
            PlayAnyKeySound();

            StartCoroutine(PlayIntroSequence());
        }
    }


    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }


    /// <summary>
    /// Any Key 입력 시 지정된 효과음을 재생합니다.
    /// </summary>
    private void PlayAnyKeySound()
    {
        if (!playAnyKeySound)
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "[PressAnyKeyIntro] AudioManager.Instance가 없어 효과음을 재생할 수 없습니다.",
                this
            );

            return;
        }

        AudioManager.Instance.PlaySfx(anyKeySfx);
    }


    /// <summary>
    /// 원본 머티리얼이 변경되지 않도록 실행 전용 머티리얼을 만듭니다.
    /// </summary>
    private void CreateRuntimeMaterial()
    {
        if (backgroundGraphic == null)
        {
            Debug.LogError(
                "[PressAnyKeyIntro] Background Graphic이 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (backgroundGraphic.material == null)
        {
            Debug.LogError(
                "[PressAnyKeyIntro] Background Graphic에 머티리얼이 없습니다.",
                this
            );

            return;
        }

        runtimeMaterial = new Material(backgroundGraphic.material);

        runtimeMaterial.name =
            backgroundGraphic.material.name + " (Runtime)";

        backgroundGraphic.material = runtimeMaterial;
        backgroundGraphic.SetMaterialDirty();
    }


    /// <summary>
    /// 씬 진입 직후 남아 있는 입력을 잠시 무시합니다.
    /// </summary>
    private IEnumerator EnableInputAfterDelay()
    {
        if (inputDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(inputDelay);
        }

        canReceiveInput = true;
    }


    /// <summary>
    /// 키보드, 마우스, 게임패드 입력을 확인합니다.
    /// </summary>
    private bool WasAnyInputPressed()
    {
        if (Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        if (allowMouseInput && Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame ||
                Mouse.current.middleButton.wasPressedThisFrame)
            {
                return true;
            }
        }

        if (allowGamepadInput && Gamepad.current != null)
        {
            foreach (InputControl control in Gamepad.current.allControls)
            {
                if (control is ButtonControl button &&
                    button.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }

        return false;
    }


    /// <summary>
    /// 전체 시작 화면 연출을 실행합니다.
    /// </summary>
    private IEnumerator PlayIntroSequence()
    {
        if (pressAnyKeyObject != null)
        {
            pressAnyKeyObject.SetActive(false);
        }

        float totalBorderDuration =
            borderDuration +
            border1Duration +
            border2Duration +
            borderInterval * 2f;

        float resolvedTitleDuration =
            titleDuration > 0f
                ? titleDuration
                : totalBorderDuration;

        Coroutine titleRoutine = null;

        if (titleRect != null)
        {
            titleRoutine =
                StartCoroutine(AnimateTitle(resolvedTitleDuration));
        }

        if (runtimeMaterial != null)
        {
            yield return AnimateMaterialFloat(
                borderProperty,
                0f,
                borderDuration
            );

            yield return WaitForBorderInterval();

            yield return AnimateMaterialFloat(
                border1Property,
                0f,
                border1Duration
            );

            yield return WaitForBorderInterval();

            yield return AnimateMaterialFloat(
                border2Property,
                0f,
                border2Duration
            );

            DisableAlphaClipping();
        }

        if (titleRoutine != null)
        {
            yield return titleRoutine;
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        // 이번 실행에서 시작 연출이 완료되었음을 저장합니다.
        // 이후 로비나 배틀에서 타이틀로 돌아와도 StartImage는 다시 표시되지 않습니다.
        introCompletedInThisRun = true;

        // 모든 연출이 끝난 뒤 StartImage 자체를 비활성화합니다.
        gameObject.SetActive(false);
    }


    /// <summary>
    /// 머티리얼의 Float 값을 지정한 시간 동안 변경합니다.
    /// </summary>
    private IEnumerator AnimateMaterialFloat(
        string propertyName,
        float targetValue,
        float duration
    )
    {
        if (runtimeMaterial == null ||
            string.IsNullOrWhiteSpace(propertyName))
        {
            yield break;
        }

        if (!runtimeMaterial.HasProperty(propertyName))
        {
            Debug.LogWarning(
                $"[PressAnyKeyIntro] 머티리얼에 '{propertyName}' 프로퍼티가 없습니다.",
                this
            );

            yield break;
        }

        float startValue =
            runtimeMaterial.GetFloat(propertyName);

        if (duration <= 0f)
        {
            runtimeMaterial.SetFloat(
                propertyName,
                targetValue
            );

            backgroundGraphic.SetMaterialDirty();

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / duration);

            float curvedTime =
                borderCurve.Evaluate(normalizedTime);

            float currentValue =
                Mathf.LerpUnclamped(
                    startValue,
                    targetValue,
                    curvedTime
                );

            runtimeMaterial.SetFloat(
                propertyName,
                currentValue
            );

            backgroundGraphic.SetMaterialDirty();

            yield return null;
        }

        runtimeMaterial.SetFloat(
            propertyName,
            targetValue
        );

        backgroundGraphic.SetMaterialDirty();
    }


    /// <summary>
    /// 타이틀의 위치와 크기를 동시에 변경합니다.
    /// </summary>
    private IEnumerator AnimateTitle(float duration)
    {
        Vector2 startPosition =
            titleRect.anchoredPosition;

        Vector3 startScale =
            titleRect.localScale;

        if (duration <= 0f)
        {
            titleRect.anchoredPosition =
                targetTitlePosition;

            titleRect.localScale =
                targetTitleScale;

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / duration);

            float curvedTime =
                titleCurve.Evaluate(normalizedTime);

            titleRect.anchoredPosition =
                Vector2.LerpUnclamped(
                    startPosition,
                    targetTitlePosition,
                    curvedTime
                );

            titleRect.localScale =
                Vector3.LerpUnclamped(
                    startScale,
                    targetTitleScale,
                    curvedTime
                );

            yield return null;
        }

        titleRect.anchoredPosition =
            targetTitlePosition;

        titleRect.localScale =
            targetTitleScale;
    }


    /// <summary>
    /// 각 테두리 연출 사이의 대기시간입니다.
    /// </summary>
    private IEnumerator WaitForBorderInterval()
    {
        if (borderInterval > 0f)
        {
            yield return new WaitForSecondsRealtime(
                borderInterval
            );
        }
    }


    /// <summary>
    /// Shader Graph의 Alpha Clipping 효과를 해제합니다.
    /// </summary>
    private void DisableAlphaClipping()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(alphaClipThresholdProperty) &&
            runtimeMaterial.HasProperty(alphaClipThresholdProperty))
        {
            runtimeMaterial.SetFloat(
                alphaClipThresholdProperty,
                0f
            );
        }

        if (runtimeMaterial.HasProperty("_AlphaClip"))
        {
            runtimeMaterial.SetFloat(
                "_AlphaClip",
                0f
            );
        }

        runtimeMaterial.DisableKeyword("_ALPHATEST_ON");

        backgroundGraphic.SetMaterialDirty();
    }

    /// <summary>
    /// 게임 시작 시 DUS와 IUM을 각각 지정된 시작 위치에서 목표 위치로 이동시킵니다.
    /// </summary>
    private IEnumerator PlayLogoPartStartAnimation()
    {
        if (dusRect == null && iumRect == null)
        {
            yield break;
        }

        if (dusRect != null)
        {
            dusRect.anchoredPosition = dusStartPosition;
        }

        if (iumRect != null)
        {
            iumRect.anchoredPosition = iumStartPosition;
        }

        if (logoPartStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(logoPartStartDelay);
        }

        if (logoPartMoveDuration <= 0f)
        {
            ApplyLogoPartCompletedState();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < logoPartMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / logoPartMoveDuration);

            float curvedTime =
                logoPartMoveCurve.Evaluate(normalizedTime);

            if (dusRect != null)
            {
                dusRect.anchoredPosition =
                    Vector2.LerpUnclamped(
                        dusStartPosition,
                        dusTargetPosition,
                        curvedTime
                    );
            }

            if (iumRect != null)
            {
                iumRect.anchoredPosition =
                    Vector2.LerpUnclamped(
                        iumStartPosition,
                        iumTargetPosition,
                        curvedTime
                    );
            }

            yield return null;
        }

        ApplyLogoPartCompletedState();
    }


    /// <summary>
    /// DUS와 IUM을 시작 연출이 끝난 최종 위치로 즉시 배치합니다.
    /// </summary>
    private void ApplyLogoPartCompletedState()
    {
        if (dusRect != null)
        {
            dusRect.anchoredPosition = dusTargetPosition;
        }

        if (iumRect != null)
        {
            iumRect.anchoredPosition = iumTargetPosition;
        }
    }

}