using System;
using System.IO;
using NUnit.Framework;

public sealed class LocalizationDynamicTextBoundaryTests
{
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string LocalizationIgnoreScriptGuid = "426c88f35f804f14a7d43a9f9611e21b";
    private const string LocalizedTmpTextScriptGuid = "700ed754fb422984990c407c26f0065c";

    [Test]
    public void EventChoiceKey_UsesEventIdChoiceOrderAndField()
    {
        var eventChoice = new Relic.Gameplay.Data.EventData
        {
            EventId = "Event_08_A",
            ChoiceOrder = 1,
        };

        Assert.That(
            GameDataLocalization.EventChoiceKey(eventChoice, "name"),
            Is.EqualTo("data.event.event_08_a.choice_1_name"));
        Assert.That(
            GameDataLocalization.EventChoiceKey(eventChoice, "description"),
            Is.EqualTo("data.event.event_08_a.choice_1_description"));
    }

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

    [Test]
    public void BattleMapIntroText_EmptyInputUsesLocalizedDefaultInsteadOfKoreanSceneFallback()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/UI/BattleMapIntroText.cs");
        string scene = File.ReadAllText(BattleScenePath);

        Assert.That(source, Does.Contain("DefaultMessageKey = \"battle.intro.enter_battle_area\""));
        Assert.That(source, Does.Contain("GameLocalization.Get(DefaultMessageKey)"));
        Assert.That(source, Does.Not.Contain("private string message ="),
            "빈 입력이 씬의 한국어 직렬화 문구로 돌아가면 안 됩니다.");
        Assert.That(scene, Does.Not.Contain("  message:"),
            "배틀 인트로에 한국어 표시 폴백을 직렬화해 두면 안 됩니다.");
    }

    [Test]
    public void EventRoomIntro_UsesLocalizedEventNameInsteadOfRawKoreanData()
    {
        string dataLocalization = File.ReadAllText(
            "Assets/Project/Scripts/Core/Localization/GameDataLocalization.cs");
        string battleController = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs");
        string eventController = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/EventRoom/EventRoomController.cs");

        Assert.That(dataLocalization, Does.Contain("EventName(EventDefinition data)"));
        Assert.That(dataLocalization,
            Does.Contain("GameLocalization.GetData(\"Event\", data.EventId, \"name\", data.EventName)"));
        Assert.That(battleController, Does.Contain("return GameDataLocalization.EventName(definition);"));
        Assert.That(battleController, Does.Not.Contain("return definition.EventName.Trim();"));
        Assert.That(eventController, Does.Contain("GameDataLocalization.EventName(definition)"));
    }

    [Test]
    public void LobbySettingOptionBack2Name_UsesLocalizedTmpTextWithoutEnglishFallback()
    {
        string scene = File.ReadAllText(LobbyScenePath);
        string settingBlock = FindGameObjectBlockByName(scene, "Setting");
        string settingTransformId = ReadFirstComponentId(settingBlock);
        string optionBlock = FindChildGameObjectBlockByName(scene, settingTransformId, "Option");
        string optionTransformId = ReadFirstComponentId(optionBlock);
        string back2Block = FindChildGameObjectBlockByName(scene, optionTransformId, "Back2");
        string back2TransformId = ReadFirstComponentId(back2Block);
        string nameBlock = FindChildGameObjectBlockByName(scene, back2TransformId, "Name");
        string nameId = ReadObjectId(nameBlock);
        string localizer = FindComponentBlockByScript(
            scene, nameBlock, nameId, LocalizedTmpTextScriptGuid);

        Assert.That(localizer, Does.Contain("localizationKey: ui.lobby.text.b1bc1a93"));
        Assert.That(localizer, Does.Contain("koreanSource:"),
            "한국어 원문은 키 매칭과 한국어 로케일 표시용으로만 보관합니다.");
    }

    [Test]
    public void LobbySharedModal_DoesNotOverwriteLocalizedBack2Name()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/LobbyPositionSharedModalBackground.cs");

        Assert.That(source, Does.Not.Contain("LobbyLocationDisplayName"));
        Assert.That(source, Does.Not.Contain("ApplyLobbyLocationName("),
            "Setting/Option/Back2/Name은 LocalizedTMPText만 작성해야 합니다.");
    }

    [Test]
    public void RuntimeLocalizers_ApplyMissingMarkerBeforeAwaitingTableLoad()
    {
        string staticSource = File.ReadAllText(
            "Assets/Project/Scripts/Core/Localization/LocalizedTMPText.cs");
        string dynamicSource = File.ReadAllText(
            "Assets/Project/Scripts/Core/Localization/DynamicLocalizedTMPText.cs");

        Assert.That(staticSource, Does.Contain("RefreshImmediatelyAndWhenReady("));
        Assert.That(dynamicSource, Does.Contain("RefreshImmediatelyAndWhenReady("));
    }

    [Test]
    public void LobbyErosionValues_AreDynamicAndEachOutputHasItsOwnTarget()
    {
        string scene = File.ReadAllText(LobbyScenePath);
        string[] erosionValues = FindGameObjectBlocksByName(scene, "Erosion_Value");

        Assert.That(erosionValues, Has.Length.EqualTo(3));
        foreach (string gameObjectBlock in erosionValues)
        {
            string gameObjectId = ReadObjectId(gameObjectBlock);
            Assert.That(
                GameObjectHasComponentScript(scene, gameObjectBlock, gameObjectId, LocalizationIgnoreScriptGuid),
                Is.True,
                "숫자 또는 포맷 문장을 런타임에 쓰는 Erosion_Value는 정적 키 자동 연결 대상이면 안 됩니다.");
        }

        Assert.That(scene, Does.Contain("erosionValueText: {fileID: 744195647}"),
            "카탈로그 점수는 ErosionSelectPanel/Select/Erosion_Value에 출력해야 합니다.");
        Assert.That(scene, Does.Contain("rewardBonusText: {fileID: 122089891}"),
            "보상 문장은 ErosionSelectPanel 바로 아래의 문장용 TMP에 출력해야 합니다.");
    }

    [TestCase("MainText", "ui.lobby.panel.erosion")]
    [TestCase("PlayText", "ui.lobby.play.ready")]
    public void RequestedIdDrivenTexts_UseLocalizedTmpText(string objectName, string expectedKey)
    {
        string scene = File.ReadAllText(LobbyScenePath);
        string gameObjectBlock = FindGameObjectBlockByName(scene, objectName);
        string gameObjectId = ReadObjectId(gameObjectBlock);

        Assert.That(GameObjectHasComponentScript(scene, gameObjectBlock, gameObjectId, LocalizedTmpTextScriptGuid), Is.True);
        Assert.That(FindComponentBlockByScript(scene, gameObjectBlock, gameObjectId, LocalizedTmpTextScriptGuid),
            Does.Contain("localizationKey: " + expectedKey));
    }

    [TestCase("301323235", "ui.lobby.culture.recipe")]
    [TestCase("1806657410", "ui.lobby.culture.material")]
    public void RequestedCultureTankStaticTexts_UseLocalizedTmpText(string gameObjectId, string expectedKey)
    {
        string scene = File.ReadAllText(LobbyScenePath);
        string gameObjectBlock = FindBlock(scene, "--- !u!1 &" + gameObjectId);

        Assert.That(GameObjectHasComponentScript(scene, gameObjectBlock, gameObjectId, LocalizedTmpTextScriptGuid), Is.True);
        Assert.That(FindComponentBlockByScript(scene, gameObjectBlock, gameObjectId, LocalizedTmpTextScriptGuid),
            Does.Contain("localizationKey: " + expectedKey));
    }

    [TestCase("00", 0, "0")]
    [TestCase("00", 17, "17")]
    [TestCase("{0}", 23, "23")]
    public void ErosionScoreFormatter_ReplacesTheNumericTemplate(string template, int score, string expected)
    {
        Assert.That(ErosionScoreTextFormatter.Format(template, score), Is.EqualTo(expected));
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

    private static string[] FindGameObjectBlocksByName(string scene, string objectName)
    {
        var results = new System.Collections.Generic.List<string>();
        foreach (string block in SplitBlocks(scene))
        {
            if (block.StartsWith("!u!1 &", StringComparison.Ordinal) &&
                block.IndexOf("\n  m_Name: " + objectName + "\n", StringComparison.Ordinal) >= 0)
                results.Add(block);
        }

        return results.ToArray();
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

    private static string FindComponentBlockByScript(
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
                return componentBlock;
        }

        return string.Empty;
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
