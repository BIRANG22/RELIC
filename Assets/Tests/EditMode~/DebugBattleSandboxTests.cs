using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class DebugBattleSandboxTests
{
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";
    private const string DebugBattleScenePath = "Assets/Project/Scenes/YDM/DebugBattle.unity";
    private const long MainCameraGameObjectFileId = 1610817903;
    private const long CameraShakeComponentFileId = 1610817909;
    private static readonly Regex SceneBlockRegex = new(
        @"(?ms)^--- !u!(?<type>\d+) &(?<id>\d+)\r?\n(?<body>.*?)(?=^--- !u!|\z)",
        RegexOptions.Compiled);

    private static readonly string[] LayoutFieldPrefixes =
    {
        "  m_LocalRotation:",
        "  m_LocalPosition:",
        "  m_LocalScale:",
        "  m_ConstrainProportionsScale:",
        "  m_LocalEulerAnglesHint:",
        "  m_AnchorMin:",
        "  m_AnchorMax:",
        "  m_AnchoredPosition:",
        "  m_SizeDelta:",
        "  m_Pivot:"
    };

    [Test]
    public void DefaultDebugPartySize_IsSingleAlly()
    {
        Assert.That(DebugBattlePartySetup.DefaultDebugPartySize, Is.EqualTo(1));
    }

    [Test]
    public void CreateDebugRuntime_UsesRequestedCharacterAndGrid()
    {
        CharacterMasterData master = new()
        {
            CharacterId = "Char_Test",
            MaxHP = 42,
            MaxCost = 7,
            CostRecovery = 3,
            PassiveSkill1 = "S_Passive_Test",
            UniqueSkill1 = "S_Unique_Test",
            CharacterSkill1 = "S_Ability_Test",
            CommonSkill1 = "S_Common_Test"
        };

        CharacterRuntimeData runtime = DebugBattlePartySetup.CreateDebugRuntime(master, 9);

        Assert.That(runtime.CharacterId, Is.EqualTo("Char_Test"));
        Assert.That(runtime.CurrentHP, Is.EqualTo(42));
        Assert.That(runtime.CurrentCost, Is.EqualTo(7));
        Assert.That(runtime.EquippedSkillIds[0], Is.EqualTo("S_Unique_Test"));
        Assert.That(runtime.EquippedSkillIds[1], Is.EqualTo("S_Ability_Test"));
        Assert.That(runtime.EquippedSkillIds[2], Is.EqualTo("S_Common_Test"));
    }

    [Test]
    public void EquipOnlyRelics_DoesNotPlaceCompoundInRelicSlots()
    {
        CharacterRuntimeData runtime = new();

        BattleEffectDebugTool.EquipOnlyRelics(
            runtime,
            new[] { "Relic_P_01", "Compound_01", "Relic_P_02" });

        Assert.That(runtime.EquippedRelicIds[0], Is.Null.Or.Empty);
        Assert.That(runtime.EquippedRelicIds[1], Is.EqualTo("Relic_P_01"));
        Assert.That(runtime.EquippedRelicIds[2], Is.EqualTo("Relic_P_02"));
    }

    [Test]
    public void SetPassiveRelicSlot_LeavesCompoundSlotUntouched()
    {
        CharacterRuntimeData runtime = new()
        {
            EquippedRelicIds = new[] { "Compound_01", "", "", "", "", "", "" }
        };

        BattleEffectDebugTool.SetPassiveRelicSlot(runtime, 0, "Relic_P_01");

        Assert.That(runtime.EquippedRelicIds[0], Is.EqualTo("Compound_01"));
        Assert.That(runtime.EquippedRelicIds[1], Is.EqualTo("Relic_P_01"));
    }

    [Test]
    public void SetCompoundSlot_UsesInternalCompoundCompatibilitySlot()
    {
        CharacterRuntimeData runtime = new();

        BattleEffectDebugTool.SetCompoundSlot(runtime, "Compound_01");

        Assert.That(runtime.EquippedRelicIds[0], Is.EqualTo("Compound_01"));
    }

    [Test]
    public void SetSkillDisplaySlot_UsesBattleCharacterPanelDisplayMapping()
    {
        CharacterRuntimeData runtime = new()
        {
            AbilitySkillId = "S_Ability_Old",
            UniqueSkillId = "S_Unique_Old",
            EquippedSkillIds = new[] { "S_Unique_Old", "S_Ability_Old", "", "" }
        };

        BattleEffectDebugTool.SetSkillDisplaySlot(runtime, 0, "S_Ability_New");
        BattleEffectDebugTool.SetSkillDisplaySlot(runtime, 1, "S_Core_01");
        BattleEffectDebugTool.SetSkillDisplaySlot(runtime, 2, "S_Core_02");
        BattleEffectDebugTool.SetSkillDisplaySlot(runtime, 3, "S_Unique_New");

        Assert.That(runtime.AbilitySkillId, Is.EqualTo("S_Ability_New"));
        Assert.That(runtime.UniqueSkillId, Is.EqualTo("S_Unique_New"));
        Assert.That(runtime.EquippedSkillIds[1], Is.EqualTo("S_Ability_New"));
        Assert.That(runtime.EquippedSkillIds[2], Is.EqualTo("S_Core_01"));
        Assert.That(runtime.EquippedSkillIds[3], Is.EqualTo("S_Core_02"));
        Assert.That(runtime.EquippedSkillIds[0], Is.EqualTo("S_Unique_New"));
    }

    [Test]
    public void AdjustDebugStats_ClampResourceValues()
    {
        CharacterRuntimeData runtime = new()
        {
            MaxHP = 20,
            CurrentHP = 5,
            MaxCost = 6,
            CurrentCost = 2,
            CurrentShield = 3,
            CostRecovery = 1
        };

        BattleEffectDebugTool.AdjustCurrentHP(runtime, -10);
        BattleEffectDebugTool.AdjustCurrentCost(runtime, 20);
        BattleEffectDebugTool.AdjustCurrentShield(runtime, -10);
        BattleEffectDebugTool.AdjustCostRecovery(runtime, -10);

        Assert.That(runtime.CurrentHP, Is.EqualTo(1));
        Assert.That(runtime.CurrentCost, Is.EqualTo(6));
        Assert.That(runtime.CurrentShield, Is.EqualTo(0));
        Assert.That(runtime.CostRecovery, Is.EqualTo(0));
    }

    [Test]
    public void ClampWindowRect_RespectsMinimumSize()
    {
        Rect clamped = BattleEffectDebugWindow.ClampWindowRect(
            new Rect(16f, 16f, 120f, 80f),
            new Vector2(520f, 420f));

        Assert.That(clamped.width, Is.EqualTo(520f));
        Assert.That(clamped.height, Is.EqualTo(420f));
    }

    [Test]
    public void ResizeHandleSize_IsLargeEnoughForComfortableDragging()
    {
        Assert.That(BattleEffectDebugWindow.ResizeHandleSize, Is.GreaterThanOrEqualTo(44f));
    }

    [Test]
    public void ClampUiScale_KeepsDebugTextReadable()
    {
        Assert.That(BattleEffectDebugWindow.ClampUiScale(0.25f), Is.EqualTo(1f));
        Assert.That(BattleEffectDebugWindow.ClampUiScale(1.5f), Is.EqualTo(1.5f));
        Assert.That(BattleEffectDebugWindow.ClampUiScale(3f), Is.EqualTo(2f));
    }

    [Test]
    public void ScaledControlHeight_GrowsWithUiScale()
    {
        Assert.That(BattleEffectDebugWindow.GetScaledControlHeight(1f), Is.EqualTo(24f));
        Assert.That(BattleEffectDebugWindow.GetScaledControlHeight(1.5f), Is.EqualTo(36f));
    }

    [Test]
    public void DebugBattleSharedSceneLayout_MatchesBattleScene()
    {
        Dictionary<long, SceneBlock> battleBlocks = ParseSceneBlocks(ReadScene(BattleScenePath));
        Dictionary<long, SceneBlock> debugBlocks = ParseSceneBlocks(ReadScene(DebugBattleScenePath));
        Dictionary<long, string> debugNames = BuildGameObjectNameMap(debugBlocks);
        List<string> mismatches = new();

        foreach ((long id, SceneBlock debugBlock) in debugBlocks)
        {
            if (!IsLayoutComponent(debugBlock.Type) ||
                !battleBlocks.TryGetValue(id, out SceneBlock battleBlock) ||
                battleBlock.Type != debugBlock.Type)
            {
                continue;
            }

            string expected = ExtractLayoutFields(battleBlock.Body);
            string actual = ExtractLayoutFields(debugBlock.Body);
            if (expected == actual)
            {
                continue;
            }

            long gameObjectId = ExtractGameObjectFileId(debugBlock.Body);
            string name = debugNames.TryGetValue(gameObjectId, out string value) ? value : "(unknown)";
            mismatches.Add($"{id} {name}\nExpected:\n{expected}\nActual:\n{actual}");
        }

        Assert.That(mismatches, Is.Empty, string.Join("\n\n", mismatches));
    }

    [Test]
    public void DebugBattleMainCamera_MatchesBattleSceneCameraComponents()
    {
        Dictionary<long, SceneBlock> battleBlocks = ParseSceneBlocks(ReadScene(BattleScenePath));
        Dictionary<long, SceneBlock> debugBlocks = ParseSceneBlocks(ReadScene(DebugBattleScenePath));

        Assert.That(debugBlocks, Contains.Key(CameraShakeComponentFileId));
        Assert.That(
            NormalizeLineEndings(debugBlocks[MainCameraGameObjectFileId].Text),
            Is.EqualTo(NormalizeLineEndings(battleBlocks[MainCameraGameObjectFileId].Text)));
        Assert.That(
            NormalizeLineEndings(debugBlocks[CameraShakeComponentFileId].Text),
            Is.EqualTo(NormalizeLineEndings(battleBlocks[CameraShakeComponentFileId].Text)));
    }

    private static string ReadScene(string scenePath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return File.ReadAllText(Path.Combine(projectRoot, scenePath));
    }

    private static Dictionary<long, SceneBlock> ParseSceneBlocks(string scene)
    {
        Dictionary<long, SceneBlock> blocks = new();
        foreach (Match match in SceneBlockRegex.Matches(scene))
        {
            long id = long.Parse(match.Groups["id"].Value);
            blocks[id] = new SceneBlock(
                int.Parse(match.Groups["type"].Value),
                match.Groups["body"].Value,
                match.Value);
        }

        return blocks;
    }

    private static Dictionary<long, string> BuildGameObjectNameMap(Dictionary<long, SceneBlock> blocks)
    {
        Dictionary<long, string> names = new();
        foreach ((long id, SceneBlock block) in blocks)
        {
            if (block.Type != 1)
            {
                continue;
            }

            Match match = Regex.Match(block.Body, @"(?m)^  m_Name: ?(.*)$");
            if (match.Success)
            {
                names[id] = match.Groups[1].Value;
            }
        }

        return names;
    }

    private static bool IsLayoutComponent(int componentType)
    {
        return componentType == 4 || componentType == 224;
    }

    private static string ExtractLayoutFields(string blockBody)
    {
        List<string> lines = new();
        foreach (string line in blockBody.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            foreach (string prefix in LayoutFieldPrefixes)
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    lines.Add(line);
                    break;
                }
            }
        }

        return string.Join("\n", lines);
    }

    private static long ExtractGameObjectFileId(string blockBody)
    {
        Match match = Regex.Match(blockBody, @"m_GameObject: \{fileID: (?<id>\d+)\}");
        return match.Success ? long.Parse(match.Groups["id"].Value) : 0;
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private readonly struct SceneBlock
    {
        public SceneBlock(int type, string body, string text)
        {
            Type = type;
            Body = body;
            Text = text;
        }

        public int Type { get; }
        public string Body { get; }
        public string Text { get; }
    }
}
