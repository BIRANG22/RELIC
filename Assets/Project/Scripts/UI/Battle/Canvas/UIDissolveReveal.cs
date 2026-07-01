using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIDissolveReveal : MonoBehaviour
{
    [SerializeField] private RawImage targetRawImage;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private string revealProperty = "_Reveal";
    [SerializeField] private string directionProperty = "_Direction";

    [SerializeField, Range(0f, 1f)] private float hiddenReveal = 0f;
    [SerializeField, Range(0f, 1f)] private float shownReveal = 0.5f;

    [Header("Info Content Alignment")]
    [SerializeField] private HorizontalOrVerticalLayoutGroup infoContentLayout;
    [SerializeField] private RectTransform monsterInfoPanel;
    [SerializeField, Min(0f)] private float horizontalPanelPadding = 80f;
    [SerializeField, Min(0f)] private float verticalBoundsSpacing = 24f;
    [SerializeField] private TextAnchor leftAlignment = TextAnchor.UpperLeft;
    [SerializeField] private TextAnchor rightAlignment = TextAnchor.UpperRight;

    private Material runtimeMaterial;
    private Coroutine routine;
    private readonly Vector3[] worldCorners = new Vector3[4];

    private void Awake()
    {
        if (targetRawImage == null)
            targetRawImage = GetComponent<RawImage>();

        if (targetRawImage == null || targetRawImage.material == null)
            return;

        runtimeMaterial = Instantiate(targetRawImage.material);
        runtimeMaterial.name = targetRawImage.material.name + " (Runtime)";
        targetRawImage.material = runtimeMaterial;

        runtimeMaterial.SetFloat(revealProperty, hiddenReveal);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            ShowFromLeft();

        if (Input.GetKeyDown(KeyCode.RightArrow))
            ShowFromRight();

        if (Input.GetKeyDown(KeyCode.UpArrow))
            HideToLeft();

        if (Input.GetKeyDown(KeyCode.DownArrow))
            HideToRight();
    }

    public void ShowFromLeft()
    {
        SetDirection(0f);
        AlignContentLeft();
        Show();
    }

    public void ShowFromRight()
    {
        SetDirection(1f);
        AlignContentRight();
        Show();
    }

    public void HideToLeft()
    {
        SetDirection(1f);
        AlignContentRight();
        Hide();
    }

    public void HideToRight()
    {
        SetDirection(0f);
        AlignContentLeft();
        Hide();
    }

    public void Show()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        Play(hiddenReveal, shownReveal);
    }

    public void Hide()
    {
        Play(shownReveal, hiddenReveal);
    }

    private void Play(float from, float to)
    {
        if (runtimeMaterial == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(RevealRoutine(from, to));
    }

    private IEnumerator RevealRoutine(float from, float to)
    {
        float time = 0f;
        runtimeMaterial.SetFloat(revealProperty, from);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            float value = Mathf.Lerp(from, to, t);
            runtimeMaterial.SetFloat(revealProperty, value);

            yield return null;
        }

        runtimeMaterial.SetFloat(revealProperty, to);
        routine = null;
    }

    private void SetDirection(float direction)
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetFloat(directionProperty, direction);
    }

    private void AlignContentLeft()
    {
        ApplyContentAlignment(false);
    }

    private void AlignContentRight()
    {
        ApplyContentAlignment(true);
    }

    private void ApplyContentAlignment(bool alignRight)
    {
        RectTransform panel = ResolveMonsterInfoPanel();
        if (panel == null)
            return;

        if (infoContentLayout != null)
        {
            infoContentLayout.childAlignment = alignRight ? rightAlignment : leftAlignment;
            infoContentLayout.enabled = false;
        }

        float targetX = alignRight
            ? panel.rect.max.x - horizontalPanelPadding
            : panel.rect.min.x + horizontalPanelPadding;
        float nextTop = panel.rect.max.y - verticalBoundsSpacing;

        for (int i = 0; i < panel.childCount; i++)
        {
            RectTransform child = panel.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
                continue;

            Bounds bounds = GetBoundsInPanel(panel, child);
            float deltaX = alignRight ? targetX - bounds.max.x : targetX - bounds.min.x;
            float deltaY = nextTop - bounds.max.y;

            child.localPosition += new Vector3(deltaX, deltaY, 0f);
            nextTop -= bounds.size.y + verticalBoundsSpacing;
        }
    }

    private RectTransform ResolveMonsterInfoPanel()
    {
        if (monsterInfoPanel != null)
            return monsterInfoPanel;

        if (infoContentLayout == null)
            return null;

        monsterInfoPanel = infoContentLayout.GetComponent<RectTransform>();
        return monsterInfoPanel;
    }

    private Bounds GetBoundsInPanel(RectTransform panel, RectTransform rect)
    {
        rect.GetWorldCorners(worldCorners);

        Vector3 min = panel.InverseTransformPoint(worldCorners[0]);
        Vector3 max = min;

        for (int i = 1; i < worldCorners.Length; i++)
        {
            Vector3 point = panel.InverseTransformPoint(worldCorners[i]);
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        Bounds bounds = new();
        bounds.SetMinMax(min, max);
        return bounds;
    }
}
