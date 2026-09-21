using NUnit.Framework;
using UnityEngine;

public sealed class UIBackgroundBlurBuildResourceTests
{
    private const string MaterialResourcePath = "UI/DustiumBackgroundBlur";

    [Test]
    public void BlurMaterial_IsLoadableFromResourcesWithSupportedShader()
    {
        Material material = Resources.Load<Material>(MaterialResourcePath);

        Assert.That(material, Is.Not.Null,
            "빌드에 포함될 블러 머티리얼을 Resources 경로에서 찾을 수 없습니다.");
        Assert.That(material.shader, Is.Not.Null);
        Assert.That(material.shader.name, Is.EqualTo("UI/DustiumBackgroundBlur"));
        Assert.That(material.shader.isSupported, Is.True,
            "현재 렌더링 환경에서 블러 셰이더를 지원하지 않습니다.");
        Assert.That(material.HasProperty("_UIBlurSourceTexture"), Is.True);
        Assert.That(material.HasProperty("_UIBlurUiTexture"), Is.True);
        Assert.That(material.HasProperty("_BlurRadius"), Is.True);
    }
}
