#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class GridHighlightMaterialTests
{
    private static readonly string[] RangePreviewMaterialPaths =
    {
        "Assets/Project/Art/Materials/Grid/M_grid_range.mat",
        "Assets/Project/Art/Materials/Grid/M_grid_move.mat"
    };

    [Test]
    public void RangePreviewMaterials_RenderBehindBattleObjectsWithoutWritingDepth()
    {
        for (int i = 0; i < RangePreviewMaterialPaths.Length; i++)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RangePreviewMaterialPaths[i]);

            Assert.That(material, Is.Not.Null, RangePreviewMaterialPaths[i]);
            Assert.That(material.renderQueue, Is.EqualTo(2400), RangePreviewMaterialPaths[i]);
            Assert.That(material.GetFloat("_Surface"), Is.EqualTo(0f).Within(0.001f), RangePreviewMaterialPaths[i]);
            Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f).Within(0.001f), RangePreviewMaterialPaths[i]);
            Assert.That(material.GetFloat("_alpha"), Is.EqualTo(1f).Within(0.001f), RangePreviewMaterialPaths[i]);
            Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo((float)BlendMode.One).Within(0.001f), RangePreviewMaterialPaths[i]);
            Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo((float)BlendMode.Zero).Within(0.001f), RangePreviewMaterialPaths[i]);
        }
    }
}
#endif
