using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;

public class BattleGoldHudUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text goldText;

    [Header("Display")]
    [SerializeField] private string emptyText = "0";
    [SerializeField] private string format = "{0}";

    [Header("숫자 변화 연출")]
    [Tooltip("현재 숫자에서 최종 숫자까지 변하는 전체 시간입니다. 변화량과 관계없이 같은 시간 안에 도착합니다.")]
    [Min(0f)]
    [SerializeField] private float numberChangeDuration = 0.2f;

    private Coroutine numberChangeCoroutine;
    private int displayedValue;
    private bool hasDisplayedValue;

    private void Awake()
    {
        AutoBind();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        StopNumberChangeCoroutine();
        hasDisplayedValue = false;
    }

    private void AutoBind()
    {
        if (goldText != null)
            return;

        Transform goldTextTransform = transform.Find("GoldText");

        if (goldTextTransform != null)
            goldText = goldTextTransform.GetComponent<TMP_Text>();

        if (goldText == null)
            goldText = GetComponentInChildren<TMP_Text>(true);
    }

    public void Refresh()
    {
        AutoBind();

        if (goldText == null)
            return;

        int targetValue = GetRemnantValue();

        // 처음 표시할 때는 현재 보유량을 즉시 보여줍니다.
        if (!hasDisplayedValue)
        {
            StopNumberChangeCoroutine();
            displayedValue = targetValue;
            hasDisplayedValue = true;
            ApplyDisplayedValue();
            return;
        }

        if (displayedValue == targetValue)
        {
            StopNumberChangeCoroutine();
            ApplyDisplayedValue();
            return;
        }

        StopNumberChangeCoroutine();
        numberChangeCoroutine = StartCoroutine(AnimateNumberChange(targetValue));
    }

    private IEnumerator AnimateNumberChange(int targetValue)
    {
        int startValue = displayedValue;

        if (numberChangeDuration <= 0f)
        {
            displayedValue = targetValue;
            ApplyDisplayedValue();
            numberChangeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < numberChangeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / numberChangeDuration);

            displayedValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, progress));
            ApplyDisplayedValue();

            yield return null;
        }

        displayedValue = targetValue;
        ApplyDisplayedValue();
        numberChangeCoroutine = null;
    }

    private void ApplyDisplayedValue()
    {
        if (goldText == null)
            return;

        goldText.text = FormatRemnantText(displayedValue);
    }

    private void StopNumberChangeCoroutine()
    {
        if (numberChangeCoroutine == null)
            return;

        StopCoroutine(numberChangeCoroutine);
        numberChangeCoroutine = null;
    }

    private string FormatRemnantText(int value)
    {
        string valueText = value.ToString();

        if (string.IsNullOrWhiteSpace(valueText))
            valueText = emptyText;

        if (string.IsNullOrWhiteSpace(format))
            return valueText;

        try
        {
            return string.Format(format, valueText);
        }
        catch (System.FormatException)
        {
            return valueText;
        }
    }

    public static void RefreshAll()
    {
        BattleGoldHudUI[] huds = Object.FindObjectsByType<BattleGoldHudUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < huds.Length; i++)
        {
            if (huds[i] != null)
                huds[i].Refresh();
        }

        RefreshFallbackGoldTexts();
    }

    private static void RefreshFallbackGoldTexts()
    {
        string text = GetRemnantValue().ToString();
        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text target = texts[i];

            if (target == null)
                continue;

            // BattleGoldHudUI가 관리하는 텍스트는 코루틴 애니메이션이 담당합니다.
            if (target.GetComponentInParent<BattleGoldHudUI>(true) != null)
                continue;

            if (!IsGoldTextCandidate(target.transform))
                continue;

            target.text = text;
        }
    }

    private static bool IsGoldTextCandidate(Transform target)
    {
        if (target == null)
            return false;

        string targetName = target.name.ToLowerInvariant();

        if (targetName.Contains("goldtext") || targetName.Contains("remnanttext"))
            return true;

        Transform current = target;

        while (current != null)
        {
            string name = current.name.ToLowerInvariant();

            if (name.Contains("goldhub") || name.Contains("goldhud") || name.Contains("remnanthub") || name.Contains("remnanthud"))
                return true;

            current = current.parent;
        }

        return false;
    }

    private static int GetRemnantValue()
    {
        int remnant = 0;

        if (DataManager.Instance != null && DataManager.Instance.BattleRuntimeStore != null)
        {
            BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.Get();

            if (runtime != null)
                remnant = Mathf.Max(0, runtime.Remnant);
        }

        return remnant;
    }
}
