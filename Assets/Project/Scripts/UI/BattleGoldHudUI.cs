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

        goldText.text = FormatRemnantText();
    }

    private string FormatRemnantText()
    {
        string value = GetRemnantText();

        if (string.IsNullOrWhiteSpace(value))
            value = emptyText;

        if (string.IsNullOrWhiteSpace(format))
            return value;

        try
        {
            return string.Format(format, value);
        }
        catch (System.FormatException)
        {
            return value;
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
        string text = GetRemnantText();
        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text target = texts[i];

            if (target == null)
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

    private static string GetRemnantText()
    {
        int remnant = 0;

        if (DataManager.Instance != null && DataManager.Instance.BattleRuntimeStore != null)
        {
            BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.Get();

            if (runtime != null)
                remnant = Mathf.Max(0, runtime.Remnant);
        }

        return remnant.ToString();
    }
}
