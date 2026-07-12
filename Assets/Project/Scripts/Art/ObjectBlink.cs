using UnityEngine;

/// <summary>
/// CanvasGroup의 알파값을 반복해서 변경하여
/// UI 오브젝트 전체를 깜빡이게 합니다.
/// 자식 이미지와 텍스트도 함께 투명해집니다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ObjectBlink : MonoBehaviour
{
    [Header("깜빡임 설정")]

    [Tooltip("가장 투명해졌을 때의 알파값입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float minimumAlpha = 0.2f;

    [Tooltip("가장 선명할 때의 알파값입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float maximumAlpha = 1f;

    [Tooltip("깜빡이는 속도입니다.")]
    [Min(0.01f)]
    [SerializeField] private float blinkSpeed = 1.5f;

    [Tooltip("오브젝트가 켜질 때 최대 알파값부터 시작합니다.")]
    [SerializeField] private bool startFromMaximumAlpha = true;

    [Tooltip("오브젝트가 꺼질 때 원래 알파값으로 복구합니다.")]
    [SerializeField] private bool restoreOriginalAlphaOnDisable = true;


    private CanvasGroup canvasGroup;
    private float originalAlpha;
    private float blinkTime;


    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        originalAlpha = canvasGroup.alpha;
    }


    private void OnEnable()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        blinkTime = startFromMaximumAlpha
            ? 0.5f
            : 0f;

        ApplyCurrentAlpha();
    }


    private void Update()
    {
        if (canvasGroup == null)
        {
            return;
        }

        blinkTime += Time.unscaledDeltaTime * blinkSpeed;

        ApplyCurrentAlpha();
    }


    private void OnDisable()
    {
        if (!restoreOriginalAlphaOnDisable)
        {
            return;
        }

        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = originalAlpha;
    }


    /// <summary>
    /// 현재 시간에 맞춰 오브젝트의 알파값을 적용합니다.
    /// </summary>
    private void ApplyCurrentAlpha()
    {
        float pingPongValue =
            Mathf.PingPong(blinkTime, 1f);

        float smoothValue =
            Mathf.SmoothStep(
                0f,
                1f,
                pingPongValue
            );

        canvasGroup.alpha =
            Mathf.Lerp(
                minimumAlpha,
                maximumAlpha,
                smoothValue
            );
    }
}