using System.IO;
using NUnit.Framework;

public sealed class WebGlVefectsShaderCompatibilityTests
{
    [TestCase("Assets/Project/Download/vfx/Vefects/Anime VFX URP/Shared/Shaders/SH_VFX_Stylized_Dissolve.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Anime VFX URP/Shared/Shaders/SH_VFX_Fresnel_Bomb.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Anime VFX URP/Shared/Shaders/SH_VFX_Stylized_Slash_01.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Anime VFX URP/Shared/Shaders/SH_VFX_Flat.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Stylized AoE URP/Shared/Shaders/SH_Vefects_URP_Windup_Core_Add_01.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Stylized AoE URP/Shared/Shaders/SH_Vefects_URP_Unlit_Simple_01.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Stylized AoE URP/Shared/Shaders/SH_Vefects_URP_Central_Area_01.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Stylized AoE URP/Shared/Shaders/SH_Vefects_URP_Coaster_01.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Stylized AoE URP/Shared/Shaders/SH_Vefects_URP_Unlit_Simple_LUT_01.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Stylized AoE URP/Shared/Shaders/SH_Vefects_URP_Dust_Puffs_01.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Stylized AoE URP/Shared/Shaders/SH_Vefects_URP_Splashes_01.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Stylized AoE URP/Shared/Shaders/SH_Vefects_URP_Unlit_Master_01.shader")]
    [TestCase("Assets/Project/Download/vfx/Vefects/Stylized AoE URP/Shared/Shaders/SH_Vefects_URP_Light_Rays_Add_01.shader")]
    public void BuildBlockingVefectsShader_IsWebGlCompatible(string shaderPath)
    {
        string shaderSource = File.ReadAllText(shaderPath);

        StringAssert.Contains("#pragma target 3.5", shaderSource, shaderPath);
        Assert.That(
            shaderSource,
            Does.Not.Contain("#error For WebGL2/GLES3"),
            shaderPath + " must not intentionally abort a WebGL build.");
    }
}
