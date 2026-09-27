using System.Linq;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class ManualBattleMapTemplateTests
{
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
}
