using System;
using System.IO;
using System.Text;
using NUnit.Framework;

public sealed class DebugBattleSceneSyncTests
{
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";
    private const string DebugBattleScenePath = "Assets/Project/Scenes/YDM/DebugBattle.unity";
    private const string DebugBattleBootstrapPath =
        "Assets/Project/Scripts/Debug/DebugBattleSceneBootstrap.cs";
    private const string DebugBattleTargetControllerPath =
        "Assets/Project/Scripts/Debug/DebugBattleTargetController.cs";

    private const long DataManagerGameObjectFileId = 910050501;
    private const long DataManagerTransformFileId = 910050502;
    private const long DataManagerComponentFileId = 910050503;

    [Test]
    public void DebugBattleScene_MatchesCurrentBattleExceptStandaloneDataManager()
    {
        string battleScene = NormalizeLineEndings(File.ReadAllText(BattleScenePath));
        string debugBattleScene = NormalizeLineEndings(File.ReadAllText(DebugBattleScenePath));

        Assert.That(RemoveStandaloneDataManager(debugBattleScene), Is.EqualTo(battleScene),
            "DebugBattle의 공용 씬 구성은 현재 Battle과 같아야 합니다.");
    }

    [Test]
    public void DebugBattleScene_PreservesStandaloneDataManagerReferences()
    {
        string scene = File.ReadAllText(DebugBattleScenePath).Replace("\r\n", "\n");

        Assert.That(Count(scene, $"--- !u!1 &{DataManagerGameObjectFileId}\n"), Is.EqualTo(1));
        Assert.That(Count(scene, $"--- !u!4 &{DataManagerTransformFileId}\n"), Is.EqualTo(1));
        Assert.That(Count(scene, $"--- !u!114 &{DataManagerComponentFileId}\n"), Is.EqualTo(1));
        Assert.That(Count(scene, $"  - {{fileID: {DataManagerTransformFileId}}}\n"), Is.EqualTo(1));
        StringAssert.Contains("  m_Name: DataManager\n", scene);
        StringAssert.Contains("  characterPrefabDatabase: {fileID: 11400000, guid: ac387f80eed220a499605aa01e734ef5, type: 2}\n", scene);
        StringAssert.Contains("  monsterPrefabDatabase: {fileID: 11400000, guid: 196f24977f0b6714db3466914f8baf08, type: 2}\n", scene);
        StringAssert.Contains("  skillVfxDatabase: {fileID: 11400000, guid: 64d6b86b66c94a0fa7609aa53000565e, type: 2}\n", scene);
    }

    [Test]
    public void DebugBattleScene_HasNoTrailingWhitespace()
    {
        string scene = File.ReadAllText(DebugBattleScenePath).Replace("\r\n", "\n");
        string[] lines = scene.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            Assert.That(lines[i], Is.EqualTo(lines[i].TrimEnd()),
                $"DebugBattle scene line {i + 1} has trailing whitespace.");
        }
    }

    [Test]
    public void DebugBattleRuntimeTools_RemainSceneScoped()
    {
        string bootstrap = File.ReadAllText(DebugBattleBootstrapPath);
        string targetController = File.ReadAllText(DebugBattleTargetControllerPath);

        StringAssert.Contains("private const string DebugBattleSceneName = \"DebugBattle\";", bootstrap);
        StringAssert.Contains("scene.name != DebugBattleSceneName", bootstrap);
        StringAssert.Contains("windowObject.AddComponent<BattleEffectDebugWindow>()", bootstrap);
        StringAssert.Contains("windowObject.AddComponent<DebugBattleTargetController>()", bootstrap);
        StringAssert.Contains("windowObject.AddComponent<DebugBattleSceneRunner>()", bootstrap);
        StringAssert.Contains("DebugBattleTargetRules.TryRestoreFullHp", targetController);
    }

    private static string RemoveStandaloneDataManager(string scene)
    {
        string result = RemoveYamlDocument(scene, 1, DataManagerGameObjectFileId);
        result = RemoveYamlDocument(result, 4, DataManagerTransformFileId);
        result = RemoveYamlDocument(result, 114, DataManagerComponentFileId);
        result = result.Replace($"  - {{fileID: {DataManagerTransformFileId}}}\n", string.Empty);
        return result;
    }

    private static string RemoveYamlDocument(string yaml, int classId, long fileId)
    {
        string header = $"--- !u!{classId} &{fileId}\n";
        int start = yaml.IndexOf(header, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"YAML 문서를 찾을 수 없습니다: {header.Trim()}");

        int next = yaml.IndexOf("--- !u!", start + header.Length, StringComparison.Ordinal);
        if (next < 0)
            return yaml.Substring(0, start);

        var builder = new StringBuilder(yaml.Length - (next - start));
        builder.Append(yaml, 0, start);
        builder.Append(yaml, next, yaml.Length - next);
        return builder.ToString();
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;

        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string NormalizeLineEndings(string value)
    {
        string[] lines = value.Replace("\r\n", "\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
            lines[i] = lines[i].TrimEnd();

        return string.Join("\n", lines);
    }
}
