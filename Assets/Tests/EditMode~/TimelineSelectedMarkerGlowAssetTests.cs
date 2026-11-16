using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TimelineSelectedMarkerGlowAssetTests
{
    private const string MaterialPath = "Assets/Project/Art/YDJ/Glow_Material.mat";
    private const string ShaderPath = "Assets/Project/Art/YDJ/TurnMark_BorderFlow.shader";

    [Test]
    public void TimelineSelectedMarkerGlowMaterial_UsesBorderFlowShader()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

        Assert.That(material, Is.Not.Null);
        Assert.That(shader, Is.Not.Null);
        Assert.That(material.shader, Is.EqualTo(shader));
        Assert.That(shader.name, Is.EqualTo("Dustium/UI/TurnMarkBorderFlow"));
    }
}
