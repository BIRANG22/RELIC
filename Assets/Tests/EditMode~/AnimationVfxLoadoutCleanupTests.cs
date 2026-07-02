using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimationVfxLoadoutCleanupTests
{
    [Test]
    public void LoadoutWrapperTypes_AreRemoved()
    {
        Assert.That(FindType("Relic.Gameplay.Data.CharacterSkillLoadout"), Is.Null);
        Assert.That(FindType("Relic.Gameplay.Data.CharacterRuneLoadout"), Is.Null);
        Assert.That(FindType("Relic.Gameplay.Data.MonsterSkillLoadoutData"), Is.Null);
    }

    [Test]
    public void CharacterEquipmentManager_WritesDirectEquipmentFields()
    {
        CharacterEquipmentManager manager = new();

        manager.EquipPassive("C_Test", "Passive_01");
        manager.EquipUnique("C_Test", "Unique_01");
        manager.EquipAbility("C_Test", "Ability_01");
        manager.EquipFreeSkill("C_Test", 1, "Free_02");
        manager.EquipRune("C_Test", 4, "Rune_05");
        manager.EquipFragment("C_Test", 3, "Fragment_04");

        CharacterEquipmentData equipment = manager.GetOrCreate("C_Test");

        Assert.That(equipment.PassiveSkillId, Is.EqualTo("Passive_01"));
        Assert.That(equipment.UniqueSkillId, Is.EqualTo("Unique_01"));
        Assert.That(equipment.AbilitySkillId, Is.EqualTo("Ability_01"));
        Assert.That(equipment.FreeSkillIds, Has.Length.EqualTo(2));
        Assert.That(equipment.FreeSkillIds[1], Is.EqualTo("Free_02"));
        Assert.That(equipment.RuneIds, Has.Length.EqualTo(5));
        Assert.That(equipment.RuneIds[4], Is.EqualTo("Rune_05"));
        Assert.That(equipment.FragmentIds, Has.Length.EqualTo(4));
        Assert.That(equipment.FragmentIds[3], Is.EqualTo("Fragment_04"));
    }

    [Test]
    public void BattleUnitActionPresentation_UsesSingleFrameFieldsOnly()
    {
        Type presentationType = typeof(BattleUnitActionPresentation);

        Assert.That(presentationType.GetField("stateName"), Is.Not.Null);
        Assert.That(presentationType.GetField("vfx"), Is.Not.Null);
        Assert.That(presentationType.GetField("readyStateName"), Is.Null);
        Assert.That(presentationType.GetField("readyVfx"), Is.Null);
        Assert.That(presentationType.GetField("actionStateName"), Is.Null);
        Assert.That(presentationType.GetField("actionVfx"), Is.Null);
    }

    [Test]
    public void BattleActionRunner_DoesNotKeepReadyDelayStage()
    {
        FieldInfo readyDelayField = typeof(BattleActionRunner).GetField(
            "ReadyDelay",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(readyDelayField, Is.Null);
    }

    [Test]
    public void BattleUnitAnimator_GroupsPlayerSkillPresentationsAndRemovesLegacyAttackFields()
    {
        Type animatorType = typeof(BattleUnitAnimator);
        Type playerPresentationsType = typeof(BattleUnitPlayerSkillPresentations);

        Assert.That(animatorType.GetField("playerSkillPresentations", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(playerPresentationsType.GetField("power"), Is.Not.Null);
        Assert.That(playerPresentationsType.GetField("attack1"), Is.Not.Null);
        Assert.That(playerPresentationsType.GetField("attack2"), Is.Not.Null);
        Assert.That(playerPresentationsType.GetField("attack3"), Is.Not.Null);
        Assert.That(playerPresentationsType.GetField("skill"), Is.Not.Null);

        Assert.That(animatorType.GetField("playerPowerPresentation", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(animatorType.GetField("playerSkillPresentation", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(animatorType.GetField("attackReady1StateName", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(animatorType.GetField("attackAction1StateName", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(animatorType.GetField("attackVfx1", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
    }

    [Test]
    public void BattleUnitAnimator_UsesEffectSpecificStatusVfxSet()
    {
        Type animatorType = typeof(BattleUnitAnimator);
        Type statusVfxType = typeof(BattleStatusVfxSet);

        Assert.That(animatorType.GetField("statusVfx", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(statusVfxType.GetField("powerVfx"), Is.Not.Null);
        Assert.That(statusVfxType.GetField("weakenVfx"), Is.Not.Null);

        Assert.That(animatorType.GetField("buffVfx", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(animatorType.GetField("debuffVfx", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
    }

    [Test]
    public void MonsterMasterData_NormalizesPossibleSkillSlotsAndPreservesActionIndex()
    {
        MonsterMasterData master = new()
        {
            MonsterId = "M_Slots",
            HP = 10,
            PossSkillId01 = "S_Monster_A",
            PossSkillId02 = "0",
            PossSkillId03 = "",
            PossSkillId04 = null,
            PossSkillId05 = "   ",
            PossSkillId10 = "S_Monster_J"
        };

        Assert.That(master.GetPossibleSkillIds(), Is.EqualTo(new[] { "S_Monster_A", "S_Monster_J" }));
        Assert.That(
            master.GetPossibleSkillIdSlots(),
            Is.EqualTo(new[] { "S_Monster_A", "", "", "", "", "", "", "", "", "S_Monster_J" }));
        Assert.That(master.GetPossibleSkillIdAtActionIndex(-1), Is.EqualTo(""));
        Assert.That(master.GetPossibleSkillIdAtActionIndex(0), Is.EqualTo(""));
        Assert.That(master.GetPossibleSkillIdAtActionIndex(1), Is.EqualTo("S_Monster_A"));
        Assert.That(master.GetPossibleSkillIdAtActionIndex(10), Is.EqualTo("S_Monster_J"));
        Assert.That(master.GetPossibleSkillIdAtActionIndex(11), Is.EqualTo(""));
        Assert.That(master.GetActionIndexForSkill("S_Monster_J"), Is.EqualTo(10));
        Assert.That(master.GetActionIndexForSkill("Missing"), Is.EqualTo(0));
    }

    [Test]
    public void MonsterRuntimeData_CopiesPossibleSkillSlotsFromMaster()
    {
        MonsterMasterData master = new()
        {
            MonsterId = "M_Runtime_Slots",
            Name = "Runtime Slots",
            HP = 10,
            PossSkillId01 = "S_Monster_A",
            PossSkillId05 = "S_Monster_E",
            PossSkillId10 = "0"
        };

        MonsterRuntimeData runtime = new("Runtime_01", master);

        Assert.That(runtime.PossSkillIds, Is.EqualTo(new[] { "S_Monster_A", "S_Monster_E" }));
        Assert.That(runtime.GetActionIndexForSkill("S_Monster_E"), Is.EqualTo(5));
    }

    [Test]
    public void MonsterRuntimeData_NullMasterCreatesEmptyPossibleSkillSlots()
    {
        MonsterRuntimeData runtime = new("Runtime_Null", null);

        Assert.That(runtime.RuntimeId, Is.EqualTo("Runtime_Null"));
        Assert.That(runtime.PossibleSkillIdsByActionIndex, Has.Length.EqualTo(MonsterMasterData.PossibleSkillSlotCount));
        Assert.That(runtime.PossibleSkillIdsByActionIndex, Is.All.EqualTo(""));
        Assert.That(runtime.PossSkillIds, Is.Empty);
        Assert.That(runtime.GetActionIndexForSkill("AnySkill"), Is.EqualTo(0));
    }

    [Test]
    public void MonsterReservedCommand_ResolvesActionIndexFromRuntimeSkillSlots()
    {
        MonsterMasterData master = new()
        {
            MonsterId = "M_Command_Slots",
            Name = "Command Slots",
            HP = 10,
            PossSkillId04 = "S_Monster_Action04"
        };
        MonsterRuntimeData runtime = new("Runtime_Command", master);
        MonsterSkillData skill = new() { SkillId = "S_Monster_Action04" };

        MonsterReservedCommand command = new(runtime, skill);

        Assert.That(command.ActionIndex, Is.EqualTo(4));
    }

    [Test]
    public void MonsterReservedCommand_SetActionIndexClampsToPossibleSkillSlots()
    {
        MonsterRuntimeData runtime = new("Runtime_Command", new MonsterMasterData());
        MonsterReservedCommand command = new(runtime, new MonsterSkillData());

        command.SetActionIndex(-1);
        Assert.That(command.ActionIndex, Is.EqualTo(0));

        command.SetActionIndex(MonsterMasterData.PossibleSkillSlotCount + 1);
        Assert.That(command.ActionIndex, Is.EqualTo(MonsterMasterData.PossibleSkillSlotCount));
    }

    [Test]
    public void BattleUnitAnimator_PlayerPowerActionSpawnsPowerVfx()
    {
        GameObject owner = new("AnimatorOwner");
        GameObject powerPrefab = new("PowerVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                power = new BattleUnitActionPresentation
                {
                    stateName = "",
                    vfx = new BattleVfxEntry { prefab = powerPrefab, flipType = VfxFlipType.None }
                }
            });

            animator.PlaySkillAction(new SkillMasterData { SkillId = "S_Power", SkillType = SkillType.Power });

            Assert.That(owner.transform.Find("PowerVfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(powerPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_PlayerPowerReadyDoesNotSpawnPresentationVfx()
    {
        GameObject owner = new("AnimatorOwner");
        GameObject powerPrefab = new("PowerReadyVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                power = new BattleUnitActionPresentation
                {
                    stateName = "",
                    vfx = new BattleVfxEntry { prefab = powerPrefab, flipType = VfxFlipType.None }
                }
            });

            animator.PlaySkillReady(new SkillMasterData { SkillId = "S_Power", SkillType = SkillType.Power });

            Assert.That(owner.transform.Find("PowerReadyVfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(powerPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_PlayerAttackActionSpawnsGroupedAttackVfx()
    {
        GameObject owner = new("AnimatorOwner");
        GameObject attackPrefab = new("PlayerAttack1Vfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                attack1 = new BattleUnitActionPresentation
                {
                    stateName = "",
                    vfx = new BattleVfxEntry { prefab = attackPrefab, flipType = VfxFlipType.None }
                }
            });

            animator.PlayAttackAction(1);

            Assert.That(owner.transform.Find("PlayerAttack1Vfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(attackPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_MonsterCommandActionSpawnsMatchingActionVfx()
    {
        GameObject owner = new("MonsterAnimatorOwner");
        GameObject action4Prefab = new("MonsterAction4Vfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            BattleUnitActionPresentation[] slots = BattleUnitActionPresentation.CreateArray(10);
            slots[3].stateName = "";
            slots[3].vfx = new BattleVfxEntry { prefab = action4Prefab, flipType = VfxFlipType.None };
            SetPrivateField(animator, "monsterActionPresentations", slots);

            MonsterMasterData master = new()
            {
                MonsterId = "M_Action4",
                HP = 10,
                PossSkillId04 = "S_Monster_Action4"
            };
            MonsterRuntimeData runtime = new("Runtime_Action4", master);
            MonsterReservedCommand command = new(runtime, new MonsterSkillData { SkillId = "S_Monster_Action4" });

            animator.PlayMonsterSkillAction(command);

            Assert.That(owner.transform.Find("MonsterAction4Vfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(action4Prefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_MonsterCommandActionRecognizesMappedProjectileVfx()
    {
        GameObject owner = new("MonsterProjectileAnimatorOwner");
        GameObject missilePrefab = new("MonsterProjectileMissileVfx");
        GameObject impactPrefab = new("MonsterProjectileImpactVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            BattleUnitActionPresentation[] slots = BattleUnitActionPresentation.CreateArray(10);
            slots[1].projectileVfx = new BattleProjectileVfxEntry
            {
                skillId = "S_Monster_Projectile",
                missilePrefab = missilePrefab,
                impactPrefab = impactPrefab,
                travelDuration = 0.01f
            };
            SetPrivateField(animator, "monsterActionPresentations", slots);

            MonsterMasterData master = new()
            {
                MonsterId = "M_Projectile",
                HP = 10,
                PossSkillId02 = "S_Monster_Projectile"
            };
            MonsterRuntimeData runtime = new("Runtime_Projectile", master);
            MonsterReservedCommand command = new(runtime, new MonsterSkillData { SkillId = "S_Monster_Projectile" });

            Assert.That(animator.HasMonsterProjectileVfx(command), Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(missilePrefab);
            UnityEngine.Object.DestroyImmediate(impactPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_MonsterCommandProjectileVfxCanBeMatchedBySkillId()
    {
        GameObject owner = new("MonsterProjectileSkillIdOwner");
        GameObject missilePrefab = new("MonsterSkillIdProjectileMissileVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            BattleUnitActionPresentation[] slots = BattleUnitActionPresentation.CreateArray(10);
            slots[1].projectileVfx = new BattleProjectileVfxEntry
            {
                skillId = "S_Monster_Projectile",
                missilePrefab = missilePrefab
            };
            SetPrivateField(animator, "monsterActionPresentations", slots);

            MonsterRuntimeData runtime = new(
                "Runtime_Projectile_Unmapped",
                new MonsterMasterData
                {
                    MonsterId = "M_Projectile_Unmapped",
                    HP = 10
                });
            MonsterReservedCommand command = new(runtime, new MonsterSkillData { SkillId = "S_Monster_Projectile" });

            Assert.That(command.ActionIndex, Is.EqualTo(0));
            Assert.That(animator.HasMonsterProjectileVfx(command), Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(missilePrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_ProjectileImpactKeepsLaunchZFor2D()
    {
        MethodInfo method = typeof(BattleUnitAnimator).GetMethod(
            "ResolveProjectileImpactPosition",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);

        Vector3 result = (Vector3)method.Invoke(
            null,
            new object[]
            {
                new Vector3(5f, 2f, 99f),
                new Vector3(0.25f, -0.5f, 7f),
                -3f
            });

        Assert.That(result, Is.EqualTo(new Vector3(5.25f, 1.5f, -3f)));
    }

    [Test]
    public void BattleUnitAnimator_ProjectileImpactUsesVfxSpawnPointRotation()
    {
        GameObject owner = new("ProjectileImpactSpawnPointOwner");
        GameObject spawnPoint = new("VfxSpawnPoint");
        GameObject impactPrefab = new("ProjectileImpactSpawnPointVfx");

        try
        {
            spawnPoint.transform.SetParent(owner.transform, false);
            spawnPoint.transform.localRotation = Quaternion.Euler(0f, 0f, 37f);

            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "vfxSpawnPoint", spawnPoint.transform);

            MethodInfo method = typeof(BattleUnitAnimator).GetMethod(
                "SpawnImpactVfx",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);

            Vector3 impactPosition = new(4f, 5f, -2f);
            BattleProjectileVfxEntry entry = new()
            {
                impactPrefab = impactPrefab,
                impactLifeTime = 2f
            };

            method.Invoke(animator, new object[] { entry, impactPosition });

            GameObject spawned = GameObject.Find("ProjectileImpactSpawnPointVfx(Clone)");

            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.transform.position, Is.EqualTo(impactPosition));
            Assert.That(spawned.transform.eulerAngles.z, Is.EqualTo(37f).Within(0.01f));

            UnityEngine.Object.DestroyImmediate(spawned);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(impactPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleVfxCameraSyncCopiesSourceCameraButPreservesVfxOutput()
    {
        GameObject sourceObject = new("MainCameraSource");
        GameObject targetObject = new("VfxCameraTarget");
        RenderTexture targetTexture = new(64, 64, 0);

        try
        {
            Camera sourceCamera = sourceObject.AddComponent<Camera>();
            Camera targetCamera = targetObject.AddComponent<Camera>();
            BattleVfxCameraSync sync = targetObject.AddComponent<BattleVfxCameraSync>();

            sourceCamera.transform.position = new Vector3(1.25f, -2.5f, -17f);
            sourceCamera.transform.rotation = Quaternion.Euler(5f, 10f, 15f);
            sourceCamera.orthographic = false;
            sourceCamera.fieldOfView = 31f;
            sourceCamera.orthographicSize = 4.25f;
            sourceCamera.nearClipPlane = 0.2f;
            sourceCamera.farClipPlane = 250f;
            sourceCamera.rect = new Rect(0.1f, 0.2f, 0.7f, 0.6f);

            targetCamera.orthographic = true;
            targetCamera.fieldOfView = 20f;
            targetCamera.orthographicSize = 8f;
            targetCamera.cullingMask = 1 << 9;
            targetCamera.targetTexture = targetTexture;
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            targetCamera.depth = 99f;

            SetPrivateField(sync, "sourceCamera", sourceCamera);
            SetPrivateField(sync, "targetCamera", targetCamera);

            sync.SyncNow();

            Assert.That(targetCamera.transform.position, Is.EqualTo(sourceCamera.transform.position));
            Assert.That(Quaternion.Angle(targetCamera.transform.rotation, sourceCamera.transform.rotation), Is.LessThan(0.01f));
            Assert.That(targetCamera.orthographic, Is.EqualTo(sourceCamera.orthographic));
            Assert.That(targetCamera.fieldOfView, Is.EqualTo(sourceCamera.fieldOfView));
            Assert.That(targetCamera.orthographicSize, Is.EqualTo(sourceCamera.orthographicSize));
            Assert.That(targetCamera.nearClipPlane, Is.EqualTo(sourceCamera.nearClipPlane));
            Assert.That(targetCamera.farClipPlane, Is.EqualTo(sourceCamera.farClipPlane));
            Assert.That(targetCamera.rect, Is.EqualTo(sourceCamera.rect));
            Assert.That(targetCamera.cullingMask, Is.EqualTo(1 << 9));
            Assert.That(targetCamera.targetTexture, Is.SameAs(targetTexture));
            Assert.That(targetCamera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
            Assert.That(targetCamera.backgroundColor, Is.EqualTo(new Color(0f, 0f, 0f, 0f)));
            Assert.That(targetCamera.depth, Is.EqualTo(99f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(targetTexture);
            UnityEngine.Object.DestroyImmediate(targetObject);
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

#if UNITY_EDITOR
    [Test]
    public void MuckProjectileImpactRootUsesSimpleGlowMaterial()
    {
        const string ImpactPrefabPath =
            "Assets/Project/Art/testYDM/VFXtest/Mon/Vfx_Mon_N_01_attack_01.prefab";
        const string SimpleGlowGuid = "dbdd7248b23714e4a860091a66653785";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImpactPrefabPath);

        Assert.That(prefab, Is.Not.Null);

        ParticleSystemRenderer rootRenderer = prefab.GetComponent<ParticleSystemRenderer>();

        Assert.That(rootRenderer, Is.Not.Null);
        Assert.That(rootRenderer.sharedMaterial, Is.Not.Null);
        Assert.That(
            AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(rootRenderer.sharedMaterial)),
            Is.EqualTo(SimpleGlowGuid));
    }

    [Test]
    public void MuckProjectileMissileEnabledRenderersDoNotUseNullMaterialSlots()
    {
        const string MissilePrefabPath =
            "Assets/Project/Art/testYDM/VFXtest/Mon/Vfx_Mon_N_01_attack_01_missile.prefab";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MissilePrefabPath);

        Assert.That(prefab, Is.Not.Null);

        string[] rendererNamesWithNullMaterials = prefab
            .GetComponentsInChildren<ParticleSystemRenderer>(true)
            .Where(renderer => renderer.enabled && renderer.gameObject.activeSelf)
            .Where(renderer => renderer.sharedMaterials.Any(material => material == null))
            .Select(renderer => renderer.name)
            .ToArray();

        Assert.That(rendererNamesWithNullMaterials, Is.Empty);
    }

    [Test]
    public void MuckProjectileVfxUsesParticleRendererFlipInsteadOfRootScaleFlip()
    {
        const string MuckPrefabPath = "Assets/Project/PrefabsR/Monster/Muck/Slime.prefab";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MuckPrefabPath);

        Assert.That(prefab, Is.Not.Null);

        BattleUnitAnimator animator = prefab.GetComponent<BattleUnitAnimator>();

        Assert.That(animator, Is.Not.Null);

        FieldInfo field = typeof(BattleUnitAnimator).GetField(
            "monsterActionPresentations",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);

        BattleUnitActionPresentation[] presentations =
            (BattleUnitActionPresentation[])field.GetValue(animator);

        BattleProjectileVfxEntry projectileVfx = presentations
            .Select(presentation => presentation?.projectileVfx)
            .FirstOrDefault(entry => entry != null && entry.skillId == "S_Monster_04");

        Assert.That(projectileVfx, Is.Not.Null);
        Assert.That(projectileVfx.missileFlipType, Is.EqualTo(VfxFlipType.ParticleRendererFlipY));
        Assert.That(projectileVfx.impactFlipType, Is.EqualTo(VfxFlipType.ParticleRendererFlipY));
    }

    [Test]
    public void MonsterBattlePrefabs_AssignStatusVfxUsedByPlayerBattlePrefabs()
    {
        const string MonsterPrefabRoot = "Assets/Project/PrefabsR/Monster";

        string[] requiredEffectIds =
        {
            "E_Aiming",
            "E_Armor",
            "E_Focus",
            "E_Power",
            "E_Recharge",
            "E_Recover",
            "E_Swift",
            "E_Thorns",
            "E_Addicted",
            "E_Bleeding",
            "E_Burn",
            "E_Corrosion",
            "E_Grudge",
            "E_Vulnerable",
            "E_Weaken"
        };

        string[] prefabPaths = AssetDatabase
            .FindAssets("t:Prefab", new[] { MonsterPrefabRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .ToArray();

        Assert.That(prefabPaths, Is.Not.Empty);

        string[] missingEntries = prefabPaths
            .Select(path => new
            {
                Path = path,
                Animator = AssetDatabase.LoadAssetAtPath<GameObject>(path)
                    ?.GetComponentInChildren<BattleUnitAnimator>(true)
            })
            .Where(entry => entry.Animator != null)
            .SelectMany(entry => requiredEffectIds
                .Where(effectId => GetStatusVfx(entry.Animator, effectId)?.prefab == null)
                .Select(effectId => $"{entry.Path}:{effectId}"))
            .ToArray();

        Assert.That(missingEntries, Is.Empty);
    }

    [Test]
    public void BattleVfxCameraClearsRenderTextureWithTransparentColor()
    {
        const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";
        const string VfxRenderTextureGuid = "f8cac70b5bbf43d449f6cf54ffa93abc";

        string sceneYaml = File.ReadAllText(BattleScenePath);
        int nameIndex = sceneYaml.IndexOf("m_Name: VFXCamera", StringComparison.Ordinal);

        Assert.That(nameIndex, Is.GreaterThanOrEqualTo(0));

        int cameraIndex = sceneYaml.IndexOf("Camera:", nameIndex, StringComparison.Ordinal);
        int transformIndex = sceneYaml.IndexOf("--- !u!4", cameraIndex, StringComparison.Ordinal);

        Assert.That(cameraIndex, Is.GreaterThan(nameIndex));
        Assert.That(transformIndex, Is.GreaterThan(cameraIndex));

        string cameraBlock = sceneYaml.Substring(cameraIndex, transformIndex - cameraIndex);

        Assert.That(cameraBlock, Does.Contain("m_ClearFlags: 2"));
        Assert.That(cameraBlock, Does.Contain("m_BackGroundColor: {r: 0, g: 0, b: 0, a: 0}"));
        Assert.That(cameraBlock, Does.Contain($"guid: {VfxRenderTextureGuid}"));
    }

    [Test]
    public void BattleVfxRawImageUsesAdditiveRenderTextureMaterial()
    {
        const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";
        const string VfxRenderTextureGuid = "f8cac70b5bbf43d449f6cf54ffa93abc";
        const string AdditiveMaterialGuid = "4cb96efc760942549b8609077d6f61fa";

        string sceneYaml = File.ReadAllText(BattleScenePath);
        string textureNeedle =
            $"m_Texture: {{fileID: 8400000, guid: {VfxRenderTextureGuid}, type: 2}}";
        int textureIndex = sceneYaml.IndexOf(textureNeedle, StringComparison.Ordinal);

        Assert.That(textureIndex, Is.GreaterThanOrEqualTo(0));

        int materialIndex = sceneYaml.LastIndexOf(
            "m_Material:",
            textureIndex,
            StringComparison.Ordinal);

        Assert.That(materialIndex, Is.GreaterThanOrEqualTo(0));

        string rawImageMaterialLine = sceneYaml.Substring(
            materialIndex,
            sceneYaml.IndexOf('\n', materialIndex) - materialIndex);

        Assert.That(rawImageMaterialLine, Does.Contain($"guid: {AdditiveMaterialGuid}"));
    }

    [Test]
    public void BattleVfxCameraHasMainCameraSyncComponent()
    {
        const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";
        const string VfxCameraSyncGuid = "edfd70dcf5b9445c819d92a30cd29470";
        const string VfxCameraFileId = "147280529";
        const string VfxCameraComponentFileId = "147280534";

        string sceneYaml = File.ReadAllText(BattleScenePath);
        string gameObjectNeedle = $"--- !u!1 &{VfxCameraFileId}";
        int gameObjectIndex = sceneYaml.IndexOf(gameObjectNeedle, StringComparison.Ordinal);

        Assert.That(gameObjectIndex, Is.GreaterThanOrEqualTo(0));

        int nextObjectIndex = sceneYaml.IndexOf("--- !u!", gameObjectIndex + gameObjectNeedle.Length, StringComparison.Ordinal);
        string gameObjectBlock = sceneYaml.Substring(gameObjectIndex, nextObjectIndex - gameObjectIndex);

        Assert.That(gameObjectBlock, Does.Contain($"- component: {{fileID: {VfxCameraComponentFileId}}}"));

        string componentNeedle = $"--- !u!114 &{VfxCameraComponentFileId}";
        int componentIndex = sceneYaml.IndexOf(componentNeedle, StringComparison.Ordinal);

        Assert.That(componentIndex, Is.GreaterThanOrEqualTo(0));

        int nextComponentIndex = sceneYaml.IndexOf("--- !u!", componentIndex + componentNeedle.Length, StringComparison.Ordinal);
        string componentBlock = sceneYaml.Substring(componentIndex, nextComponentIndex - componentIndex);

        Assert.That(componentBlock, Does.Contain($"guid: {VfxCameraSyncGuid}"));
        Assert.That(componentBlock, Does.Contain("sourceCamera: {fileID: 1610817905}"));
        Assert.That(componentBlock, Does.Contain("targetCamera: {fileID: 147280532}"));
    }
#endif

    [Test]
    public void BattleUnitAnimator_MonsterCommandReadyDoesNotSpawnPresentationVfx()
    {
        GameObject owner = new("MonsterAnimatorOwner");
        GameObject action4Prefab = new("MonsterAction4ReadyVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            BattleUnitActionPresentation[] slots = BattleUnitActionPresentation.CreateArray(10);
            slots[3].stateName = "";
            slots[3].vfx = new BattleVfxEntry { prefab = action4Prefab, flipType = VfxFlipType.None };
            SetPrivateField(animator, "monsterActionPresentations", slots);

            MonsterMasterData master = new()
            {
                MonsterId = "M_Action4",
                HP = 10,
                PossSkillId04 = "S_Monster_Action4"
            };
            MonsterRuntimeData runtime = new("Runtime_Action4", master);
            MonsterReservedCommand command = new(runtime, new MonsterSkillData { SkillId = "S_Monster_Action4" });

            animator.PlayMonsterSkillReady(command);

            Assert.That(owner.transform.Find("MonsterAction4ReadyVfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(action4Prefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_MonsterCommandActionWithUnmappedActionIndexDoesNotSpawnSlot1Vfx()
    {
        GameObject owner = new("MonsterAnimatorOwner");
        GameObject action1Prefab = new("MonsterAction1Vfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();

            BattleUnitActionPresentation[] slots = BattleUnitActionPresentation.CreateArray(10);
            slots[0].stateName = "";
            slots[0].vfx = new BattleVfxEntry { prefab = action1Prefab, flipType = VfxFlipType.None };
            SetPrivateField(animator, "monsterActionPresentations", slots);

            MonsterMasterData master = new()
            {
                MonsterId = "M_Unmapped",
                HP = 10
            };
            MonsterRuntimeData runtime = new("Runtime_Unmapped", master);
            MonsterReservedCommand command = new(
                runtime,
                new MonsterSkillData
                {
                    SkillId = "S_Monster_Unmapped",
                    TimelineNotation = TimelineActionType.Attack
                });

            Assert.That(command.ActionIndex, Is.EqualTo(0));

            animator.PlayMonsterSkillAction(command);

            Assert.That(owner.transform.Find("MonsterAction1Vfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(action1Prefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_MonsterCommandActionDoesNotUsePlayerAttackVfx()
    {
        GameObject owner = new("MonsterAnimatorOwner");
        GameObject playerAttackPrefab = new("PlayerOnlyAttackVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "playerSkillPresentations", new BattleUnitPlayerSkillPresentations
            {
                attack1 = new BattleUnitActionPresentation
                {
                    stateName = "",
                    vfx = new BattleVfxEntry { prefab = playerAttackPrefab, flipType = VfxFlipType.None }
                }
            });

            MonsterRuntimeData runtime = new("Runtime_Unmapped", new MonsterMasterData { HP = 10 });
            MonsterReservedCommand command = new(
                runtime,
                new MonsterSkillData
                {
                    SkillId = "S_Monster_Unmapped",
                    TimelineNotation = TimelineActionType.Attack
                });

            animator.PlayMonsterSkillAction(command);

            Assert.That(owner.transform.Find("PlayerOnlyAttackVfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(playerAttackPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void BattleUnitAnimator_PlayBuffAndDebuffDoNotSpawnStatusVfx()
    {
        GameObject owner = new("StatusUseOwner");
        GameObject buffPrefab = new("UseBuffVfx");
        GameObject debuffPrefab = new("UseDebuffVfx");

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                powerVfx = new BattleVfxEntry { prefab = buffPrefab, flipType = VfxFlipType.None },
                weakenVfx = new BattleVfxEntry { prefab = debuffPrefab, flipType = VfxFlipType.None }
            });

            animator.PlayBuff();
            animator.PlayDebuff();

            Assert.That(owner.transform.Find("UseBuffVfx(Clone)"), Is.Null);
            Assert.That(owner.transform.Find("UseDebuffVfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(buffPrefab);
            UnityEngine.Object.DestroyImmediate(debuffPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void AddStatusToPlayer_SpawnsBuffVfxOnTarget()
    {
        GameObject owner = new("PlayerStatusTarget");
        GameObject buffPrefab = new("BuffStatusVfx");

        try
        {
            BattleCharacter character = owner.AddComponent<BattleCharacter>();
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                powerVfx = new BattleVfxEntry { prefab = buffPrefab, flipType = VfxFlipType.None }
            });

            character.Initialize(new CharacterRuntimeData
            {
                CharacterId = "C_Status",
                MaxHP = 10,
                CurrentHP = 10
            });

            BattleEffectUtility.AddStatusToPlayer(character, "E_Power", 1, 1);

            Assert.That(owner.transform.Find("BuffStatusVfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(buffPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void AddStatusToPlayer_DoesNotSpawnVfxWhenStatusListIsMissing()
    {
        GameObject owner = new("PlayerMissingStatusListTarget");
        GameObject buffPrefab = new("MissingListBuffStatusVfx");

        try
        {
            BattleCharacter character = owner.AddComponent<BattleCharacter>();
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                powerVfx = new BattleVfxEntry { prefab = buffPrefab, flipType = VfxFlipType.None }
            });

            character.Initialize(new CharacterRuntimeData
            {
                CharacterId = "C_Status_NullList",
                MaxHP = 10,
                CurrentHP = 10,
                StatusEffects = null
            });

            BattleEffectUtility.AddStatusToPlayer(character, "E_Power", 1, 1);

            Assert.That(owner.transform.Find("MissingListBuffStatusVfx(Clone)"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(buffPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void AddStatusToMonster_SpawnsDebuffVfxOnTarget()
    {
        GameObject owner = new("MonsterStatusTarget");
        GameObject debuffPrefab = new("DebuffStatusVfx");

        try
        {
            MonsterUnit monster = owner.AddComponent<MonsterUnit>();
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                weakenVfx = new BattleVfxEntry { prefab = debuffPrefab, flipType = VfxFlipType.None }
            });

            MonsterRuntimeData runtime = new(
                "Runtime_Status",
                new MonsterMasterData
                {
                    MonsterId = "M_Status",
                    Name = "StatusMonster",
                    HP = 10
                });

            monster.Initialize(runtime);

            BattleEffectUtility.AddStatusToMonster(monster, "E_Weaken", 1, 1);

            Assert.That(owner.transform.Find("DebuffStatusVfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(debuffPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void AddStatusToMonster_UsesChildAnimatorForStatusVfx()
    {
        GameObject owner = new("MonsterStatusRoot");
        GameObject visual = new("MonsterVisual");
        GameObject debuffPrefab = new("ChildDebuffStatusVfx");

        try
        {
            visual.transform.SetParent(owner.transform, false);

            MonsterUnit monster = owner.AddComponent<MonsterUnit>();
            BattleUnitAnimator animator = visual.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                weakenVfx = new BattleVfxEntry { prefab = debuffPrefab, flipType = VfxFlipType.None }
            });

            MonsterRuntimeData runtime = new(
                "Runtime_Status_ChildAnimator",
                new MonsterMasterData
                {
                    MonsterId = "M_Status_ChildAnimator",
                    Name = "StatusChildAnimatorMonster",
                    HP = 10
                });

            monster.Initialize(runtime);

            BattleEffectUtility.AddStatusToMonster(monster, "E_Weaken", 1, 1);

            Assert.That(visual.transform.Find("ChildDebuffStatusVfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(debuffPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void AddShieldToPlayer_SpawnsArmorVfxWhenShieldIncreases()
    {
        GameObject owner = new("PlayerArmorTarget");
        GameObject armorPrefab = new("PlayerArmorStatusVfx");

        try
        {
            BattleCharacter character = owner.AddComponent<BattleCharacter>();
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                armorVfx = new BattleVfxEntry { prefab = armorPrefab, flipType = VfxFlipType.None }
            });

            character.Initialize(new CharacterRuntimeData
            {
                CharacterId = "C_Armor",
                MaxHP = 10,
                CurrentHP = 10
            });

            BattleEffectUtility.AddShieldToPlayer(character, 2);

            Assert.That(owner.transform.Find("PlayerArmorStatusVfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(armorPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void AddShieldToMonster_UsesChildAnimatorForArmorVfxWhenShieldIncreases()
    {
        GameObject owner = new("MonsterArmorRoot");
        GameObject visual = new("MonsterArmorVisual");
        GameObject armorPrefab = new("MonsterArmorStatusVfx");

        try
        {
            visual.transform.SetParent(owner.transform, false);

            MonsterUnit monster = owner.AddComponent<MonsterUnit>();
            BattleUnitAnimator animator = visual.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "statusVfx", new BattleStatusVfxSet
            {
                armorVfx = new BattleVfxEntry { prefab = armorPrefab, flipType = VfxFlipType.None }
            });

            MonsterRuntimeData runtime = new(
                "Runtime_Armor_ChildAnimator",
                new MonsterMasterData
                {
                    MonsterId = "M_Armor_ChildAnimator",
                    Name = "ArmorChildAnimatorMonster",
                    HP = 10
                });

            monster.Initialize(runtime);

            BattleEffectUtility.AddShieldToMonster(monster, 2);

            Assert.That(visual.transform.Find("MonsterArmorStatusVfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(armorPrefab);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    private static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(type => type != null);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static BattleVfxEntry GetStatusVfx(BattleUnitAnimator animator, string effectId)
    {
        FieldInfo field = typeof(BattleUnitAnimator).GetField(
            "statusVfx",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);

        BattleStatusVfxSet statusVfx = (BattleStatusVfxSet)field.GetValue(animator);
        return statusVfx?.Get(effectId);
    }
}
