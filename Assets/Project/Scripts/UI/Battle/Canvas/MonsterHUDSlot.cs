using Relic.Gameplay.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterHUDSlot : MonoBehaviour
{
    [Header("Basic")]
    [SerializeField] private TMP_Text nameText;

    [Header("HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text hpValueText;

    [Header("Shield")]
    [SerializeField] private Image shieldFill;
    [SerializeField] private TMP_Text shieldValueText;

    [Header("Status Effects")]
    [SerializeField] private Transform statusIconRoot;
    [SerializeField] private StatusEffectIcon statusIconPrefab;
    [SerializeField] private float statusEffectIconSpacing = 4f;

    private MonsterRuntimeData boundRuntime;
    private readonly List<StatusEffectIcon> spawnedStatusIcons = new();

    private void Awake()
    {
        ApplyStatusEffectParentLayout();
    }

    public void Bind(MonsterRuntimeData runtimeData)
    {
        boundRuntime = runtimeData;
        ApplyStatusEffectParentLayout();

        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        Refresh();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        if (nameText != null)
            nameText.text = boundRuntime.Name;

        RefreshBar(hpFill, hpValueText, boundRuntime.CurrentHp, boundRuntime.MaxHp);
        //RefreshShield(boundRuntime.CurrentShield, boundRuntime.MaxHp);
        RefreshStatusEffects(boundRuntime.StatusEffects);
    }

    private void RefreshBar(Image fill, TMP_Text valueText, int current, int max)
    {
        max = Mathf.Max(1, max);
        current = Mathf.Clamp(current, 0, max);

        if (fill != null)
            fill.fillAmount = (float)current / max;

        if (valueText != null)
            valueText.text = current.ToString();
    }

    private void RefreshShield(int shield, int maxHp)
    {
        shield = Mathf.Max(0, shield);
        maxHp = Mathf.Max(1, maxHp);

        if (shieldFill != null)
        {
            shieldFill.gameObject.SetActive(shield > 0);
            shieldFill.fillAmount = (float)shield / maxHp;
        }

        if (shieldValueText != null)
        {
            shieldValueText.gameObject.SetActive(shield > 0);
            shieldValueText.text = shield.ToString();
        }
    }

    private void RefreshStatusEffects(List<StatusEffectRuntimeData> statusEffects)
    {
        ClearStatusEffectIcons();
        ApplyStatusEffectParentLayout();

        if (statusIconRoot == null || statusIconPrefab == null)
            return;

        if (statusEffects == null)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i] == null)
                continue;

            StatusEffectIcon icon = Instantiate(statusIconPrefab, statusIconRoot);

            // 여기서 StatusEffectIcon 내부가 EffectId로 DB 조회해서 Sprite 세팅
            icon.Set(statusEffects[i]);

            spawnedStatusIcons.Add(icon);
        }
    }

    private void ClearStatusEffectIcons()
    {
        for (int i = spawnedStatusIcons.Count - 1; i >= 0; i--)
        {
            if (spawnedStatusIcons[i] != null)
                Destroy(spawnedStatusIcons[i].gameObject);
        }

        spawnedStatusIcons.Clear();

        if (statusIconRoot == null)
            return;

        for (int i = statusIconRoot.childCount - 1; i >= 0; i--)
            Destroy(statusIconRoot.GetChild(i).gameObject);
    }

    private void ApplyStatusEffectParentLayout()
    {
        if (statusIconRoot == null)
            return;

        HorizontalLayoutGroup layout = statusIconRoot.GetComponent<HorizontalLayoutGroup>();

        if (layout != null)
            layout.spacing = statusEffectIconSpacing;
    }

    private void Clear()
    {
        if (nameText != null)
            nameText.text = "";

        RefreshBar(hpFill, hpValueText, 0, 1);
        RefreshShield(0, 1);
        ClearStatusEffectIcons();
    }
}