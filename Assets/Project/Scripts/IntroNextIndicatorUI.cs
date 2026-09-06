using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인트로 문장의 타자 효과가 끝난 뒤 다음 진행 표시 이미지를
/// 마지막으로 표시된 글자 바로 뒤에 배치하고 위아래로 반복 이동시킵니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class IntroNextIndicatorUI : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("인트로 문장을 출력하는 TMP 텍스트입니다. 비워두면 같은 부모 아래에서 자동으로 찾습니다.")]
    [SerializeField] private TMP_Text introText;

    [Tooltip("표시/숨김할 이미지입니다. 비워두면 이 오브젝트의 Graphic을 사용합니다.")]
    [SerializeField] private Graphic indicatorGraphic;

    [Header("텍스트 끝 위치")]
    [Tooltip("마지막 글자 끝에서 표시 이미지를 얼마나 떨어뜨릴지 지정합니다.")]
    [SerializeField] private Vector2 endOffset = new Vector2(12f, 0f);

    [Header("위아래 이동")]
    [Tooltip("기준 위치(Y 오프셋 0)를 중심으로 위아래로 움직이는 거리입니다. 5로 설정하면 -5 ~ +5 범위로 이동합니다.")]
    [Min(0f)]
    [SerializeField] private float indicatorMoveDistance = 5f;

    [Tooltip("초당 위아래 반복 횟수입니다.")]
    [Min(0f)]
    [SerializeField] private float indicatorMoveSpeed = 0.5f;

    private RectTransform indicatorRect;
    private RectTransform indicatorParentRect;
    private Vector2 baseAnchoredPosition;
    private string lastText;
    private int lastVisibleCharacters = -1;
    private int lastCharacterCount = -1;
    private bool isShown;
    private float moveStartedAt;

    private void Awake()
    {
        indicatorRect = transform as RectTransform;
        indicatorParentRect = indicatorRect != null ? indicatorRect.parent as RectTransform : null;

        if (indicatorGraphic == null)
            indicatorGraphic = GetComponent<Graphic>();

        FindIntroTextIfNeeded();
        SetIndicatorVisible(false);
    }

    private void OnEnable()
    {
        FindIntroTextIfNeeded();
        RefreshIndicatorState(true);
    }

    private void Update()
    {
        RefreshIndicatorState(false);

        if (!isShown || indicatorRect == null)
            return;

        // 표시되는 순간에는 RectTransform Pos Y 기준 위치(기본 0)에서 시작합니다.
        // indicatorMoveDistance가 5라면 기준 위치를 중심으로 -5 ~ +5 범위만 이동합니다.
        float elapsed = Mathf.Max(0f, Time.unscaledTime - moveStartedAt);
        float offsetY = Mathf.Sin(elapsed * indicatorMoveSpeed * Mathf.PI * 2f)
                        * indicatorMoveDistance;

        indicatorRect.anchoredPosition = baseAnchoredPosition + new Vector2(0f, offsetY);
    }

    private void FindIntroTextIfNeeded()
    {
        if (introText != null)
            return;

        Transform parent = transform.parent;
        if (parent == null)
            return;

        Transform namedText = parent.Find("Text");
        if (namedText != null)
            introText = namedText.GetComponent<TMP_Text>();

        if (introText != null)
            return;

        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null || texts[i].transform.IsChildOf(transform))
                continue;

            introText = texts[i];
            break;
        }
    }

    private void RefreshIndicatorState(bool force)
    {
        if (introText == null || indicatorRect == null || indicatorParentRect == null)
        {
            SetIndicatorVisible(false);
            return;
        }

        introText.ForceMeshUpdate();

        int characterCount = introText.textInfo != null ? introText.textInfo.characterCount : 0;
        int visibleCharacters = introText.maxVisibleCharacters;
        string currentText = introText.text ?? string.Empty;

        bool changed = force ||
                       !string.Equals(lastText, currentText) ||
                       lastVisibleCharacters != visibleCharacters ||
                       lastCharacterCount != characterCount;

        lastText = currentText;
        lastVisibleCharacters = visibleCharacters;
        lastCharacterCount = characterCount;

        if (!changed)
            return;

        bool typingFinished = characterCount > 0 && visibleCharacters >= characterCount;
        if (!typingFinished)
        {
            SetIndicatorVisible(false);
            return;
        }

        int lastVisibleIndex = FindLastVisibleCharacterIndex(characterCount);
        if (lastVisibleIndex < 0)
        {
            SetIndicatorVisible(false);
            return;
        }

        PositionAfterCharacter(lastVisibleIndex);
        SetIndicatorVisible(true);
    }

    private int FindLastVisibleCharacterIndex(int characterCount)
    {
        if (introText == null || introText.textInfo == null)
            return -1;

        for (int i = characterCount - 1; i >= 0; i--)
        {
            TMP_CharacterInfo characterInfo = introText.textInfo.characterInfo[i];
            if (characterInfo.isVisible)
                return i;
        }

        return -1;
    }

    private void PositionAfterCharacter(int characterIndex)
    {
        TMP_CharacterInfo characterInfo = introText.textInfo.characterInfo[characterIndex];
        RectTransform textRect = introText.rectTransform;

        // 마지막 글자의 오른쪽 중앙을 기준점으로 사용합니다.
        Vector3 characterRightCenter = (characterInfo.topRight + characterInfo.bottomRight) * 0.5f;
        Vector3 worldPosition = textRect.TransformPoint(characterRightCenter);
        Vector3 localPosition = indicatorParentRect.InverseTransformPoint(worldPosition);

        // X는 마지막 글자 끝을 따라가지만 Y는 텍스트 위치를 따라가지 않습니다.
        // Image의 RectTransform Pos Y를 항상 endOffset.y(기본 0)로 두고
        // 그 값을 중심으로 위아래 이동하도록 합니다.
        float anchoredXOffset = indicatorRect.anchoredPosition.x - indicatorRect.localPosition.x;
        baseAnchoredPosition = new Vector2(
            localPosition.x + endOffset.x + anchoredXOffset,
            endOffset.y);

        indicatorRect.anchoredPosition = baseAnchoredPosition;
    }

    private void SetIndicatorVisible(bool visible)
    {
        bool wasShown = isShown;
        isShown = visible;

        if (indicatorGraphic != null)
            indicatorGraphic.enabled = visible;

        if (indicatorRect == null)
            return;

        if (visible && !wasShown)
        {
            // 처음 표시되는 프레임은 Pos Y 0(또는 End Offset Y)에서 시작합니다.
            moveStartedAt = Time.unscaledTime;
            indicatorRect.anchoredPosition = baseAnchoredPosition;
        }
        else if (!visible)
        {
            // 숨겨질 때도 기준 위치로 되돌려 다음 문장에서 이전 위상이 남지 않게 합니다.
            indicatorRect.anchoredPosition = baseAnchoredPosition;
        }
    }
}
