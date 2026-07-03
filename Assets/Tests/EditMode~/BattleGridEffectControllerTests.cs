using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleGridEffectControllerTests
{
    private GameObject controllerObject;
    private GameObject viewObject;

    [TearDown]
    public void TearDown()
    {
        if (viewObject != null)
            Object.DestroyImmediate(viewObject);

        if (controllerObject != null)
            Object.DestroyImmediate(controllerObject);
    }

    [Test]
    public void ApplyRendererSorting_PreservesPrefabSortingLayerByDefault()
    {
        controllerObject = new GameObject("GridEffectController");
        BattleGridEffectController controller = controllerObject.AddComponent<BattleGridEffectController>();

        viewObject = new GameObject("GridEffectView");
        GameObject spriteObject = new("Sprite");
        spriteObject.transform.SetParent(viewObject.transform);
        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Unit";
        renderer.sortingOrder = 3;

        InvokeApplyRendererSorting(controller, viewObject);

        Assert.That(renderer.sortingLayerName, Is.EqualTo("Unit"));
        Assert.That(renderer.sortingOrder, Is.EqualTo(4));
    }

    private static void InvokeApplyRendererSorting(
        BattleGridEffectController controller,
        GameObject view)
    {
        MethodInfo method = typeof(BattleGridEffectController).GetMethod(
            "ApplyRendererSorting",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        method.Invoke(controller, new object[] { view });
    }
}
