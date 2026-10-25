using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

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
    public void HardModeSlots_UseDedicatedRedBlurBehindEachIcon()
    {
        const string blurPrefabPath =
            "Assets/Project/PrefabsR/ExplorationResultHardModeBlur.prefab";
        Assert.That(File.Exists(blurPrefabPath), Is.True);

        string prefab = File.ReadAllText(blurPrefabPath).Replace("\r\n", "\n");
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");

        Assert.That(prefab, Does.Contain("m_Name: HardModeRedBlur"));
        Assert.That(prefab, Does.Contain(
            "m_Sprite: {fileID: 21300000, guid: be42d44efea28fe46abefbe8794bb30c, type: 3}"));
        Assert.That(prefab, Does.Contain("m_RaycastTarget: 0"));
        Assert.That(prefab, Does.Contain("m_Color: {r: 1, g: 0.08, b: 0.08, a: 0.5}"));
        Assert.That(scene, Does.Contain(
            "hardModeBlurPrefab: {fileID: 400000000000000004, guid: 9f4c8d0a18ac4fb4aa19b28f19b904a1, type: 3}"));
        Assert.That(source, Does.Contain("private Image hardModeBlurPrefab;"));
        Assert.That(source, Does.Contain("EnsureHardModeBlur(slot);"));
        Assert.That(source, Does.Contain("Transform icon = slot.transform.Find(\"Icon\");"));
        Assert.That(source, Does.Contain("blur.transform.SetSiblingIndex(icon.GetSiblingIndex());"));
        Assert.That(source, Does.Not.Contain("blur.transform.SetAsFirstSibling();"));
        Assert.That(source, Does.Contain("slot.transform.Find(\"HardModeRedBlur\")"));
    }

    [Test]
    public void HardModeSlots_PlaceBackdropBlurBehindWholeSlot()
    {
        const string backdropPrefabPath =
            "Assets/Project/PrefabsR/ExplorationResultHardModeBackdropBlur.prefab";
        Assert.That(File.Exists(backdropPrefabPath), Is.True);

        string prefab = File.ReadAllText(backdropPrefabPath).Replace("\r\n", "\n");
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");

        Assert.That(prefab, Does.Contain("m_Name: HardModeSlotBackdropBlur"));
        Assert.That(prefab, Does.Contain("m_SizeDelta: {x: 160, y: 160}"));
        Assert.That(prefab, Does.Contain(
            "m_Sprite: {fileID: 21300000, guid: b635db50f56843bc919984362b23a5c7, type: 3}"));
        Assert.That(prefab, Does.Contain("m_RaycastTarget: 0"));
        Assert.That(prefab, Does.Contain("m_Color: {r: 1, g: 0.03, b: 0.03, a: 0.65}"));
        Assert.That(scene, Does.Contain(
            "hardModeBackdropBlurPrefab: {fileID: 410000000000000004, guid: 72d9d7fc1f184bd78fb4ff94f26f31e8, type: 3}"));
        Assert.That(source, Does.Contain("private Image hardModeBackdropBlurPrefab;"));
        Assert.That(source, Does.Contain("private readonly List<Image> hardModeBackdropBlurs = new();"));
        Assert.That(source, Does.Contain("EnsureHardModeBackdropBlurCount(maxHardModeSlots);"));
        Assert.That(source, Does.Contain("Instantiate(hardModeBackdropBlurPrefab, hardModeSlotRoot, false)"));
        Assert.That(source, Does.Contain(
            "SyncHardModeBackdropTransform(backdropBlur.rectTransform, slot.RectTransform);"));
        Assert.That(source, Does.Contain("backdropBlur.gameObject.SetActive(visible);"));
        Assert.That(source, Does.Contain(
            "PlaceBackdropImmediatelyBeforeSlot(backdropBlur.transform, slot.transform);"));
    }

    [Test]
    public void HardModeBackdropLayout_AlignsVisualCenterAndPreservesBackdropSize()
    {
        GameObject root = new("Root", typeof(RectTransform));
        try
        {
            RectTransform slot = CreateRectTransform("Slot", root.transform);
            slot.anchorMin = new Vector2(0f, 0.5f);
            slot.anchorMax = new Vector2(0f, 0.5f);
            slot.pivot = new Vector2(0f, 0.5f);
            slot.sizeDelta = new Vector2(80f, 80f);
            slot.anchoredPosition = new Vector2(100f, -42f);
            slot.localScale = Vector3.one * 0.38f;

            RectTransform backdrop = CreateRectTransform("Backdrop", root.transform);
            backdrop.sizeDelta = new Vector2(150f, 150f);

            InvokePrivateStatic("SyncHardModeBackdropTransform", backdrop, slot);

            Assert.That(Vector3.Distance(backdrop.position, slot.TransformPoint(slot.rect.center)),
                Is.LessThan(0.001f));
            Assert.That(backdrop.localScale, Is.EqualTo(slot.localScale));
            Assert.That(backdrop.sizeDelta, Is.EqualTo(new Vector2(150f, 150f)));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void HardModeBackdropOrdering_RemainsImmediatelyBeforeSlotAcrossRepeatedBindings()
    {
        GameObject root = new("Root", typeof(RectTransform));
        try
        {
            Transform backdrop = CreateRectTransform("Backdrop", root.transform);
            Transform slot = CreateRectTransform("Slot", root.transform);

            InvokePrivateStatic("PlaceBackdropImmediatelyBeforeSlot", backdrop, slot);
            InvokePrivateStatic("PlaceBackdropImmediatelyBeforeSlot", backdrop, slot);

            Assert.That(backdrop.GetSiblingIndex() + 1, Is.EqualTo(slot.GetSiblingIndex()));

            backdrop.SetAsLastSibling();
            InvokePrivateStatic("PlaceBackdropImmediatelyBeforeSlot", backdrop, slot);

            Assert.That(backdrop.GetSiblingIndex() + 1, Is.EqualTo(slot.GetSiblingIndex()));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
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

        Assert.That(ExtractDocument(scene, "940000000000000096"), Does.Contain("m_AnchoredPosition: {x: -288, y: -20}"));
        Assert.That(ExtractDocument(scene, "940000000000000148"), Does.Contain("m_AnchoredPosition: {x: 37, y: -20}"));
        Assert.That(ExtractDocument(scene, "940000000000000200"), Does.Contain("m_AnchoredPosition: {x: 362, y: -20}"));
        Assert.That(Count(scene, "m_SizeDelta: {x: 310, y: 460}"), Is.GreaterThanOrEqualTo(3));
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
    public void BattleScene_ProvidesStageSummaryIconPlaceholders()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        foreach ((string objectName, string position) in new[]
                 {
                     ("ExplorationReportElapsedTimeIcon", "{x: -735, y: 290}"),
                     ("ExplorationReportNormalBattleIcon", "{x: -735, y: 210}"),
                     ("ExplorationReportEliteBattleIcon", "{x: -735, y: 130}"),
                     ("ExplorationReportEventIcon", "{x: -735, y: 50}")
                 })
        {
            string icon = ExtractGameObjectWithComponents(scene, objectName);
            Assert.That(icon, Does.Contain("m_Father: {fileID: 940000000000000256}"));
            Assert.That(icon, Does.Contain($"m_AnchoredPosition: {position}"));
            Assert.That(icon, Does.Contain("m_SizeDelta: {x: 40, y: 40}"));
            Assert.That(icon, Does.Contain("m_Enabled: 0"));
            Assert.That(icon, Does.Contain("m_RaycastTarget: 0"));
        }
    }

    [Test]
    public void BattleScene_ProvidesCharacterRowFramePlaceholdersBehindContent()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        foreach ((string objectName, string parentId, string frameRectId) in new[]
                 {
                     ("ExplorationReportRowFrame_0", "940000000000000096", "950000000000000022"),
                     ("ExplorationReportRowFrame_1", "940000000000000148", "950000000000000026"),
                     ("ExplorationReportRowFrame_2", "940000000000000200", "950000000000000030")
                 })
        {
            string frame = ExtractGameObjectWithComponents(scene, objectName);
            Assert.That(frame, Does.Contain($"m_Father: {{fileID: {parentId}}}"));
            Assert.That(frame, Does.Contain("m_AnchorMin: {x: 0, y: 0}"));
            Assert.That(frame, Does.Contain("m_AnchorMax: {x: 1, y: 1}"));
            Assert.That(frame, Does.Contain("m_Enabled: 0"));
            Assert.That(frame, Does.Contain("m_RaycastTarget: 0"));

            string parent = ExtractDocument(scene, parentId);
            Assert.That(parent, Does.Match($@"(?m)^  m_Children:\n  - {{fileID: {frameRectId}}}$"));
        }
    }

    [Test]
    public void BattleScene_ProvidesRewardFramePlaceholderBehindContent()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        string frame = ExtractGameObjectWithComponents(scene, "ExplorationReportRewardFrame");
        Assert.That(frame, Does.Contain("m_Father: {fileID: 940000000000000002}"));
        Assert.That(frame, Does.Contain("m_AnchoredPosition: {x: 130, y: -275}"));
        Assert.That(frame, Does.Contain("m_SizeDelta: {x: 880, y: 190}"));
        Assert.That(frame, Does.Contain("m_Enabled: 0"));
        Assert.That(frame, Does.Contain("m_RaycastTarget: 0"));

        string root = ExtractDocument(scene, "940000000000000002");
        Assert.That(root, Does.Match(@"(?m)^  m_Children:\n  - \{fileID: 950000000000000034\}$"));
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

    [TestCase(120f, 0f, 0)]
    [TestCase(120f, 0.5f, 60)]
    [TestCase(120f, 1f, 120)]
    [TestCase(120f, -1f, 0)]
    [TestCase(120f, 2f, 120)]
    [TestCase(-10f, 0.5f, 0)]
    public void ResultCountUp_InterpolatesElapsedSeconds(
        float targetSeconds,
        float progress,
        int expected)
    {
        Assert.That(
            ExplorationResultPanelUI.CalculateCountUpSeconds(targetSeconds, progress),
            Is.EqualTo(expected));
    }

    [TestCase(5, 0f, 0)]
    [TestCase(5, 0.5f, 2)]
    [TestCase(5, 1f, 5)]
    [TestCase(5, -1f, 0)]
    [TestCase(5, 2f, 5)]
    [TestCase(-3, 0.5f, 0)]
    public void ResultCountUp_InterpolatesNodeCounts(
        int targetCount,
        float progress,
        int expected)
    {
        Assert.That(
            ExplorationResultPanelUI.CalculateCountUpCount(targetCount, progress),
            Is.EqualTo(expected));
    }

    [Test]
    public void ResultPanel_CountsUpElapsedTimeAndVisitedNodeCountsTogether()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");

        Assert.That(source, Does.Contain("[Header(\"Result Count-Up\")]"));
        Assert.That(source, Does.Contain("countUpDuration"));
        Assert.That(source, Does.Contain("StartResultCountUp("));
        Assert.That(source, Does.Contain("AnimateResultCountUp("));
        Assert.That(source, Does.Contain("Time.unscaledDeltaTime"));
        Assert.That(source, Does.Contain("StopResultCountUp(true)"));
        Assert.That(source, Does.Contain("BattleExplorationElapsedTime.Format(displayedSeconds)"));
        Assert.That(source, Does.Contain("SetText(normalBattleCountText, displayedNormalCount.ToString())"));
        Assert.That(source, Does.Contain("SetText(eliteBattleCountText, displayedEliteCount.ToString())"));
        Assert.That(source, Does.Contain("SetText(eventCountText, displayedEventCount.ToString())"));
    }

    [Test]
    public void ResultPanel_KeepsZeroValuesForTheFirstRenderedFrame()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");
        string method = ExtractMethod(source, "private IEnumerator AnimateResultCountUp()");

        Assert.That(
            method.IndexOf("yield return null;", System.StringComparison.Ordinal),
            Is.LessThan(method.IndexOf("elapsed += Time.unscaledDeltaTime;", System.StringComparison.Ordinal)));
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

    [TestCase(310f, 6, 6, 40f, 57.6f, 26.2f)]
    [TestCase(310f, 4, 4, 60f, 83.2f, 23.4f)]
    [TestCase(200f, 6, 6, 40f, 50f, 0f)]
    [TestCase(310f, 0, 6, 40f, 57.6f, 0f)]
    public void CharacterReport_CentersSlotColumnsInsideRoot(
        float rootWidth,
        int maxCount,
        int columns,
        float horizontalSpacing,
        float slotVisualWidth,
        float expectedStartX)
    {
        float result = ExplorationResultCharacterRowUI.CalculateCenteredSlotStartX(
            rootWidth,
            maxCount,
            columns,
            horizontalSpacing,
            slotVisualWidth);

        Assert.That(result, Is.EqualTo(expectedStartX).Within(0.001f));
    }

    [Test]
    public void BattleScene_KeepsLevelAndExperienceInsideRoomyCharacterFrames()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        foreach ((string rowId, string sliderId, string fillId, string experienceTextId, string levelTextId, string frameId) in new[]
                 {
                     ("940000000000000096", "940000000000000125", "940000000000000130", "940000000000000134", "940000000000000138", "950000000000000022"),
                     ("940000000000000148", "940000000000000177", "940000000000000182", "940000000000000186", "940000000000000190", "950000000000000026"),
                     ("940000000000000200", "940000000000000229", "940000000000000234", "940000000000000238", "940000000000000242", "950000000000000030")
                 })
        {
            string row = ExtractDocument(scene, rowId);
            Assert.That(row, Does.Contain("m_SizeDelta: {x: 310, y: 460}"));
            Assert.That(row, Does.Match(@"m_AnchoredPosition: \{x: [-0-9.]+, y: -20\}"));

            string slider = ExtractDocument(scene, sliderId);
            Assert.That(slider, Does.Contain("m_AnchorMin: {x: 0, y: 0}"));
            Assert.That(slider, Does.Contain("m_AnchorMax: {x: 0, y: 0}"));
            Assert.That(slider, Does.Contain("m_AnchoredPosition: {x: 0, y: 0}"));
            Assert.That(slider, Does.Contain("m_SizeDelta: {x: 0, y: 32}"));

            string fill = ExtractDocument(scene, fillId);
            Assert.That(fill, Does.Contain("m_LocalScale: {x: 0.72196245, y: 1, z: 1}"));
            Assert.That(fill, Does.Contain("m_AnchoredPosition: {x: 31, y: 0}"));

            string experienceText = ExtractDocument(scene, experienceTextId);
            Assert.That(experienceText, Does.Contain("m_AnchorMin: {x: 0, y: 1}"));
            Assert.That(experienceText, Does.Contain("m_AnchorMax: {x: 0, y: 1}"));
            Assert.That(experienceText, Does.Contain("m_AnchoredPosition: {x: 100, y: -12}"));
            Assert.That(experienceText, Does.Contain("m_SizeDelta: {x: 30, y: 24}"));

            string levelText = ExtractDocument(scene, levelTextId);
            Assert.That(levelText, Does.Contain("m_AnchorMin: {x: 0, y: 1}"));
            Assert.That(levelText, Does.Contain("m_AnchorMax: {x: 0, y: 1}"));
            Assert.That(levelText, Does.Contain("m_AnchoredPosition: {x: 37, y: -12}"));
            Assert.That(levelText, Does.Contain("m_SizeDelta: {x: 60, y: 30}"));

            string frame = ExtractDocument(scene, frameId);
            Assert.That(frame, Does.Contain("m_AnchorMin: {x: 0, y: 0}"));
            Assert.That(frame, Does.Contain("m_AnchorMax: {x: 1, y: 1}"));
            Assert.That(frame, Does.Contain("m_AnchoredPosition: {x: 0, y: 0}"));
            Assert.That(frame, Does.Contain("m_SizeDelta: {x: 0, y: 0}"));
        }

        foreach ((string childId, string position, string size) in new[]
                 {
                     ("940000000000000361", "{x: 155, y: -75}", "{x: 250, y: 90}"),
                     ("940000000000000105", "{x: 155, y: -140}", "{x: 250, y: 40}"),
                     ("940000000000000364", "{x: 155, y: -176}", "{x: 250, y: 32}"),
                     ("940000000000000113", "{x: 155, y: -225}", "{x: 250, y: 66}"),
                     ("940000000000000117", "{x: 155, y: -291}", "{x: 250, y: 66}"),
                     ("940000000000000121", "{x: 155, y: -354}", "{x: 250, y: 60}"),
                     ("940000000000000371", "{x: 155, y: -75}", "{x: 250, y: 90}"),
                     ("940000000000000157", "{x: 155, y: -140}", "{x: 250, y: 40}"),
                     ("940000000000000374", "{x: 155, y: -176}", "{x: 250, y: 32}"),
                     ("940000000000000165", "{x: 155, y: -225}", "{x: 250, y: 66}"),
                     ("940000000000000169", "{x: 155, y: -291}", "{x: 250, y: 66}"),
                     ("940000000000000173", "{x: 155, y: -354}", "{x: 250, y: 60}"),
                     ("940000000000000381", "{x: 155, y: -75}", "{x: 250, y: 90}"),
                     ("940000000000000209", "{x: 155, y: -140}", "{x: 250, y: 40}"),
                     ("940000000000000384", "{x: 155, y: -176}", "{x: 250, y: 32}"),
                     ("940000000000000217", "{x: 155, y: -225}", "{x: 250, y: 66}"),
                     ("940000000000000221", "{x: 155, y: -291}", "{x: 250, y: 66}"),
                     ("940000000000000225", "{x: 155, y: -354}", "{x: 250, y: 60}")
                 })
        {
            string child = ExtractDocument(scene, childId);
            Assert.That(child, Does.Contain("m_AnchorMin: {x: 0, y: 1}"));
            Assert.That(child, Does.Contain("m_AnchorMax: {x: 0, y: 1}"));
            Assert.That(child, Does.Contain($"m_AnchoredPosition: {position}"));
            Assert.That(child, Does.Contain($"m_SizeDelta: {size}"));
        }

        foreach (string childId in new[]
                 {
                     "940000000000000161", "940000000000000213"
                 })
        {
            Assert.That(
                ExtractDocument(scene, childId),
                Does.Contain("m_AnchoredPosition: {x: 150, y: -220}"),
                $"Top-left anchored child {childId} must keep its screen position after the Row grows.");
        }

        foreach (string childId in new[]
                 {
                     "940000000000000142", "940000000000000194", "940000000000000246"
                 })
        {
            Assert.That(
                ExtractDocument(scene, childId),
                Does.Contain("m_AnchoredPosition: {x: 150, y: -515}"),
                $"Top-left anchored child {childId} must keep its screen position after the Row grows.");
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
    public void CharacterReport_PlacesInformationIconsInsidePortraitsAndUsesCompactRows()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        string[] rowRectIds = { "940000000000000096", "940000000000000148", "940000000000000200" };
        string[] viewportRectIds = { "940000000000000361", "940000000000000371", "940000000000000381" };
        string[] iconRectIds = { "940000000000000105", "940000000000000157", "940000000000000209" };
        string[] overlayAnchorRectIds = { "940000000000000364", "940000000000000374", "940000000000000384" };
        string[] verticalLayoutGroupIds = { "810445366", "604961630", "837265848" };

        for (int index = 0; index < rowRectIds.Length; index++)
        {
            string row = ExtractDocument(scene, rowRectIds[index]);
            string viewport = ExtractDocument(scene, viewportRectIds[index]);
            string icon = ExtractDocument(scene, iconRectIds[index]);
            string overlayAnchor = ExtractDocument(scene, overlayAnchorRectIds[index]);
            string verticalLayoutGroup = ExtractDocument(scene, verticalLayoutGroupIds[index]);

            Assert.That(row, Does.Contain("  m_SizeDelta: {x: 310, y: 310}"));
            Assert.That(row, Does.Not.Contain($"  - {{fileID: {iconRectIds[index]}}}"));
            Assert.That(row, Does.Not.Contain($"  - {{fileID: {overlayAnchorRectIds[index]}}}"));
            Assert.That(viewport, Does.Contain($"  - {{fileID: {iconRectIds[index]}}}"));
            Assert.That(icon, Does.Contain($"  m_Father: {{fileID: {viewportRectIds[index]}}}"));
            Assert.That(icon, Does.Contain($"  - {{fileID: {overlayAnchorRectIds[index]}}}"));
            Assert.That(icon, Does.Contain("  m_AnchorMin: {x: 0, y: 0}"));
            Assert.That(icon, Does.Contain("  m_AnchorMax: {x: 0, y: 0}"));
            Assert.That(icon, Does.Contain("  m_AnchoredPosition: {x: 26, y: 26}"));
            Assert.That(icon, Does.Contain("  m_SizeDelta: {x: 40, y: 40}"));
            Assert.That(icon, Does.Contain("  m_Pivot: {x: 0.5, y: 0.5}"));
            Assert.That(overlayAnchor, Does.Contain($"  m_Father: {{fileID: {iconRectIds[index]}}}"));
            Assert.That(overlayAnchor, Does.Contain("  m_AnchorMin: {x: 0, y: 0}"));
            Assert.That(overlayAnchor, Does.Contain("  m_AnchorMax: {x: 1, y: 1}"));
            Assert.That(overlayAnchor, Does.Contain("  m_AnchoredPosition: {x: 0, y: 0}"));
            Assert.That(overlayAnchor, Does.Contain("  m_SizeDelta: {x: 0, y: 0}"));
            Assert.That(verticalLayoutGroup, Does.Contain("  m_Spacing: -1"));
        }
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

    private static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = value.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void InvokePrivateStatic(string methodName, params object[] arguments)
    {
        MethodInfo method = typeof(ExplorationResultPanelUI).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, $"Missing method: {methodName}");
        method.Invoke(null, arguments);
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
