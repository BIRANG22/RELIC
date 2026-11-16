using Relic.Gameplay.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitStatusEffectTooltipItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    public void BindFallbackReferences(Image icon, TMP_Text title, TMP_Text description)
    {
        iconImage = icon;
        titleText = title;
        descriptionText = description;
    }

    public void BindFallbackReferences(Image background, Image icon, TMP_Text title, TMP_Text description)
    {
        backgroundImage = background;
        iconImage = icon;
        titleText = title;
        descriptionText = description;
    }

    private void Awake()
    {
        DisableRaycastTargets();
    }

    public void Set(StatusEffectRuntimeData data)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        DisableRaycastTargets();

        EffectMasterData masterData = GetEffectMasterData(data.EffectId);

        if (iconImage != null)
        {
            iconImage.sprite = GetIcon(data.EffectId);
            iconImage.color = GetEffectIconColor(data.EffectId);
            iconImage.enabled = iconImage.sprite != null;
            iconImage.raycastTarget = false;
        }

        if (titleText != null)
        {
            if (masterData != null && !string.IsNullOrWhiteSpace(masterData.Name))
                titleText.text = GameDataLocalization.EffectName(masterData);
            else
                titleText.text = data.EffectId;
        }

        if (descriptionText != null)
        {
            if (masterData != null && !string.IsNullOrWhiteSpace(masterData.ToolTip))
            {
                string tooltip = GameDataLocalization.EffectTooltip(masterData);
                descriptionText.text = MonsterInnateEffectDescriptionFormatter.Format(
                    tooltip,
                    data.EffectId,
                    DataManager.Instance?.EffectDatabase,
                    DataManager.Instance?.GridEffectDatabase);
            }
            else
                descriptionText.text = string.Empty;
        }
    }

    // Grid effects referenced by monster innate descriptions share this item view.
    public void SetGridEffect(GridEffectData data)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        DisableRaycastTargets();
        if (iconImage != null)
        {
            // Slime (GR_Residue) shares the Trail status effect icon.
            Sprite gridIcon = data.GridEffectID == "GR_Residue" ? GetIcon("E_Trail") : null;
            iconImage.sprite = gridIcon;
            // Grid effects are not beneficial or harmful. Reset pooled icon tint.
            iconImage.color = Color.white;
            iconImage.enabled = gridIcon != null;
            iconImage.raycastTarget = false;
        }
        if (titleText != null)
            titleText.text = GameDataLocalization.GridEffectName(data);
        if (descriptionText != null)
        {
            string description = GameDataLocalization.GridEffectTooltip(data) ?? string.Empty;
            description = description.Replace("{ValueRate1}", data.ValueRate.ToString())
                .Replace("{ValueRate}", data.ValueRate.ToString())
                .Replace("{Duration}", Mathf.Max(0, data.Duration).ToString());
            descriptionText.text = SkillDescriptionFormatter.ReplaceEffectTokens(description);
        }
    }

    private void DisableRaycastTargets()
    {
        if (backgroundImage != null)
            backgroundImage.raycastTarget = false;

        if (iconImage != null)
            iconImage.raycastTarget = false;

        if (titleText != null)
            titleText.raycastTarget = false;

        if (descriptionText != null)
            descriptionText.raycastTarget = false;
    }

    private EffectMasterData GetEffectMasterData(string effectId)
    {
        if (DataManager.Instance == null || DataManager.Instance.EffectDatabase == null)
            return null;

        DataManager.Instance.EffectDatabase.TryGet(effectId, out EffectMasterData masterData);
        return masterData;
    }

    private Color GetEffectIconColor(string effectId)
    {
        DataManager manager = DataManager.Instance;
        if (manager == null || manager.StatusEffectIconDatabase == null)
            return Color.white;

        return manager.StatusEffectIconDatabase.GetIconColor(
            effectId, manager.EffectDatabase, false);
    }

    private Sprite GetIcon(string effectId)
    {
        if (DataManager.Instance == null || DataManager.Instance.StatusEffectIconDatabase == null)
            return null;

        if (DataManager.Instance.StatusEffectIconDatabase.TryGetIcon(effectId, out Sprite icon))
            return icon;

        return null;
    }
}

public static class MonsterInnateEffectDescriptionFormatter
{
    private readonly struct Reference
    {
        public Reference(string id, string name, bool effect)
        {
            Id = id;
            Name = name;
            IsEffect = effect;
        }

        public string Id { get; }
        public string Name { get; }
        public bool IsEffect { get; }
    }

    public static string Format(
        string source,
        string currentEffectId,
        EffectDatabase effectDatabase,
        GridEffectDatabase gridEffectDatabase)
    {
        if (string.IsNullOrWhiteSpace(source))
            return source ?? string.Empty;

        List<Reference> references = BuildReferences(
            currentEffectId, effectDatabase, gridEffectDatabase);
        if (references.Count == 0)
            return source;

        Dictionary<string, Reference> byName = references.ToDictionary(
            value => value.Name, StringComparer.Ordinal);
        string pattern = string.Join("|", references.Select(value => Regex.Escape(value.Name)));
        return Regex.Replace(
            source,
            pattern,
            match =>
            {
                Reference reference = byName[match.Value];
                return reference.IsEffect
                    ? $"<link=\"effecticon:{reference.Id}\"><u>{reference.Name}</u></link>"
                    : $"<link=\"gridicon:{reference.Id}\"><u>{reference.Name}</u></link>";
            },
            RegexOptions.CultureInvariant);
    }

    private static List<Reference> BuildReferences(
        string currentEffectId,
        EffectDatabase effectDatabase,
        GridEffectDatabase gridEffectDatabase)
    {
        Dictionary<string, Reference> byName = new(StringComparer.Ordinal);
        if (effectDatabase != null)
        {
            IEnumerable<EffectMasterData> effects = effectDatabase.All.Values
                .Where(value => value != null)
                .OrderByDescending(value => value.Mark)
                .ThenBy(value => value.EffectId, StringComparer.Ordinal);
            foreach (EffectMasterData effect in effects)
            {
                if (string.Equals(effect.EffectId, currentEffectId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(effect.EffectId, "E_Strike", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(effect.EffectId, "E_Move", StringComparison.OrdinalIgnoreCase))
                    continue;
                string name = GameDataLocalization.EffectName(effect)?.Trim();
                if (!string.IsNullOrEmpty(name) && !byName.ContainsKey(name))
                    byName.Add(name, new Reference(effect.EffectId, name, true));
            }
        }

        if (gridEffectDatabase != null)
        {
            foreach (GridEffectData grid in gridEffectDatabase.GetAll().Values)
            {
                if (grid == null)
                    continue;
                string name = GameDataLocalization.GridEffectName(grid)?.Trim();
                if (!string.IsNullOrEmpty(name) && !byName.ContainsKey(name))
                    byName.Add(name, new Reference(grid.GridEffectID, name, false));
            }
        }

        return byName.Values
            .OrderByDescending(value => value.Name.Length)
            .ThenBy(value => value.Name, StringComparer.Ordinal)
            .ToList();
    }
}
