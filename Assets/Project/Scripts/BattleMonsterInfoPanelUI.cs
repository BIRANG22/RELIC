using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BattleCharacterPanel 안에서 선택된 몬스터의 핵심 정보를 표시합니다.
/// 모든 UI 참조는 Inspector에서 직접 연결합니다.
/// </summary>
public class BattleMonsterInfoPanelUI : MonoBehaviour
{
    [Header("Monster Info")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;

    [Header("HP Info")]
    [SerializeField] private Image hpIconImage;
    [SerializeField] private TMP_Text hpText;

    [Header("Armor Info")]
    [SerializeField] private Image armorIconImage;
    [SerializeField] private TMP_Text armorText;

    [Header("Status Effects")]
    [SerializeField] private RectTransform statusEffectListRoot;
    [SerializeField] private StatusEffectIcon statusEffectIconPrefab;

    [Header("Portrait")]
    [Tooltip("MonsterIconDatabase에서 일반 Icon을 찾지 못했을 때만 월드 스프라이트를 예비 초상화로 사용합니다.")]
    [SerializeField] private bool useWorldSpriteAsPortraitFallback = true;

    private readonly List<StatusEffectIcon> spawnedStatusEffectIcons = new();
    private MonsterUnit boundMonster;
    private MonsterRuntimeData boundRuntime;
    private int lastHp = int.MinValue;
    private int lastMaxHp = int.MinValue;
    private int lastArmor = int.MinValue;
    private int lastStatusHash = int.MinValue;

    private void OnEnable()
    {
        Refresh(true);
    }

    private void OnDisable()
    {
        ClearStatusEffects();
        boundMonster = null;
        boundRuntime = null;
        ResetCachedValues();
    }

    private void Update()
    {
        Refresh(false);
    }

    public void ConfigureStatusEffectPrefab(StatusEffectIcon prefab)
    {
        if (prefab != null)
            statusEffectIconPrefab = prefab;
    }

    public void Bind(MonsterUnit monster)
    {
        boundMonster = monster;
        boundRuntime = monster != null ? monster.RuntimeData : null;
        ResetCachedValues();
        Refresh(true);
    }

    public void Clear()
    {
        boundMonster = null;
        boundRuntime = null;
        ResetCachedValues();

        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        if (nameText != null)
            nameText.text = string.Empty;

        if (hpText != null)
            hpText.text = string.Empty;

        if (armorText != null)
            armorText.text = string.Empty;

        ClearStatusEffects();
    }

    private void Refresh(bool force)
    {
        if (boundMonster != null)
            boundRuntime = boundMonster.RuntimeData;

        if (boundRuntime == null)
            return;

        if (force)
        {
            if (nameText != null)
                nameText.text = boundRuntime.GetDisplayName();

            RefreshPortrait();
        }

        if (force || lastHp != boundRuntime.CurrentHP || lastMaxHp != boundRuntime.MaxHP)
        {
            if (hpText != null)
                hpText.text = $"{Mathf.Max(0, boundRuntime.CurrentHP)} / {Mathf.Max(0, boundRuntime.MaxHP)}";

            lastHp = boundRuntime.CurrentHP;
            lastMaxHp = boundRuntime.MaxHP;
        }

        if (force || lastArmor != boundRuntime.CurrentShield)
        {
            if (armorText != null)
                armorText.text = Mathf.Max(0, boundRuntime.CurrentShield).ToString();

            lastArmor = boundRuntime.CurrentShield;
        }

        int statusHash = CalculateStatusHash(boundRuntime.StatusEffects);
        if (force || statusHash != lastStatusHash)
        {
            RebuildStatusEffects(boundRuntime.StatusEffects);
            lastStatusHash = statusHash;
        }
    }

    private void RefreshPortrait()
    {
        if (portraitImage == null)
            return;

        Sprite portrait = null;

        if (boundRuntime != null &&
            !string.IsNullOrWhiteSpace(boundRuntime.MonsterId) &&
            DataManager.Instance != null &&
            DataManager.Instance.MonsterIconDatabase != null)
        {
            DataManager.Instance.MonsterIconDatabase.TryGetIcon(
                boundRuntime.MonsterId,
                out portrait
            );
        }

        if (portrait == null && useWorldSpriteAsPortraitFallback && boundMonster != null)
        {
            SpriteRenderer renderer = boundMonster.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null)
                portrait = renderer.sprite;
        }

        portraitImage.sprite = portrait;
        portraitImage.enabled = portrait != null;
        portraitImage.preserveAspect = true;
    }

    private void RebuildStatusEffects(List<StatusEffectRuntimeData> statusEffects)
    {
        ClearStatusEffects();

        if (statusEffectListRoot == null || statusEffectIconPrefab == null || statusEffects == null)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData statusEffect = statusEffects[i];
            if (statusEffect == null || !statusEffect.IsValid())
                continue;

            StatusEffectIcon icon = Instantiate(statusEffectIconPrefab, statusEffectListRoot);
            icon.gameObject.name = $"StatusEffect_{statusEffect.EffectId}";
            icon.transform.localScale = Vector3.one;
            icon.Set(statusEffect);
            spawnedStatusEffectIcons.Add(icon);
        }
    }

    private void ClearStatusEffects()
    {
        for (int i = 0; i < spawnedStatusEffectIcons.Count; i++)
        {
            StatusEffectIcon icon = spawnedStatusEffectIcons[i];
            if (icon != null)
                Destroy(icon.gameObject);
        }

        spawnedStatusEffectIcons.Clear();
    }

    private static int CalculateStatusHash(List<StatusEffectRuntimeData> statusEffects)
    {
        if (statusEffects == null)
            return 0;

        unchecked
        {
            int hash = 17;
            for (int i = 0; i < statusEffects.Count; i++)
            {
                StatusEffectRuntimeData status = statusEffects[i];
                if (status == null)
                    continue;

                hash = hash * 31 + (status.EffectId != null ? status.EffectId.GetHashCode() : 0);
                hash = hash * 31 + status.Stack;
                hash = hash * 31 + status.TurnCount;
            }

            return hash;
        }
    }

    private void ResetCachedValues()
    {
        lastHp = int.MinValue;
        lastMaxHp = int.MinValue;
        lastArmor = int.MinValue;
        lastStatusHash = int.MinValue;
    }
}
