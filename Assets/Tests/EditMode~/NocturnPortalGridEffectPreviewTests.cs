using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class NocturnPortalGridEffectPreviewTests
{
    private const string DatabasePath = "Assets/DB/GridEffectSpriteDatabase.asset";

    [Test]
    public void Database_ResolvesNocturnPortalPreviewToMoveVfxPrefab()
    {
        GridEffectSpriteDatabase database =
            AssetDatabase.LoadAssetAtPath<GridEffectSpriteDatabase>(DatabasePath);

        Assert.That(database, Is.Not.Null);
        Assert.That(
            database.TryGetPrefab("GR_nocturn_portal_preview", out GameObject prefab),
            Is.True);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.name, Is.EqualTo("Vfx_Gr_Mon_E_01_move"));
    }

    [Test]
    public void PreviewEntry_UsesAlphaWorldRenderTextureProxy()
    {
        GridEffectSpriteDatabase database =
            AssetDatabase.LoadAssetAtPath<GridEffectSpriteDatabase>(DatabasePath);

        MethodInfo factory = typeof(PlayerSkillReservationController).GetMethod(
            "CreateNocturnPortalPreviewEntry",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(factory, Is.Not.Null);

        BattleVfxEntry entry = factory.Invoke(null, new object[] { database, 13 }) as BattleVfxEntry;

        Assert.That(entry, Is.Not.Null);
        Assert.That(entry.prefab, Is.Not.Null);
        Assert.That(entry.prefab.name, Is.EqualTo("Vfx_Gr_Mon_E_01_move"));
        Assert.That(entry.renderMode, Is.EqualTo(BattleVfxRenderMode.IndividualWorldRenderTexture));
        Assert.That(entry.proxyBlendMode, Is.EqualTo(BattleVfxProxyBlendMode.Alpha));
        Assert.That(entry.proxySortingLayerName, Is.EqualTo("Unit"));
        Assert.That(entry.proxySortingOrderOffset, Is.EqualTo(13));
    }
}
