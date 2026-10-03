using System.Linq;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEngine;

public sealed class ManualBattleMapTemplateTests
{
    private const string DemoTemplatePath =
        "Assets/Project/Data/MapTemplates/Test Manual Battle Map Template Demo.asset";

    [Test]
    public void ResolveTemplate_WhenDemoBattle_SelectsDemoTemplate()
    {
        ManualBattleMapTemplate regularTemplate = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();
        ManualBattleMapTemplate demoTemplate = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();

        try
        {
            ManualBattleMapTemplate selected = BattleMapTemplateSelection.Resolve(
                new BattleRuntimeData { IsDemoBattle = true },
                regularTemplate,
                demoTemplate);

            Assert.That(selected, Is.SameAs(demoTemplate));
        }
        finally
        {
            Object.DestroyImmediate(regularTemplate);
            Object.DestroyImmediate(demoTemplate);
        }
    }

    [Test]
    public void ResolveTemplate_WhenRegularBattle_SelectsRegularTemplate()
    {
        ManualBattleMapTemplate regularTemplate = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();
        ManualBattleMapTemplate demoTemplate = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();

        try
        {
            ManualBattleMapTemplate selected = BattleMapTemplateSelection.Resolve(
                new BattleRuntimeData(),
                regularTemplate,
                demoTemplate);

            Assert.That(selected, Is.SameAs(regularTemplate));
        }
        finally
        {
            Object.DestroyImmediate(regularTemplate);
            Object.DestroyImmediate(demoTemplate);
        }
    }

    [Test]
    public void Nodes_DefaultTemplate_UsesLayer13AsBoss()
    {
        ManualBattleMapTemplate template = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();

        try
        {
            var nodes = template.Nodes;

            Assert.That(nodes.Max(node => node.LayerIndex), Is.EqualTo(13));
            Assert.That(nodes.Single(node => node.Type == "Boss").NodeIndex, Is.EqualTo(49));
        }
        finally
        {
            Object.DestroyImmediate(template);
        }
    }

    [Test]
    public void Nodes_WhenBossLayerIs11_EndsAtLayer11WithReservedBossNodeIndex()
    {
        ManualBattleMapTemplate template = ScriptableObject.CreateInstance<ManualBattleMapTemplate>();

        try
        {
            template.BossLayerIndex = 11;

            var nodes = template.Nodes;

            Assert.That(nodes.Max(node => node.LayerIndex), Is.EqualTo(11));
            Assert.That(nodes.Any(node => node.LayerIndex == 12 || node.LayerIndex == 13), Is.False);
            Assert.That(nodes.Single(node => node.Type == "Boss").NodeIndex, Is.EqualTo(41));
        }
        finally
        {
            Object.DestroyImmediate(template);
        }
    }

    [Test]
    public void DemoTemplate_AddsCommonBattleBetweenSpecialAndShopInOneCenteredLine()
    {
        ManualBattleMapTemplate template =
            AssetDatabase.LoadAssetAtPath<ManualBattleMapTemplate>(DemoTemplatePath);

        Assert.That(template, Is.Not.Null);

        var nodes = template.Nodes.OrderBy(node => node.LayerIndex).ToList();

        Assert.That(nodes.Select(node => node.LayerIndex),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
        Assert.That(nodes.Select(node => node.Type),
            Is.EqualTo(new[] { "Special", "Common", "Special", "Common", "Shop", "Rest", "Boss" }));
        Assert.That(nodes.Single(node => node.LayerIndex == 3).MapIdOverride, Is.Empty,
            "The inserted Common node must select a regular battle map without an override.");

        foreach (var node in nodes.Where(node => node.LayerIndex > 0 && node.LayerIndex < 6))
        {
            Assert.That(node.UseCustomPosition, Is.True,
                $"Layer {node.LayerIndex} must use its centered demo position.");
            Assert.That(node.CustomPosition,
                Is.EqualTo(new Vector2(node.LayerIndex * BattleMapLayoutUtility.LayerGap, 0f)));
        }
    }
}
