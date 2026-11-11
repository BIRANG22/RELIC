using NUnit.Framework;
using Relic.Gameplay.Data;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public sealed class MonsterInfoNameAndPassiveActivationTests
{
    [Test]
    public void MonsterSkillEffectTooltip_AlignTopLeft_MatchesRequestedWorldPoint()
    {
        GameObject root = new GameObject("Tooltip", typeof(RectTransform));
        try
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 95f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.position = new Vector3(40f, 20f, 0f);
            Vector3 requestedTopLeft = new Vector3(150f, 275f, 0f);

            MonsterSkillEffectLinkTooltip.AlignTopLeftToWorldPoint(rect, requestedTopLeft);

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Assert.That(corners[1].x, Is.EqualTo(requestedTopLeft.x).Within(0.001f));
            Assert.That(corners[1].y, Is.EqualTo(requestedTopLeft.y).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void MonsterSkillItem_FormatDetail_PreservesBarrierEffectLinkAndUnderline()
    {
        string formatted = MonsterInfoSkillItemUI.FormatDetailForEffectHover(
            "자신에게 <link=\"effecticon:E_Barrier\">장막</link><space=1em>을 부여한다.");

        Assert.That(formatted, Does.Contain("<link=\"effecticon:E_Barrier\"><u>장막</u></link>"));
        Assert.That(formatted, Does.Not.Contain("<space=1em>"));
    }

    [Test]
    public void MonsterSkillItem_FormatDetail_PreservesEveryEffectLink()
    {
        string formatted = MonsterInfoSkillItemUI.FormatDetailForEffectHover(
            "<link=\"effecticon:E_Poison\">중독</link><space=1em>과 " +
            "<link=\"effecticon:E_Bleed\">출혈</link><space=1em>을 부여한다.");

        Assert.That(
            SkillDescriptionFormatter.ExtractLinkedEffectIds(formatted),
            Is.EqualTo(new[] { "E_Poison", "E_Bleed" }));
        Assert.That(formatted, Does.Contain("<u>중독</u>"));
        Assert.That(formatted, Does.Contain("<u>출혈</u>"));
    }

    [TestCase("effecticon:E_Poison", "E_Poison")]
    [TestCase("effecticon:E_Bleed", "E_Bleed")]
    public void MonsterSkillEffectLink_TryParseValidId_ReturnsEffectId(string linkId, string expected)
    {
        bool parsed = MonsterSkillEffectLinkTooltip.TryParseEffectId(linkId, out string effectId);

        Assert.That(parsed, Is.True);
        Assert.That(effectId, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("effecticon:")]
    [TestCase("other:E_Poison")]
    public void MonsterSkillEffectLink_TryParseInvalidId_ReturnsFalse(string linkId)
    {
        Assert.That(
            MonsterSkillEffectLinkTooltip.TryParseEffectId(linkId, out string effectId),
            Is.False);
        Assert.That(effectId, Is.Empty);
    }

    [Test]
    public void MonsterSkillEffectLink_BindEnablesRichTextAndRaycast()
    {
        GameObject root = new GameObject("MonsterSkillDetail", typeof(RectTransform));
        try
        {
            TMP_Text text = root.AddComponent<TextMeshProUGUI>();
            text.richText = false;
            text.raycastTarget = false;
            MonsterSkillEffectLinkTooltip hover = root.AddComponent<MonsterSkillEffectLinkTooltip>();

            hover.Bind(text);

            Assert.That(text.richText, Is.True);
            Assert.That(text.raycastTarget, Is.True);
            Assert.That(hover.enabled, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PlayerSkillHoverDescription_UsesExcelSourceWhenLocalizationIsMissing()
    {
        Assert.That(
            GameDataLocalization.ResolveLocalizedOrSource("미번역", "{E_Strike} {ValueRate1}", "ko"),
            Is.EqualTo("{E_Strike} {ValueRate1}"));
    }

    [Test]
    public void EnglishMissingSkillTranslation_DoesNotFallBackToKoreanSource()
    {
        Assert.That(
            GameDataLocalization.ResolveLocalizedOrSource(
                "Untranslated",
                "한국어 스킬 원문",
                "en"),
            Is.EqualTo("Untranslated"));
    }

    [Test]
    public void KoreanMissingSkillTranslation_UsesKoreanSource()
    {
        Assert.That(
            GameDataLocalization.ResolveLocalizedOrSource(
                "미번역",
                "한국어 스킬 원문",
                "ko"),
            Is.EqualTo("한국어 스킬 원문"));
    }

    [Test]
    public void OptionalStructuredSkillSentence_MissingKeyKeepsEmptyFallback()
    {
        Assert.That(GameLocalization.ResolveMissingTranslation("ko", string.Empty), Is.Empty);
        Assert.That(GameLocalization.ResolveMissingTranslation("en", string.Empty), Is.Empty);
        Assert.That(GameLocalization.ResolveMissingTranslation("ko", null), Is.EqualTo("미번역"));
    }

    [Test]
    public void DynamicSkillTooltipDetail_DisablesStaticLocalizationWriter()
    {
        GameObject root = new GameObject("TooltipDetail", typeof(RectTransform));
        try
        {
            TMP_Text text = root.AddComponent<TextMeshProUGUI>();
            LocalizedTMPText staticWriter = root.AddComponent<LocalizedTMPText>();

            LocalizedTMPText.ProtectDynamicOwnership(text);

            Assert.That(root.GetComponent<LocalizationIgnore>(), Is.Not.Null);
            Assert.That(staticWriter.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void MonsterSkillDescription_ReplacesEffectIdWithInlineEffectLink()
    {
        MonsterSkillData skill = new MonsterSkillData
        {
            EffectIds = "E_Strike",
            ValueRate = "5",
            CountRate = "1"
        };

        string formatted = MonsterSkillDescriptionFormatter.Format(
            "전방에 {E_Strike}를 {ValueRate1}만큼 준다.", skill);

        Assert.That(formatted, Does.Not.Contain("{E_Strike}"));
        Assert.That(formatted, Does.Contain("<link=\"effecticon:E_Strike\">"));
        Assert.That(formatted, Does.Contain("5"));
    }

    [Test]
    public void ResolveSkillName_UsesExcelLoadedNameBeforeSkillId()
    {
        MonsterSkillData skill = new MonsterSkillData
        {
            SkillId = "S_Monster_02",
            Name = "점액투척"
        };

        Assert.That(MonsterInfoDataNameResolver.ResolveSkillName(skill), Is.EqualTo("점액투척"));
    }

    [Test]
    public void ResolveSkillName_FallsBackToSkillIdWhenExcelNameIsBlank()
    {
        MonsterSkillData skill = new MonsterSkillData
        {
            SkillId = "S_Monster_02",
            Name = "  "
        };

        Assert.That(MonsterInfoDataNameResolver.ResolveSkillName(skill), Is.EqualTo("S_Monster_02"));
    }

    [Test]
    public void ResolveEffectName_UsesExcelLoadedNameBeforeEffectId()
    {
        EffectMasterData effect = new EffectMasterData
        {
            EffectId = "E_Poison",
            Name = "중독"
        };

        Assert.That(
            MonsterInfoDataNameResolver.ResolveEffectName(effect, "E_Poison"),
            Is.EqualTo("중독"));
    }

    [Test]
    public void ResolveEffectName_FallsBackToEffectIdWhenDataIsMissing()
    {
        Assert.That(
            MonsterInfoDataNameResolver.ResolveEffectName(null, " E_Unknown "),
            Is.EqualTo("E_Unknown"));
    }

    [Test]
    public void ShouldApplyPassive_ReturnsTrueWhenCurrentKarmaIsZero()
    {
        SkillMasterData passiveSkill = new SkillMasterData { SkillId = "S_Passive_01" };
        CharacterRuntimeData runtime = new CharacterRuntimeData
        {
            CharacterId = "C_01",
            PassiveSkillId = "S_Passive_01",
            MaxHP = 10,
            CurrentHP = 10,
            CurrentResource = 0
        };

        Assert.That(BattlePassiveSkillService.ShouldApplyPassive(passiveSkill, runtime), Is.True);
    }

    [Test]
    public void ShouldApplyPassive_ReturnsFalseWhenCharacterIsDead()
    {
        SkillMasterData passiveSkill = new SkillMasterData { SkillId = "S_Passive_01" };
        CharacterRuntimeData runtime = new CharacterRuntimeData
        {
            CharacterId = "C_01",
            PassiveSkillId = "S_Passive_01",
            MaxHP = 10,
            CurrentHP = 0
        };

        Assert.That(BattlePassiveSkillService.ShouldApplyPassive(passiveSkill, runtime), Is.False);
    }

    [Test]
    public void RefreshRuntimePassiveEffects_DeadCharacter_ClearsOnlyPassiveStatuses()
    {
        CharacterRuntimeData runtime = new CharacterRuntimeData
        {
            CharacterId = "C_01",
            MaxHP = 10,
            CurrentHP = 0,
            StatusEffects = new System.Collections.Generic.List<StatusEffectRuntimeData>
            {
                new StatusEffectRuntimeData("E_Boost", 2) { IsPassive = true },
                new StatusEffectRuntimeData("E_Poison", 3) { IsPassive = false }
            }
        };

        BattlePassiveSkillService.RefreshRuntimePassiveEffects(runtime);

        Assert.That(runtime.StatusEffects, Has.Count.EqualTo(1));
        Assert.That(runtime.StatusEffects[0].EffectId, Is.EqualTo("E_Poison"));
        Assert.That(runtime.StatusEffects[0].IsPassive, Is.False);
    }

    [Test]
    public void PassiveTargetPolicy_EnemyPartyTargetsMonstersInsteadOfOwner()
    {
        Assert.That(
            BattlePassiveTargetPolicy.Resolve(TargetType.EnemyParty),
            Is.EqualTo(BattlePassiveTargetGroup.Monsters));
    }

    [Test]
    public void RuntimeWorkbook_LoadsMonsterSkillAndEffectNamesFromExcelExport()
    {
        byte[] bytes = File.ReadAllBytes("Assets/Resources/Data/GameDataRuntime.csv");
        var workbook = ExcelWorkbookReader.Read(bytes);
        var monsterSkills = MonsterSkillCsvLoader.Load(workbook);
        var effects = EffectCsvLoader.Load(workbook);

        MonsterSkillData skill = monsterSkills.Find(x => x.SkillId == "S_Monster_02");
        EffectMasterData effect = effects.Find(x => x.EffectId == "E_Poison");

        Assert.That(skill, Is.Not.Null);
        Assert.That(skill.Name, Is.EqualTo("점액투척"));
        Assert.That(effect, Is.Not.Null);
        Assert.That(effect.Name, Is.EqualTo("중독"));
    }

    [Test]
    public void BattleRoomLoader_AppliesBattleStartPassivesAfterMonstersSpawn()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleRoomLoader.cs");

        int spawnIndex = source.IndexOf("SpawnMonstersAndHUD(false);", System.StringComparison.Ordinal);
        int passiveIndex = source.IndexOf("RefreshBattleStartPassives();", System.StringComparison.Ordinal);

        Assert.That(spawnIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(passiveIndex, Is.GreaterThan(spawnIndex));
    }

    [Test]
    public void MonsterInfoPrefabs_DoNotContainStaticLocalizationForDynamicFields()
    {
        string skillPrefab = File.ReadAllText("Assets/Project/PrefabsR/MonsterSkill.prefab");
        string statusPrefab = File.ReadAllText("Assets/Project/PrefabsR/MonsterStatus.prefab");

        Assert.That(skillPrefab, Does.Not.Contain("ui.monsterskill.name."));
        Assert.That(skillPrefab, Does.Not.Contain("ui.monsterskill.detail."));
        Assert.That(skillPrefab, Does.Not.Contain("ui.monsterskill.typetext."));
        Assert.That(statusPrefab, Does.Not.Contain("ui.monsterstatus.name."));
        Assert.That(statusPrefab, Does.Not.Contain("ui.monsterstatus.detail."));
    }

    [Test]
    public void BattleScene_MonsterInfoListReferencesRemainConnected()
    {
        const string scenePath = "Assets/Project/Scenes/YDM/Battle.unity";
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;

        if (openedForTest)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        try
        {
            BattleMonsterInfoCanvasUI canvas = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                canvas = root.GetComponentInChildren<BattleMonsterInfoCanvasUI>(true);
                if (canvas != null)
                    break;
            }

            Assert.That(canvas, Is.Not.Null);

            SerializedObject serializedCanvas = new SerializedObject(canvas);
            Assert.That(
                serializedCanvas.FindProperty("skillContent").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                serializedCanvas.FindProperty("monsterSkillPrefab").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                serializedCanvas.FindProperty("statusContent").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                serializedCanvas.FindProperty("monsterStatusPrefab").objectReferenceValue,
                Is.Not.Null);
        }
        finally
        {
            if (openedForTest && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void MonsterInfoPrefabs_HaveRequiredInspectorReferences()
    {
        MonsterInfoSkillItemUI skillItem = UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterInfoSkillItemUI>(
            "Assets/Project/PrefabsR/MonsterSkill.prefab");
        MonsterInfoStatusItemUI statusItem = UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterInfoStatusItemUI>(
            "Assets/Project/PrefabsR/MonsterStatus.prefab");

        Assert.That(skillItem, Is.Not.Null);
        Assert.That(statusItem, Is.Not.Null);

        AssertSerializedReference(skillItem, "skillIconImage");
        AssertSerializedReference(skillItem, "nameText");
        AssertSerializedReference(skillItem, "detailText");
        AssertSerializedReference(skillItem, "rangeImage");
        AssertSerializedReference(skillItem, "typeText");

        AssertSerializedReference(statusItem, "statusIconImage");
        AssertSerializedReference(statusItem, "nameText");
        AssertSerializedReference(statusItem, "detailText");
    }

    [Test]
    public void MonsterInfoPrefabs_DoNotExposePlaceholderNamesBeforeBinding()
    {
        MonsterInfoSkillItemUI skillItem = UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterInfoSkillItemUI>(
            "Assets/Project/PrefabsR/MonsterSkill.prefab");
        MonsterInfoStatusItemUI statusItem = UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterInfoStatusItemUI>(
            "Assets/Project/PrefabsR/MonsterStatus.prefab");

        TMP_Text skillName = GetSerializedReference<TMP_Text>(skillItem, "nameText");
        TMP_Text statusName = GetSerializedReference<TMP_Text>(statusItem, "nameText");

        Assert.That(skillName.text, Is.Empty);
        Assert.That(statusName.text, Is.Empty);
    }

    private static void AssertSerializedReference(Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        Assert.That(property.objectReferenceValue, Is.Not.Null, propertyName);
    }

    private static T GetSerializedReference<T>(Object target, string propertyName)
        where T : Object
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        return property.objectReferenceValue as T;
    }
}
