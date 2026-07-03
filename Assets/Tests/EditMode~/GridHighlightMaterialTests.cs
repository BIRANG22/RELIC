#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class GridHighlightMaterialTests
{
    private const string HighlightMaterialPath = "Assets/Project/Art/Materials/grid/M_grid1.mat";

    [Test]
    public void GridHighlightMaterial_IsOpaqueAndRendersBeforeTransparentUnits()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(HighlightMaterialPath);

        Assert.That(material, Is.Not.Null);
        Assert.That(material.renderQueue, Is.EqualTo(2499));
        Assert.That(material.GetFloat("_Surface"), Is.EqualTo(0f).Within(0.001f));
        Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f).Within(0.001f));
        Assert.That(material.GetFloat("_alpha"), Is.EqualTo(1f).Within(0.001f));
        Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo((float)BlendMode.One).Within(0.001f));
        Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo((float)BlendMode.Zero).Within(0.001f));
    }
}
#endif
