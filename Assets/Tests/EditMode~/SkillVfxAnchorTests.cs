using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class SkillVfxAnchorTests
{
    [Test]
    public void BattleVfxEntry_DefaultsToCasterSpawnAnchor()
    {
        BattleVfxEntry entry = new();

        Assert.That(entry.spawnAnchor, Is.EqualTo(BattleVfxSpawnAnchor.Caster));
        Assert.That(entry.scaleDirectWorldRendererToProxyHeight, Is.False);
    }

    [Test]
    public void PlaySkillAction_DirectWorldRendererAppliesProxyOffsetAndOptionalHeight()
    {
        GameObject owner = new("DirectWorldOwner");
        GameObject spawnPoint = new("DirectWorldSpawn");
        GameObject prefab = CreateSpritePrefab("DirectWorldSkillVfx", spriteHeight: 2f);
        SkillVfxDatabase database = ScriptableObject.CreateInstance<SkillVfxDatabase>();

        try
        {
            spawnPoint.transform.SetParent(owner.transform, false);
            spawnPoint.transform.position = new Vector3(10f, 20f, 0f);

            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "vfxSpawnPoint", spawnPoint.transform);
            SetPrivateField(animator, "skillVfxDatabase", database);

            SetEntries(database, new List<SkillVfxEntry>
            {
                new()
                {
                    SkillId = "S_Ability_11",
                    Vfx = new BattleVfxEntry
                    {
                        prefab = prefab,
                        flipType = VfxFlipType.None,
                        renderMode = BattleVfxRenderMode.DirectWorldRenderer,
                        proxyWorldOffset = new Vector3(1f, 2f, 0f),
                        proxyWorldHeight = 4f,
                        scaleDirectWorldRendererToProxyHeight = true
                    }
                }
            });

            animator.PlaySkillAction(CreateSkill(), 0);

            Transform spawned = spawnPoint.transform.Find("DirectWorldSkillVfx(Clone)");
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.localPosition, Is.EqualTo(new Vector3(1f, 2f, 0f)));
            Assert.That(spawned.localScale, Is.EqualTo(new Vector3(2f, 2f, 2f)));
        }
        finally
        {
            DestroyObject(database);
            DestroySpritePrefab(prefab);
            DestroyObject(owner);
        }
    }

    [Test]
    public void PlaySkillAction_SelectionGridAnchorSpawnsAtSelectedGridPosition()
    {
        GameObject owner = new("SkillVfxSelectedGridOwner");
        GameObject spawnPoint = new("VfxSpawnPoint");
        GameObject prefab = new("SelectedGridSkillVfx");
        GameObject gridRoot = new("GridRoot");
        SkillVfxDatabase database = ScriptableObject.CreateInstance<SkillVfxDatabase>();

        try
        {
            spawnPoint.transform.SetParent(owner.transform, false);
            spawnPoint.transform.position = new Vector3(-5f, -5f, 0f);

            CreateGridCells(gridRoot.transform);
            GridManager gridManager = gridRoot.AddComponent<GridManager>();
            InvokePrivateMethod(gridManager, "InitializeCells");

            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "vfxSpawnPoint", spawnPoint.transform);
            SetPrivateField(animator, "gridManager", gridManager);
            SetPrivateField(animator, "skillVfxDatabase", database);

            SetEntries(database, new List<SkillVfxEntry>
            {
                new()
                {
                    SkillId = "S_Ability_11",
                    Vfx = new BattleVfxEntry
                    {
                        prefab = prefab,
                        flipType = VfxFlipType.None,
                        renderMode = BattleVfxRenderMode.DirectWorldRenderer,
                        spawnAnchor = BattleVfxSpawnAnchor.SelectedGrid,
                        proxyWorldOffset = new Vector3(0.5f, -0.25f, 0f)
                    }
                }
            });

            SkillMasterData skillData = CreateSkill();
            PlayerReservedCommand command = new(new CharacterRuntimeData { CharacterId = "Char_01" }, skillData);
            command.SetSelectionAreaResult(
                BattleDirection.Right,
                selectedGridIndex: 17,
                rangeGridIndices: new List<int> { 17 });

            animator.PlaySkillAction(command);

            GameObject spawned = GameObject.Find("SelectedGridSkillVfx(Clone)");
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.transform.position, Is.EqualTo(new Vector3(3.5f, 1.75f, 0f)));
        }
        finally
        {
            DestroyObject(GameObject.Find("SelectedGridSkillVfx(Clone)"));
            DestroyObject(GameObject.Find("SelectedGridSkillVfx_VfxAnchor"));
            DestroyObject(database);
            DestroyObject(prefab);
            DestroyObject(gridRoot);
            DestroyObject(owner);
        }
    }

    [Test]
    public void PlaySkillAction_SelectedGridAnchorFallsBackToCasterWhenSelectionIsMissing()
    {
        GameObject owner = new("SkillVfxFallbackOwner");
        GameObject prefab = new("FallbackSkillVfx");
        SkillVfxDatabase database = ScriptableObject.CreateInstance<SkillVfxDatabase>();

        try
        {
            BattleUnitAnimator animator = owner.AddComponent<BattleUnitAnimator>();
            SetPrivateField(animator, "skillVfxDatabase", database);

            SetEntries(database, new List<SkillVfxEntry>
            {
                new()
                {
                    SkillId = "S_Ability_11",
                    Vfx = new BattleVfxEntry
                    {
                        prefab = prefab,
                        flipType = VfxFlipType.None,
                        spawnAnchor = BattleVfxSpawnAnchor.SelectedGrid
                    }
                }
            });

            SkillMasterData skillData = CreateSkill();
            PlayerReservedCommand command = new(new CharacterRuntimeData { CharacterId = "Char_01" }, skillData);

            animator.PlaySkillAction(command);

            Assert.That(owner.transform.Find("FallbackSkillVfx(Clone)"), Is.Not.Null);
        }
        finally
        {
            DestroyObject(database);
            DestroyObject(prefab);
            DestroyObject(owner);
        }
    }

    private static SkillMasterData CreateSkill()
    {
        return new SkillMasterData
        {
            SkillId = "S_Ability_11",
            SkillType = SkillType.Attack,
            Category = Category.Ability
        };
    }

    private static void CreateGridCells(Transform parent)
    {
        for (int x = 0; x < 7; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                GameObject cellObject = new($"Cell_{x}_{y}");
                cellObject.transform.SetParent(parent, false);
                cellObject.transform.position = new Vector3(x, y, 0f);
                cellObject.AddComponent<GridCell>();
            }
        }
    }

    private static GameObject CreateSpritePrefab(string name, float spriteHeight)
    {
        GameObject prefab = new(name);
        Texture2D texture = new(100, 100, TextureFormat.RGBA32, false);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 100f, 100f),
            new Vector2(0.5f, 0.5f),
            100f / spriteHeight);
        SpriteRenderer renderer = prefab.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        return prefab;
    }

    private static void DestroySpritePrefab(GameObject prefab)
    {
        if (prefab != null && prefab.TryGetComponent(out SpriteRenderer renderer) && renderer.sprite != null)
        {
            Texture2D texture = renderer.sprite.texture;
            DestroyObject(renderer.sprite);
            DestroyObject(texture);
        }

        DestroyObject(prefab);
    }

    private static void SetEntries(SkillVfxDatabase database, List<SkillVfxEntry> entries)
    {
        FieldInfo field = typeof(SkillVfxDatabase).GetField(
            "entries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        field.SetValue(database, entries);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, methodName);
        method.Invoke(target, null);
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }
}
