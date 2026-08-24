using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class ExplorationResultPanelSourceTests
{
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags InstanceAnyVisibility =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Test]
    public void ExplorationResultPanel_UsesScenePlacedStructuredBindings()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");

        StringAssert.Contains("titleText", source);
        StringAssert.Contains("stageNameText", source);
        StringAssert.Contains("stageResultText", source);
        StringAssert.Contains("stagePreviewImage", source);
        StringAssert.Contains("stagePresentations", source);
        StringAssert.Contains("StagePresentationEntry", source);
        StringAssert.Contains("PreviewSprite", source);
        StringAssert.Contains("redDustiumText", source);
        StringAssert.Contains("characterRows", source);
        StringAssert.Contains("ExplorationResultCharacterRowUI", source);
        StringAssert.Contains("SetActive", source);
        Assert.That(source, Does.Not.Contain("Instantiate("));
        Assert.That(source, Does.Not.Contain("new GameObject("));
    }

    [Test]
    public void StagePresentation_UsesMapDataStageBindingKey()
    {
        Type panelType = typeof(ExplorationResultPanelUI);
        Type entryType = panelType.GetNestedType("StagePresentationEntry", BindingFlags.NonPublic);

        Assert.That(entryType, Is.Not.Null);
        Assert.That(entryType.GetField("stage", InstancePrivate), Is.Not.Null);
        Assert.That(entryType.GetField("mapId", InstancePrivate), Is.Null);
        Assert.That(entryType.GetField("nodeType", InstancePrivate), Is.Null);
        Assert.That(
            panelType.GetMethod(
                "ResolveStagePresentation",
                InstancePrivate,
                null,
                new[] { typeof(MapData) },
                null),
            Is.Not.Null);
    }

    [Test]
    public void StagePresentationResolver_MatchesDataSheetStage()
    {
        GameObject panelObject = new("ExplorationResultPanel");

        try
        {
            ExplorationResultPanelUI panel = panelObject.AddComponent<ExplorationResultPanelUI>();
            Type panelType = typeof(ExplorationResultPanelUI);
            Type entryType = panelType.GetNestedType("StagePresentationEntry", BindingFlags.NonPublic);
            Assert.That(entryType, Is.Not.Null);

            object stage1Entry = CreateStagePresentationEntry(entryType, "Stage1", "Ruined Road");
            object stage2Entry = CreateStagePresentationEntry(entryType, "Stage2", "Black Chapel");

            FieldInfo presentationsField = panelType.GetField("stagePresentations", InstancePrivate);
            Assert.That(presentationsField, Is.Not.Null);
            IList presentations = (IList)Activator.CreateInstance(presentationsField.FieldType);
            presentations.Add(stage1Entry);
            presentations.Add(stage2Entry);
            presentationsField.SetValue(panel, presentations);

            MethodInfo resolver = panelType.GetMethod(
                "ResolveStagePresentation",
                InstancePrivate,
                null,
                new[] { typeof(MapData) },
                null);
            Assert.That(resolver, Is.Not.Null);

            object matched = resolver.Invoke(
                panel,
                new object[]
                {
                    new MapData
                    {
                        MapId = "Map_Boss_01",
                        Type = "Boss",
                        Stage = "Stage2"
                    }
                });

            object notMatched = resolver.Invoke(
                panel,
                new object[]
                {
                    new MapData
                    {
                        MapId = "Stage1",
                        Type = "Stage1",
                        Stage = "UnknownStage"
                    }
                });

            Assert.That(matched, Is.SameAs(stage2Entry));
            Assert.That(ReadStringProperty(entryType, matched, "DisplayName"), Is.EqualTo("Black Chapel"));
            Assert.That(notMatched, Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void ExplorationResultPanel_UsesBattleHudSideImageResolver()
    {
        string resultPanelSource = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");
        string playerHudSource = File.ReadAllText(
            "Assets/Project/Scripts/UI/Battle/Canvas/PlayerHUDSlot.cs");

        StringAssert.Contains("TryGetSideImage", playerHudSource);
        StringAssert.Contains("TryGetSideImage", resultPanelSource);
    }

    [Test]
    public void ExplorationResultCharacterRow_DoesNotCreateRuntimeObjects()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultCharacterRowUI.cs");

        Assert.That(source, Does.Not.Contain("Instantiate("));
        Assert.That(source, Does.Not.Contain("new GameObject("));
    }

    [Test]
    public void ExplorationResultPanel_DoesNotCreateSortingComponentsAtRuntime()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");

        Assert.That(source, Does.Not.Contain("AddComponent<Canvas>"));
        Assert.That(source, Does.Not.Contain("AddComponent<GraphicRaycaster>"));
    }

    [Test]
    public void BattleScene_ContainsPlacedExplorationReportObjects()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");

        StringAssert.Contains("m_Name: ExplorationResultPanel", scene);
        StringAssert.Contains("m_Name: ExplorationReportFrame", scene);
        StringAssert.Contains("m_Name: ExplorationReportTopGroup", scene);
        StringAssert.Contains("m_Name: ExplorationReportStageGroup", scene);
        StringAssert.Contains("m_Name: ExplorationReportTableHeaderGroup", scene);
        StringAssert.Contains("m_Name: ExplorationReportTitle", scene);
        StringAssert.Contains("m_Name: ExplorationReportStageName", scene);
        StringAssert.Contains("m_Name: ExplorationReportStagePreview", scene);
        StringAssert.Contains("m_Name: ExplorationReportRedDustium", scene);
        StringAssert.Contains("m_Name: ExplorationReportRows", scene);
        StringAssert.Contains("m_Name: ExplorationReportRow_0", scene);
        StringAssert.Contains("m_Name: ExplorationReportRow_1", scene);
        StringAssert.Contains("m_Name: ExplorationReportRow_2", scene);
        StringAssert.Contains("m_Name: ExplorationReportExpSlider_0", scene);
        Assert.That(scene, Does.Not.Contain("`n"));
        Assert.That(scene.Replace("\r\n", "\n"), Does.Not.Contain("m_Children:\n  []"));
    }

    [Test]
    public void BattleScene_GroupsExplorationReportFrameChildrenByRole()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");

        string frameRect = GetYamlObject(scene, "940000000000000002");
        string topGroupRect = GetYamlObject(scene, "940000000000000254");
        string stageGroupRect = GetYamlObject(scene, "940000000000000256");
        string tableHeaderGroupRect = GetYamlObject(scene, "940000000000000258");
        string titleRect = GetYamlObject(scene, "940000000000000006");
        string stageNameRect = GetYamlObject(scene, "940000000000000026");
        string headerKillRect = GetYamlObject(scene, "940000000000000066");
        string rowsRect = GetYamlObject(scene, "940000000000000094");

        StringAssert.Contains("  - {fileID: 940000000000000254}", frameRect);
        StringAssert.Contains("  - {fileID: 940000000000000256}", frameRect);
        StringAssert.Contains("  - {fileID: 940000000000000258}", frameRect);
        StringAssert.Contains("  - {fileID: 940000000000000094}", frameRect);
        StringAssert.Contains("m_Father: {fileID: 940000000000000254}", titleRect);
        StringAssert.Contains("m_Father: {fileID: 940000000000000256}", stageNameRect);
        StringAssert.Contains("m_Father: {fileID: 940000000000000258}", headerKillRect);
        StringAssert.Contains("m_Father: {fileID: 940000000000000002}", rowsRect);
        Assert.That(topGroupRect, Does.Not.Contain("m_Children: []"));
        Assert.That(stageGroupRect, Does.Not.Contain("m_Children: []"));
        Assert.That(tableHeaderGroupRect, Does.Not.Contain("m_Children: []"));
    }

    [Test]
    public void BattleScene_ParentsExplorationResultPanelUnderRootCanvas()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");

        string rootCanvasRectTransform = GetYamlObject(scene, "742669610");
        string battleHudCanvasRectTransform = GetYamlObject(scene, "1512511010");
        string resultPanelRectTransform = GetYamlObject(scene, "1684958231");

        StringAssert.Contains("  - {fileID: 1684958231}", rootCanvasRectTransform);
        Assert.That(battleHudCanvasRectTransform, Does.Not.Contain("  - {fileID: 1684958231}"));
        StringAssert.Contains("m_Father: {fileID: 742669610}", resultPanelRectTransform);
    }

    [Test]
    public void BattleScene_ResultPanelHasScenePlacedTopCanvas()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");
        string resultPanelGameObject = GetYamlObject(scene, "1684958230");
        string resultPanelCanvas = GetYamlComponentForGameObject(scene, "223", "1684958230");
        string resultPanelRaycaster = GetYamlMonoBehaviourForGameObject(
            scene,
            "1684958230",
            "dc42784cf147c0c48a680349fa168899");

        StringAssert.Contains("- component: {fileID: 940000000000000251}", resultPanelGameObject);
        StringAssert.Contains("m_OverrideSorting: 1", resultPanelCanvas);
        StringAssert.Contains("m_SortingOrder: 25000", resultPanelCanvas);
        StringAssert.Contains("m_IgnoreReversedGraphics: 1", resultPanelRaycaster);
    }

    [Test]
    public void BattleScene_DoesNotContainDuplicateYamlIdentifiers()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");
        MatchCollection matches = Regex.Matches(
            scene,
            @"^--- !u!\d+ &(\d+)$",
            RegexOptions.Multiline);
        Dictionary<string, int> firstLineById = new();
        List<string> duplicateReports = new();

        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            string id = match.Groups[1].Value;
            int line = CountLinesBefore(scene, match.Index) + 1;

            if (firstLineById.TryGetValue(id, out int firstLine))
                duplicateReports.Add($"{id}: first line {firstLine}, duplicate line {line}");
            else
                firstLineById.Add(id, line);
        }

        Assert.That(duplicateReports, Is.Empty, string.Join("\n", duplicateReports));
    }

    private static string GetYamlObject(string yaml, string fileId)
    {
        Match match = Regex.Match(
            yaml,
            @"^--- !u!\d+ &" + Regex.Escape(fileId) + @"\r?\n.*?(?=^--- !u!|\z)",
            RegexOptions.Multiline | RegexOptions.Singleline);

        Assert.That(match.Success, Is.True, $"Missing yaml object: {fileId}");
        return match.Value;
    }

    private static string GetYamlComponentForGameObject(string yaml, string componentType, string gameObjectId)
    {
        Match match = Regex.Match(
            yaml,
            @"^--- !u!" + Regex.Escape(componentType) + @" &\d+\r?\n.*?m_GameObject: \{fileID: " +
            Regex.Escape(gameObjectId) + @"\}.*?(?=^--- !u!|\z)",
            RegexOptions.Multiline | RegexOptions.Singleline);

        Assert.That(match.Success, Is.True, $"Missing component {componentType} for GameObject {gameObjectId}");
        return match.Value;
    }

    private static string GetYamlMonoBehaviourForGameObject(string yaml, string gameObjectId, string scriptGuid)
    {
        Match match = Regex.Match(
            yaml,
            @"^--- !u!114 &\d+\r?\n.*?m_GameObject: \{fileID: " +
            Regex.Escape(gameObjectId) + @"\}.*?m_Script: \{fileID: 11500000, guid: " +
            Regex.Escape(scriptGuid) + @", type: 3\}.*?(?=^--- !u!|\z)",
            RegexOptions.Multiline | RegexOptions.Singleline);

        Assert.That(match.Success, Is.True, $"Missing MonoBehaviour {scriptGuid} for GameObject {gameObjectId}");
        return match.Value;
    }

    private static int CountLinesBefore(string text, int index)
    {
        int lineCount = 0;
        for (int i = 0; i < index; i++)
        {
            if (text[i] == '\n')
                lineCount++;
        }

        return lineCount;
    }

    private static object CreateStagePresentationEntry(Type entryType, string stage, string displayName)
    {
        object entry = Activator.CreateInstance(entryType, true);
        SetPrivateField(entry, "stage", stage);
        SetPrivateField(entry, "displayName", displayName);
        return entry;
    }

    private static string ReadStringProperty(Type type, object target, string propertyName)
    {
        PropertyInfo property = type.GetProperty(propertyName, InstanceAnyVisibility);
        Assert.That(property, Is.Not.Null, $"Missing property: {propertyName}");
        return property.GetValue(target) as string;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, InstancePrivate);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }
}
