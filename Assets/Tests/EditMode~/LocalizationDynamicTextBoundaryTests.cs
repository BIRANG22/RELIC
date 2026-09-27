using System;
using System.IO;
using NUnit.Framework;

public sealed class LocalizationDynamicTextBoundaryTests
{
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";
    private const string LocalizationIgnoreScriptGuid = "426c88f35f804f14a7d43a9f9611e21b";

    [Test]
    public void RuntimeAutoLocalizer_RoutesIgnoredDynamicTextWithoutPermanentlySkippingSceneText()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Core/Localization/RuntimeTMPTextAutoLocalizer.cs");

        Assert.That(source, Does.Contain("DynamicLocalizedTMPText"));
        Assert.That(source, Does.Contain("TryAttach(text);"),
            "초기 씬 TMP도 검사 완료 후 표의 고유 원문과 연결해야 합니다.");
        Assert.That(source, Does.Contain("SceneManager.sceneLoaded"),
            "부트스트랩 뒤에 로드되는 로비/배틀 씬의 초기 TMP도 검사해야 합니다.");
        Assert.That(source, Does.Not.Contain("SceneStartupTextIds.Contains"),
            "씬 시작 시 존재한 TMP도 이후 동적 값 변경은 처리해야 합니다.");
        Assert.That(source, Does.Not.Contain("if (text.GetComponent<LocalizedTMPText>() != null)\n            return;"),
            "기존 정적 바인딩이 있어도 런타임에 다른 한국어 문구가 대입되면 새 키로 갱신해야 합니다.");
    }

    [Test]
    public void BattleWarningUiRoot_IsMarkedAsDynamicLocalizationOutput()
    {
        string scene = File.ReadAllText(BattleScenePath);
        string gameObjectBlock = FindGameObjectBlockByName(scene, "BattleWarningUI");
        string gameObjectId = ReadObjectId(gameObjectBlock);

        Assert.That(GameObjectHasComponentScript(scene, gameObjectBlock, gameObjectId, LocalizationIgnoreScriptGuid), Is.True,
            "BattleWarningUI는 호출부에서 번역된 메시지를 받는 동적 출력 루트여야 합니다.");
    }

    [Test]
    public void Back2Name_IsMarkedAsDynamicLocalizationOutput()
    {
        string scene = File.ReadAllText(BattleScenePath);
        string back2Block = FindGameObjectBlockByName(scene, "Back2");
        string back2Id = ReadObjectId(back2Block);
        string back2TransformId = ReadFirstComponentId(back2Block);
        string nameBlock = FindChildGameObjectBlockByName(scene, back2TransformId, "Name");
        string nameId = ReadObjectId(nameBlock);

        Assert.That(GameObjectHasComponentScript(scene, nameBlock, nameId, LocalizationIgnoreScriptGuid), Is.True,
            "Back2/Name은 지역명과 턴을 런타임에서 쓰는 동적 출력이어야 합니다.");
    }

    [Test]
    public void BattleMapIntroTextRoot_IsMarkedAsDynamicLocalizationOutput()
    {
        string scene = File.ReadAllText(BattleScenePath);
        string gameObjectBlock = FindGameObjectBlockByName(scene, "BattleMapIntroText");
        string gameObjectId = ReadObjectId(gameObjectBlock);

        Assert.That(GameObjectHasComponentScript(scene, gameObjectBlock, gameObjectId, LocalizationIgnoreScriptGuid), Is.True,
            "BattleMapIntroText는 런타임에서 번역된 문구를 쓰는 동적 출력 루트여야 합니다.");
    }

    private static string FindGameObjectBlockByName(string scene, string objectName)
    {
        foreach (string block in SplitBlocks(scene))
        {
            if (block.StartsWith("!u!1 &", StringComparison.Ordinal) &&
                block.IndexOf("\n  m_Name: " + objectName + "\n", StringComparison.Ordinal) >= 0)
                return block;
        }

        Assert.Fail($"GameObject를 찾을 수 없습니다: {objectName}");
        return string.Empty;
    }

    private static string FindChildGameObjectBlockByName(string scene, string parentTransformId, string objectName)
    {
        foreach (string block in SplitBlocks(scene))
        {
            if (!block.StartsWith("!u!224 &", StringComparison.Ordinal) ||
                block.IndexOf("\n  m_Father: {fileID: " + parentTransformId + "}\n", StringComparison.Ordinal) < 0)
                continue;

            string gameObjectId = ReadReferencedId(block, "m_GameObject");
            string gameObjectBlock = FindBlock(scene, "--- !u!1 &" + gameObjectId);
            if (gameObjectBlock.IndexOf("\n  m_Name: " + objectName + "\n", StringComparison.Ordinal) >= 0)
                return gameObjectBlock;
        }

        Assert.Fail($"자식 GameObject를 찾을 수 없습니다: {objectName}");
        return string.Empty;
    }

    private static bool GameObjectHasComponentScript(
        string scene,
        string gameObjectBlock,
        string gameObjectId,
        string scriptGuid)
    {
        foreach (string componentId in ReadComponentIds(gameObjectBlock))
        {
            string componentBlock = FindBlock(scene, "--- !u!114 &" + componentId);
            if (componentBlock.IndexOf("m_GameObject: {fileID: " + gameObjectId + "}", StringComparison.Ordinal) >= 0 &&
                componentBlock.IndexOf("guid: " + scriptGuid, StringComparison.Ordinal) >= 0)
                return true;
        }

        return false;
    }

    private static string[] SplitBlocks(string scene) =>
        scene.Replace("\r\n", "\n").Split(new[] { "--- " }, StringSplitOptions.RemoveEmptyEntries);

    private static string FindBlock(string scene, string header)
    {
        string normalizedHeader = header.StartsWith("--- ", StringComparison.Ordinal)
            ? header.Substring(4)
            : header;
        foreach (string block in SplitBlocks(scene))
            if (block.StartsWith(normalizedHeader, StringComparison.Ordinal))
                return "--- " + block;
        return string.Empty;
    }

    private static string ReadObjectId(string block)
    {
        int ampersand = block.IndexOf('&');
        int lineEnd = block.IndexOf('\n', ampersand);
        return block.Substring(ampersand + 1, lineEnd - ampersand - 1).Trim();
    }

    private static string ReadFirstComponentId(string gameObjectBlock)
    {
        string[] ids = ReadComponentIds(gameObjectBlock);
        Assert.That(ids, Is.Not.Empty);
        return ids[0];
    }

    private static string[] ReadComponentIds(string gameObjectBlock)
    {
        var ids = new System.Collections.Generic.List<string>();
        const string marker = "component: {fileID: ";
        int index = 0;
        while ((index = gameObjectBlock.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            int start = index + marker.Length;
            int end = gameObjectBlock.IndexOf('}', start);
            ids.Add(gameObjectBlock.Substring(start, end - start));
            index = end + 1;
        }
        return ids.ToArray();
    }

    private static string ReadReferencedId(string block, string field)
    {
        string marker = field + ": {fileID: ";
        int start = block.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        int end = block.IndexOf('}', start);
        return block.Substring(start, end - start);
    }
}
