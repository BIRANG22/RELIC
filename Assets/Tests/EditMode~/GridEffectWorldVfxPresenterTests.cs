using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class GridEffectWorldVfxPresenterTests
{
    private GameObject root;
    private GameObject childSource;
    private GameObject externalSource;

    [TearDown]
    public void TearDown()
    {
        DestroyObject(externalSource);
        DestroyObject(root);
    }

    [Test]
    public void PrepareSourceObjectsForProxy_HidesChildVfxSource()
    {
        GridEffectWorldVfxPresenter presenter = CreatePresenter();
        childSource = new GameObject("RawImageVfxSource");
        childSource.transform.SetParent(root.transform);
        SetEntries(presenter, new BattleVfxEntry { prefab = childSource });

        presenter.PrepareSourceObjectsForProxy();

        Assert.That(childSource.activeSelf, Is.False);
    }

    [Test]
    public void PrepareSourceObjectsForProxy_LeavesExternalPrefabSourceActive()
    {
        GridEffectWorldVfxPresenter presenter = CreatePresenter();
        externalSource = new GameObject("ExternalVfxPrefab");
        SetEntries(presenter, new BattleVfxEntry { prefab = externalSource });

        presenter.PrepareSourceObjectsForProxy();

        Assert.That(externalSource.activeSelf, Is.True);
    }

    [Test]
    public void PrepareSourceObjectsForProxy_DoesNotDisablePresenterRoot()
    {
        GridEffectWorldVfxPresenter presenter = CreatePresenter();
        SetEntries(presenter, new BattleVfxEntry { prefab = root });

        presenter.PrepareSourceObjectsForProxy();

        Assert.That(root.activeSelf, Is.True);
    }

    [Test]
    public void CreateRuntimeEntry_UsesControllerProxySortingContext()
    {
        GridEffectWorldVfxPresenter presenter = CreatePresenter();
        externalSource = new GameObject("ExternalVfxPrefab");
        BattleVfxEntry source = new()
        {
            prefab = externalSource,
            renderMode = BattleVfxRenderMode.DirectWorldRenderer,
            proxySortingLayerName = "PrefabLayer",
            proxySortingOrderOffset = 3,
            proxyYMultiplier = 5f,
            sfx = new BattleVfxSfxEntry
            {
                playSfx = true,
                sfxId = "grid.poison",
                volumeMultiplier = 0.25f
            }
        };
        GridEffectWorldVfxSpawnContext context = new(
            Vector3.zero,
            renderLayer: 0,
            visibleLayer: 0,
            sortingLayerName: "ControllerLayer",
            sortingOrderOffset: 7,
            yMultiplier: 123f,
            lifeTime: 1f);

        BattleVfxEntry runtimeEntry = InvokeCreateRuntimeEntry(
            presenter,
            source,
            context);

        Assert.That(runtimeEntry.renderMode, Is.EqualTo(BattleVfxRenderMode.IndividualWorldRenderTexture));
        Assert.That(runtimeEntry.proxySortingLayerName, Is.EqualTo("ControllerLayer"));
        Assert.That(runtimeEntry.proxySortingOrderOffset, Is.EqualTo(10));
        Assert.That(runtimeEntry.proxyYMultiplier, Is.EqualTo(123f));
        Assert.That(runtimeEntry.sfx.playSfx, Is.True);
        Assert.That(runtimeEntry.sfx.sfxId, Is.EqualTo("grid.poison"));
        Assert.That(runtimeEntry.sfx.volumeMultiplier, Is.EqualTo(0.25f));
    }

    private GridEffectWorldVfxPresenter CreatePresenter()
    {
        root = new GameObject("GridEffectView");
        return root.AddComponent<GridEffectWorldVfxPresenter>();
    }

    private static void SetEntries(
        GridEffectWorldVfxPresenter presenter,
        params BattleVfxEntry[] entries)
    {
        FieldInfo field = typeof(GridEffectWorldVfxPresenter).GetField(
            "vfxEntries",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        field.SetValue(presenter, entries);
    }

    private static BattleVfxEntry InvokeCreateRuntimeEntry(
        GridEffectWorldVfxPresenter presenter,
        BattleVfxEntry source,
        GridEffectWorldVfxSpawnContext context)
    {
        MethodInfo method = typeof(GridEffectWorldVfxPresenter).GetMethod(
            "CreateRuntimeEntry",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        return (BattleVfxEntry)method.Invoke(presenter, new object[] { source, context });
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
