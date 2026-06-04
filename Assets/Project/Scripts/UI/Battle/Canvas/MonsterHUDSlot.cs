using Relic.Gameplay.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterHUDSlot : MonoBehaviour
{
    [Header("Basic")]
    [SerializeField] private Image portraitImage;
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

    private MonsterRuntimeData boundRuntime;

    public void Bind(MonsterRuntimeData runtimeData)
    {
        boundRuntime = runtimeData;
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

        if (portraitImage != null)
        {
            portraitImage.sprite = GetMonsterIcon(boundRuntime.MonsterId);
            portraitImage.enabled = portraitImage.sprite != null;
        }

        if (nameText != null)
            nameText.text = boundRuntime.Name;

        RefreshHp(boundRuntime.CurrentHp, boundRuntime.MaxHp);
        RefreshShield(boundRuntime.CurrentShield, boundRuntime.MaxHp);
        RefreshStatusEffects(boundRuntime.StatusEffects);
    }

    private void RefreshHp(int currentHp, int maxHp)
    {
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        if (hpFill != null)
            hpFill.fillAmount = maxHp > 0 ? (float)currentHp / maxHp : 0f;

        if (hpValueText != null)
            hpValueText.text = $"{currentHp} / {maxHp}";
    }

    private void RefreshShield(int shield, int maxHp)
    {
        shield = Mathf.Max(0, shield);

        if (shieldFill != null)
        {
            shieldFill.gameObject.SetActive(shield > 0);
            shieldFill.fillAmount = maxHp > 0 ? (float)shield / maxHp : 0f;
        }

        if (shieldValueText != null)
        {
            shieldValueText.gameObject.SetActive(shield > 0);
            shieldValueText.text = shield.ToString();
        }
    }

    private void RefreshStatusEffects(List<StatusEffectRuntimeData> statusEffects)
    {
        if (statusIconRoot == null || statusIconPrefab == null)
            return;

        for (int i = statusIconRoot.childCount - 1; i >= 0; i--)
            Destroy(statusIconRoot.GetChild(i).gameObject);

        if (statusEffects == null)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i] == null)
                continue;

            StatusEffectIcon icon = Instantiate(statusIconPrefab, statusIconRoot);
            icon.Set(statusEffects[i]);
        }
    }

    private Sprite GetMonsterIcon(string monsterId)
    {
        // 나중에 MonsterIconDatabase 있으면 여기서 연결
        return null;
    }

    private void Clear()
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        if (nameText != null)
            nameText.text = "";

        RefreshHp(0, 1);
        RefreshShield(0, 1);
        RefreshStatusEffects(null);
    }
}