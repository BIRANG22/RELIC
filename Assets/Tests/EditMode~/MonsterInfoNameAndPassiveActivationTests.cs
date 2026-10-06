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
            CurrentResource = 0
        };

        Assert.That(BattlePassiveSkillService.ShouldApplyPassive(passiveSkill, runtime), Is.True);
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
