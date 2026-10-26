using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;

public class BattleGoldHudUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text addAndUseText;

    [Header("Display")]
    [SerializeField] private string emptyText = "0";
    [SerializeField] private string format = "{0}";

    [Header("숫자 변화 연출")]
    [Tooltip("현재 숫자에서 최종 숫자까지 변하는 전체 시간입니다. 변화량과 관계없이 같은 시간 안에 도착합니다.")]
    [Min(0f)]
    [SerializeField] private float numberChangeDuration = 0.2f;

    [Header("AddandUse 등장 연출")]
    [SerializeField, Min(0f)] private float addAndUseIntroDuration = 0.3f;
    [SerializeField, Min(1f)] private float addAndUseIntroPeakScale = 1.2f;

    private Coroutine numberChangeCoroutine;
    private Vector3 addAndUseBaseScale = Vector3.one;
    private bool hasAddAndUseBaseScale;
    private int displayedValue;
    private bool hasDisplayedValue;
    private int animationTargetValue;
    private bool isAnimatingNumber;

    private void Awake()
    {
        AutoBind();
        CacheAddAndUseBaseScale();
        HideAddAndUse();
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
        HideAddAndUse();
        hasDisplayedValue = false;
    }

    private void AutoBind()
    {
        if (goldText == null)
        {
            Transform t = transform.Find("GoldText");
            if (t != null)
                goldText = t.GetComponent<TMP_Text>();
        }

        if (goldText == null)
            goldText = GetComponentInChildren<TMP_Text>(true);

        if (addAndUseText == null)
        {
            Transform t = transform.Find("AddandUse");
            if (t != null)
                addAndUseText = t.GetComponent<TMP_Text>();
        }

        CacheAddAndUseBaseScale();
    }

    private void CacheAddAndUseBaseScale()
    {
        if (hasAddAndUseBaseScale || addAndUseText == null)
            return;

        addAndUseBaseScale = addAndUseText.transform.localScale;
        hasAddAndUseBaseScale = true;
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
            HideAddAndUse();
            return;
        }

        if (isAnimatingNumber && animationTargetValue == targetValue)
            return;

        if (displayedValue == targetValue)
        {
            StopNumberChangeCoroutine();
            ApplyDisplayedValue();
            HideAddAndUse();
            return;
        }

        StopNumberChangeCoroutine();
        animationTargetValue = targetValue;
        isAnimatingNumber = true;
        numberChangeCoroutine = StartCoroutine(AnimateNumberChange(targetValue));
    }

    private IEnumerator AnimateNumberChange(int targetValue)
    {
        int startValue = displayedValue;
        int delta = targetValue - startValue;
        string sign = delta > 0 ? "+" : "-";
        ShowAddAndUse(sign, Mathf.Abs(delta));

        // 재화 수치 변경 전에 AddandUse를 먼저 등장시킨다.
        // 이 동안 GoldText와 AddandUse에 표시된 수치는 변하지 않는다.
        if (addAndUseText != null)
        {
            CacheAddAndUseBaseScale();
            float introElapsed = 0f;
            while (introElapsed < addAndUseIntroDuration)
            {
                introElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(introElapsed / addAndUseIntroDuration);
                float scale = t < 0.5f
                    ? Mathf.Lerp(1f, addAndUseIntroPeakScale, t * 2f)
                    : Mathf.Lerp(addAndUseIntroPeakScale, 1f, (t - 0.5f) * 2f);
                addAndUseText.transform.localScale = addAndUseBaseScale * scale;
                yield return null;
            }
            addAndUseText.transform.localScale = addAndUseBaseScale;
        }

        if (numberChangeDuration <= 0f)
        {
            displayedValue = targetValue;
            ApplyDisplayedValue();
            ShowAddAndUse(sign, 0);
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < numberChangeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / numberChangeDuration);

                displayedValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, progress));
                ApplyDisplayedValue();

                // GoldText에 실제로 반영된 값만큼 차감하여 남은 획득/소모량을 표시한다.
                ShowAddAndUse(sign, Mathf.Abs(targetValue - displayedValue));
                yield return null;
            }
        }

        displayedValue = targetValue;
        ApplyDisplayedValue();
        ShowAddAndUse(sign, 0);

        // +0/-0을 마지막에 잠깐 보여준 후 AddandUse만 비활성화한다.
        yield return new WaitForSecondsRealtime(0.12f);
        HideAddAndUse();
        isAnimatingNumber = false;
        numberChangeCoroutine = null;
    }

    private void ShowAddAndUse(string sign, int remaining)
    {
        if (addAndUseText == null)
            return;

        addAndUseText.text = sign + remaining;
        if (!addAndUseText.gameObject.activeSelf)
            addAndUseText.gameObject.SetActive(true);
    }

    private void HideAddAndUse()
    {
        if (addAndUseText == null)
            return;

        if (hasAddAndUseBaseScale)
            addAndUseText.transform.localScale = addAndUseBaseScale;

        if (addAndUseText.gameObject.activeSelf)
            addAndUseText.gameObject.SetActive(false);
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
        isAnimatingNumber = false;
        if (addAndUseText != null && hasAddAndUseBaseScale)
            addAndUseText.transform.localScale = addAndUseBaseScale;
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
