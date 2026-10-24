using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class ExplorationResultPanelLayoutTests
{
    [Test]
    public void VisitedNodeSummary_UsesEnteredNodesInsteadOfClearedNodes()
    {
        MapRuntimeData runtime = new()
        {
            VisitedMapIds = new List<string> { "1", "2", "3", "4" },
            ClearedMapIds = new List<string> { "1" },
            GeneratedNodes = new List<GeneratedMapNodeData>
            {
                new() { NodeIndex = 1, Type = "Battle" },
                new() { NodeIndex = 2, Type = "Common" },
                new() { NodeIndex = 3, Type = "Elite" },
                new() { NodeIndex = 4, Type = "Event" }
            }
        };

        ExplorationNodeVisitSummary summary = ExplorationNodeVisitSummary.Count(runtime);

        Assert.That(summary.NormalBattleCount, Is.EqualTo(2));
        Assert.That(summary.EliteBattleCount, Is.EqualTo(1));
        Assert.That(summary.EventCount, Is.EqualTo(1));
    }

    [Test]
    public void VisitedNodeSummary_IgnoresDuplicateInvalidAndUnsupportedNodes()
    {
        MapRuntimeData runtime = new()
        {
            VisitedMapIds = new List<string> { "1", "1", "missing", "99", "2", "3" },
            GeneratedNodes = new List<GeneratedMapNodeData>
            {
                new() { NodeIndex = 1, Type = "Elite" },
                new() { NodeIndex = 2, Type = "Special" },
                new() { NodeIndex = 3, Type = "Rest" }
            }
        };

        ExplorationNodeVisitSummary summary = ExplorationNodeVisitSummary.Count(runtime);

        Assert.That(summary.NormalBattleCount, Is.Zero);
        Assert.That(summary.EliteBattleCount, Is.EqualTo(1));
        Assert.That(summary.EventCount, Is.EqualTo(1));
    }

    [TestCase(0, "0")]
    [TestCase(10, "10")]
    public void ErosionLevel_UsesScoreSumLabel(int score, string expected)
    {
        Assert.That(ExplorationResultPanelUI.FormatErosionLevel(score), Is.EqualTo(expected));
    }

    [Test]
    public void SelectedErosionScore_UsesLobbyDifficultyScores_NotAccumulatedBattleErosion()
    {
        ErosionDatabase database = new();
        database.Initialize(new[]
        {
            new ErosionData { DifficultyId = "Erosion_A_1", Score = 3, Tier = 1 },
            new ErosionData { DifficultyId = "Erosion_B_2", Score = 7, Tier = 2 }
        });

        int score = ExplorationResultPanelUI.CalculateSelectedErosionScore(
            new List<string> { "Erosion_A_1", "missing", "Erosion_B_2" },
            database);

        Assert.That(score, Is.EqualTo(10));
    }

    [TestCase(1, "I")]
    [TestCase(2, "II")]
    [TestCase(3, "III")]
    [TestCase(0, "")]
    public void ErosionTierBadge_UsesRomanNumerals(int tier, string expected)
    {
        Assert.That(ExplorationResultPanelUI.FormatErosionTier(tier), Is.EqualTo(expected));
    }

    [Test]
    public void BattleScene_UsesDedicatedSummaryAndAcquiredItemRoots()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        Assert.That(scene, Does.Contain("m_Name: ExplorationReportSummaryGroup"));
        Assert.That(scene, Does.Contain("m_Name: ExplorationReportAcquiredItemSlots"));
        Assert.That(scene, Does.Contain("m_Name: ExplorationReportAcquiredItemsDivider"));
        Assert.That(scene, Does.Contain("m_Name: ExplorationReportAcquiredItemsLabel"));
        Assert.That(scene, Does.Contain("m_Name: ExplorationReportErosionIcon"));
        Assert.That(scene, Does.Contain("m_Name: ExplorationReportErosionLevel"));
        Assert.That(scene, Does.Contain("m_Name: ExplorationReportSelectedErosionSlots"));
        Assert.That(scene, Does.Contain(
            "m_Sprite: {fileID: 21300000, guid: 5927fbacdef890243827d3e11cca58fb, type: 3}"));
    }

    [Test]
    public void ResultPanel_ExposesRuntimeIconLayoutInInspector()
    {
        string panelSource = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");
        string rowSource = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs");

        Assert.That(panelSource, Does.Contain("hardModeSlotScale"));
        Assert.That(panelSource, Does.Contain("acquiredItemSlotScale"));
        Assert.That(panelSource, Does.Contain("hardModeSlotColumns"));
        Assert.That(rowSource, Does.Contain("relicSlotScale"));
        Assert.That(rowSource, Does.Contain("skillSlotScale"));
        Assert.That(rowSource, Does.Contain("relicSlotColumns"));
    }

    [Test]
    public void BattleScene_DynamicSummaryValuesAreNotOverwrittenByStaticLocalization()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        AssertDynamicCountTextIsLocalizationIgnored(scene, "ExplorationReportNormalBattleCount");
        AssertDynamicCountTextIsLocalizationIgnored(scene, "ExplorationReportEliteBattleCount");
        AssertDynamicCountTextIsLocalizationIgnored(scene, "ExplorationReportEventCount");
    }

    [Test]
    public void BattleScene_ShowsOnlyIconAndValueForRedDustium()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        string iconObject = ExtractGameObject(scene, "ExplorationReportRedDustiumIcon");
        string valueObject = ExtractGameObject(scene, "ExplorationReportRedDustium");
        Assert.That(iconObject, Does.Contain("m_IsActive: 1"));
        Assert.That(valueObject, Does.Contain("m_IsActive: 1"));

        string groupRect = ExtractDocument(scene, "258994648");
        string iconRect = ExtractDocument(scene, "940000000000000014");
        string valueRect = ExtractDocument(scene, "940000000000000018");
        Assert.That(groupRect, Does.Contain("m_SizeDelta: {x: 250, y: 80}"));
        Assert.That(iconRect, Does.Contain("m_Father: {fileID: 258994648}"));
        Assert.That(iconRect, Does.Contain("m_AnchoredPosition: {x: -65, y: 0}"));
        Assert.That(valueRect, Does.Contain("m_Father: {fileID: 258994648}"));
        Assert.That(valueRect, Does.Contain("m_AnchoredPosition: {x: 50, y: 0}"));

        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");
        Assert.That(source, Does.Contain("FormatRedDustium"));
    }

    [Test]
    public void BattleScene_PlacesAllExperienceBarsAbovePortraits()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        Assert.That(Count(scene, "m_AnchoredPosition: {x: 165, y: -25}"), Is.EqualTo(3));
        Assert.That(Count(scene, "m_AnchoredPosition: {x: 120, y: -105}"), Is.EqualTo(3));
    }

    [Test]
    public void BattleScene_ResultPanelRectTransformHierarchyIsBidirectionallyConsistent()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");
        Dictionary<string, RectTransformRecord> rects = ParseRectTransforms(scene);

        foreach (KeyValuePair<string, RectTransformRecord> pair in rects)
        {
            string childId = pair.Key;
            RectTransformRecord child = pair.Value;
            if (!childId.StartsWith("940000000000000", System.StringComparison.Ordinal) ||
                child.ParentId == "0" ||
                !rects.TryGetValue(child.ParentId, out RectTransformRecord parent))
            {
                continue;
            }

            Assert.That(
                parent.ChildIds,
                Does.Contain(childId),
                $"RectTransform {childId} points to parent {child.ParentId}, but the parent does not contain it.");
        }
    }

    [Test]
    public void BattleScene_CharacterCardsUseOneLowerHorizontalBaseline()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        string rows = ExtractDocument(scene, "940000000000000094");
        Assert.That(rows, Does.Contain("m_AnchoredPosition: {x: 150, y: 120}"));

        Assert.That(ExtractDocument(scene, "940000000000000096"), Does.Contain("m_AnchoredPosition: {x: -325, y: 0}"));
        Assert.That(ExtractDocument(scene, "940000000000000148"), Does.Contain("m_AnchoredPosition: {x: 0, y: 0}"));
        Assert.That(ExtractDocument(scene, "940000000000000200"), Does.Contain("m_AnchoredPosition: {x: 325, y: 0}"));
        Assert.That(Count(scene, "m_SizeDelta: {x: 250, y: 360}"), Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void BattleScene_StacksVisitedNodeCountsVertically()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        Assert.That(scene, Does.Contain("m_AnchoredPosition: {x: -600, y: 210}"));
        Assert.That(scene, Does.Contain("m_AnchoredPosition: {x: -600, y: 130}"));
        Assert.That(scene, Does.Contain("m_AnchoredPosition: {x: -600, y: 50}"));
        Assert.That(scene, Does.Contain("m_AnchoredPosition: {x: -390, y: 210}"));
        Assert.That(scene, Does.Contain("m_AnchoredPosition: {x: -390, y: 130}"));
        Assert.That(scene, Does.Contain("m_AnchoredPosition: {x: -390, y: 50}"));
    }

    [Test]
    public void LevelLabel_UsesFinalLevelWithoutLevelUpBanner()
    {
        Assert.That(ExplorationResultCharacterRowUI.FormatLevelLabel(3), Is.EqualTo("LV.03"));
        Assert.That(ExplorationResultCharacterRowUI.FormatLevelLabel(11), Is.EqualTo("LV.11"));
    }

    [Test]
    public void ElapsedSummary_StaysOnOneLineLikeReferenceLayout()
    {
        Assert.That(
            ExplorationResultPanelUI.FormatElapsedSummary(3723f, "소요 시간"),
            Is.EqualTo("소요 시간  1:02:03"));
    }

    [Test]
    public void BattleScene_SeparatesElapsedTimeLabelAndRightAlignedValue()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        string label = ExtractGameObjectWithComponents(scene, "ExplorationReportElapsedTimeLabel");
        string value = ExtractGameObjectWithComponents(scene, "ExplorationReportElapsedTimeCount");

        Assert.That(label, Does.Contain("m_SizeDelta: {x: 207, y: 56}"));
        Assert.That(label, Does.Contain("m_Father: {fileID: 940000000000000256}"));
        Assert.That(label, Does.Contain("m_AnchoredPosition: {x: -600, y: 290}"));
        Assert.That(label, Does.Contain("m_fontSize: 30"));
        Assert.That(label, Does.Contain("m_HorizontalAlignment: 1"));
        Assert.That(value, Does.Contain("m_SizeDelta: {x: 170, y: 56}"));
        Assert.That(value, Does.Contain("m_Father: {fileID: 940000000000000256}"));
        Assert.That(value, Does.Contain("m_AnchoredPosition: {x: -384, y: 290}"));
        Assert.That(value, Does.Contain("m_fontSize: 42"));
        Assert.That(value, Does.Contain("m_HorizontalAlignment: 4"));
    }

    [Test]
    public void CharacterReport_KeepsUnfilledRelicAndSkillSlotsVisible()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs");
        string method = ExtractMethod(source, "private void BindIconSlots(");
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        Assert.That(method, Does.Contain("slot.gameObject.SetActive(true);"));
        Assert.That(method, Does.Contain("slot.SetupDisplayIcon(null);"));
        Assert.That(method, Does.Not.Contain("slot.gameObject.SetActive(filled);"));

        foreach (string componentId in new[]
                 {
                     "940000000000000099",
                     "940000000000000151",
                     "940000000000000203"
                 })
        {
            string row = ExtractDocument(scene, componentId);
            Assert.That(row, Does.Contain("maxRelicSlots: 6"));
            Assert.That(row, Does.Contain("maxSkillSlots: 4"));
            Assert.That(row, Does.Contain("relicSlotScale: 0.36"));
            Assert.That(row, Does.Contain("relicSlotHorizontalSpacing: 40"));
            Assert.That(row, Does.Contain("relicSlotColumns: 6"));
            Assert.That(row, Does.Contain("skillSlotScale: 0.52"));
            Assert.That(row, Does.Contain("skillSlotHorizontalSpacing: 60"));
            Assert.That(row, Does.Contain("skillSlotColumns: 4"));
        }
    }

    [Test]
    public void BattleScene_UsesShorterAndThickerExperienceGauges()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        foreach (string objectName in new[]
                 {
                     "ExplorationReportExpSlider_0",
                     "ExplorationReportExpSlider_1",
                     "ExplorationReportExpSlider_2"
                 })
        {
            string slider = ExtractGameObjectWithComponents(scene, objectName);
            Assert.That(slider, Does.Contain("m_SizeDelta: {x: 220, y: 32}"));
        }
    }

    [Test]
    public void StatisticsOverlay_IsPlacedBelowHoveredIconAndClampedInsidePanel()
    {
        Rect boundary = new(-500f, -300f, 1000f, 600f);
        Vector2 overlaySize = new(300f, 205f);
        Vector2 pivot = new(0.5f, 1f);

        Vector2 centered = ExplorationResultPanelUI.CalculateStatisticsOverlayPosition(
            new Vector2(100f, 80f),
            boundary,
            overlaySize,
            pivot,
            12f);
        Assert.That(centered, Is.EqualTo(new Vector2(100f, 68f)));

        Vector2 rightEdge = ExplorationResultPanelUI.CalculateStatisticsOverlayPosition(
            new Vector2(490f, 80f),
            boundary,
            overlaySize,
            pivot,
            12f);
        Assert.That(rightEdge.x, Is.EqualTo(350f));
        Assert.That(rightEdge.y, Is.EqualTo(68f));

        Vector2 anchored = ExplorationResultPanelUI.ConvertBoundaryLocalToAnchoredPosition(
            centered,
            boundary,
            new Vector2(0f, 1f));
        Assert.That(anchored, Is.EqualTo(new Vector2(600f, -232f)));
    }

    [Test]
    public void CharacterInformationSupport_HoverAndClick()
    {
        string rowSource = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs");
        string targetSource = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultInformationHoverTarget.cs");

        Assert.That(rowSource, Does.Not.Contain("IPointerEnterHandler"));
        Assert.That(rowSource, Does.Not.Contain("IPointerExitHandler"));
        Assert.That(rowSource, Does.Not.Contain("IPointerClickHandler"));
        Assert.That(targetSource, Does.Contain("IPointerEnterHandler"));
        Assert.That(targetSource, Does.Contain("IPointerExitHandler"));
        Assert.That(targetSource, Does.Contain("IPointerClickHandler"));
    }

    [Test]
    public void CharacterReport_UsesMaskedDoubleScalePortraitAndCornerInformationButton()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs");

        Assert.That(Count(scene, "m_Name: ExplorationReportPortraitViewport"), Is.EqualTo(3));
        Assert.That(Count(scene, "m_Script: {fileID: 11500000, guid: 3312d7739989d2b4e91e6319e9a96d76, type: 3}"),
            Is.GreaterThanOrEqualTo(3));
        Assert.That(Count(scene, "m_LocalScale: {x: 2, y: 2, z: 1}"), Is.GreaterThanOrEqualTo(3));
        Assert.That(Count(scene, "m_Name: StatisticsOverlayAnchor"), Is.EqualTo(3));
        Assert.That(Count(scene, "m_Name: InformationGlyph"), Is.EqualTo(3));
        Assert.That(Count(scene, "m_SizeDelta: {x: 40, y: 40}"), Is.GreaterThanOrEqualTo(3));
        Assert.That(Count(scene, "m_Sprite: {fileID: 21300000, guid: b3c1b8145715e654baaaba25ffe89195, type: 3}"),
            Is.GreaterThanOrEqualTo(3));
        Assert.That(source, Does.Contain("statisticsOverlayAnchor"));
        Assert.That(source, Does.Contain("statisticsOverlayAnchor ?? informationHoverTarget"));
    }

    [Test]
    public void ResultPanel_DoesNotResolveErosionSettingsAsBagItems()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");
        string method = ExtractMethod(source, "private void ApplyHardModeSlots(");

        Assert.That(method, Does.Not.Contain("slot.Setup("));
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static string ExtractGameObject(string scene, string objectName)
    {
        string marker = $"  m_Name: {objectName}";
        int nameIndex = scene.IndexOf(marker, System.StringComparison.Ordinal);
        Assert.That(nameIndex, Is.GreaterThanOrEqualTo(0), $"Missing object: {objectName}");

        int start = scene.LastIndexOf("--- !u!1 &", nameIndex, System.StringComparison.Ordinal);
        int end = scene.IndexOf("\n--- !u!", nameIndex, System.StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));
        return scene.Substring(start, end - start);
    }

    private static string ExtractDocument(string scene, string fileId)
    {
        Match match = Regex.Match(
            scene,
            $@"(?ms)^--- !u!\d+ &{fileId}\n.*?(?=^--- !u!|\z)");
        Assert.That(match.Success, Is.True, $"Missing document: {fileId}");
        return match.Value;
    }

    private static string ExtractGameObjectWithComponents(string scene, string objectName)
    {
        string gameObject = ExtractGameObject(scene, objectName);
        System.Text.StringBuilder result = new(gameObject);
        foreach (Match componentMatch in Regex.Matches(
                     gameObject,
                     @"component: \{fileID: (?<id>\d+)\}"))
        {
            result.Append(ExtractDocument(scene, componentMatch.Groups["id"].Value));
        }

        return result.ToString();
    }

    private static void AssertDynamicCountTextIsLocalizationIgnored(string scene, string objectName)
    {
        const string staticLocalizerGuid = "700ed754fb422984990c407c26f0065c";
        const string autoBindingIgnoreGuid = "a6ef87d46a1e4f88b9b4c3f947b86fe2";

        string gameObject = ExtractGameObject(scene, objectName);
        MatchCollection componentMatches = Regex.Matches(gameObject, @"component: \{fileID: (?<id>\d+)\}");
        Assert.That(componentMatches.Count, Is.GreaterThan(0));

        bool hasAutoBindingIgnore = false;
        foreach (Match componentMatch in componentMatches)
        {
            string component = ExtractDocument(scene, componentMatch.Groups["id"].Value);
            Assert.That(component, Does.Not.Contain(staticLocalizerGuid),
                $"{objectName} must not have a static LocalizedTMPText writer.");
            hasAutoBindingIgnore |= component.Contains(autoBindingIgnoreGuid);
        }

        Assert.That(hasAutoBindingIgnore, Is.True,
            $"{objectName} must opt out of runtime localization auto-binding.");
    }

    private static Dictionary<string, RectTransformRecord> ParseRectTransforms(string scene)
    {
        Dictionary<string, RectTransformRecord> result = new();
        MatchCollection documents = Regex.Matches(
            scene,
            @"(?ms)^--- !u!224 &(?<id>\d+)\n(?<body>.*?)(?=^--- !u!|\z)");

        foreach (Match document in documents)
        {
            string body = document.Groups["body"].Value;
            Match parent = Regex.Match(body, @"(?m)^  m_Father: \{fileID: (?<id>\d+)\}");
            Match children = Regex.Match(body, @"(?ms)^  m_Children:\n(?<items>.*?)(?=^  m_Father:)");
            List<string> childIds = new();
            foreach (Match child in Regex.Matches(children.Groups["items"].Value, @"fileID: (?<id>\d+)"))
                childIds.Add(child.Groups["id"].Value);

            result[document.Groups["id"].Value] = new RectTransformRecord(
                parent.Groups["id"].Value,
                childIds);
        }

        return result;
    }

    private sealed class RectTransformRecord
    {
        public RectTransformRecord(string parentId, List<string> childIds)
        {
            ParentId = parentId;
            ChildIds = childIds;
        }

        public string ParentId { get; }
        public List<string> ChildIds { get; }
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, System.StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Missing method: {signature}");

        int nextMethod = source.IndexOf("\n    private ", start + signature.Length, System.StringComparison.Ordinal);
        Assert.That(nextMethod, Is.GreaterThan(start));
        return source.Substring(start, nextMethod - start);
    }
}
