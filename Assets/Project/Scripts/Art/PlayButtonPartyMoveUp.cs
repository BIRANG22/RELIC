using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayButtonPartyMoveUp : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button playButton;

    [Header("이동할 오브젝트")]
    [SerializeField] private RectTransform partyPanel;
    [SerializeField] private Transform anchor;

    [Header("이동 설정")]
    [SerializeField] private float partyPanelMoveY = 300f;
    [SerializeField] private float anchorMoveY = 300f;
    [SerializeField] private float moveDuration = 0.5f;

    [Tooltip("체크하면 Time.timeScale이 0이어도 애니메이션이 재생됩니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("이동 곡선")]
    [SerializeField]
    private AnimationCurve moveCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );

    [Header("애니메이션이 끝난 후 실행")]
    [SerializeField] private UnityEvent onAnimationFinished;

    private bool isPlaying;

    private void Awake()
    {
        if (playButton == null)
            playButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (playButton != null)
            playButton.onClick.AddListener(PlayMoveAnimation);
    }

    private void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(PlayMoveAnimation);
    }

    /// <summary>
    /// PartyPanel과 anchor를 동시에 위로 이동시킵니다.
    /// </summary>
    public void PlayMoveAnimation()
    {
        if (isPlaying)
            return;

        if (partyPanel == null && anchor == null)
        {
            Debug.LogWarning(
                "[PlayButtonPartyMoveUp] 이동할 PartyPanel 또는 anchor가 연결되지 않았습니다.",
                this
            );
            return;
        }

        StartCoroutine(MoveUpCoroutine());
    }

    private IEnumerator MoveUpCoroutine()
    {
        isPlaying = true;

        if (playButton != null)
            playButton.interactable = false;

        Vector2 partyStartPosition = Vector2.zero;
        Vector2 partyEndPosition = Vector2.zero;

        if (partyPanel != null)
        {
            partyStartPosition = partyPanel.anchoredPosition;
            partyEndPosition =
                partyStartPosition + new Vector2(0f, partyPanelMoveY);
        }

        RectTransform anchorRectTransform =
            anchor != null ? anchor.GetComponent<RectTransform>() : null;

        Vector2 anchorRectStartPosition = Vector2.zero;
        Vector2 anchorRectEndPosition = Vector2.zero;

        Vector3 anchorTransformStartPosition = Vector3.zero;
        Vector3 anchorTransformEndPosition = Vector3.zero;

        if (anchorRectTransform != null)
        {
            anchorRectStartPosition = anchorRectTransform.anchoredPosition;
            anchorRectEndPosition =
                anchorRectStartPosition + new Vector2(0f, anchorMoveY);
        }
        else if (anchor != null)
        {
            anchorTransformStartPosition = anchor.localPosition;
            anchorTransformEndPosition =
                anchorTransformStartPosition + new Vector3(0f, anchorMoveY, 0f);
        }

        float elapsedTime = 0f;
        float safeDuration = Mathf.Max(0.01f, moveDuration);

        while (elapsedTime < safeDuration)
        {
            elapsedTime += useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(elapsedTime / safeDuration);
            float curveValue = moveCurve.Evaluate(normalizedTime);

            if (partyPanel != null)
            {
                partyPanel.anchoredPosition = Vector2.LerpUnclamped(
                    partyStartPosition,
                    partyEndPosition,
                    curveValue
                );
            }

            if (anchorRectTransform != null)
            {
                anchorRectTransform.anchoredPosition = Vector2.LerpUnclamped(
                    anchorRectStartPosition,
                    anchorRectEndPosition,
                    curveValue
                );
            }
            else if (anchor != null)
            {
                anchor.localPosition = Vector3.LerpUnclamped(
                    anchorTransformStartPosition,
                    anchorTransformEndPosition,
                    curveValue
                );
            }

            yield return null;
        }

        if (partyPanel != null)
            partyPanel.anchoredPosition = partyEndPosition;

        if (anchorRectTransform != null)
            anchorRectTransform.anchoredPosition = anchorRectEndPosition;
        else if (anchor != null)
            anchor.localPosition = anchorTransformEndPosition;

        onAnimationFinished?.Invoke();

        isPlaying = false;
    }
}