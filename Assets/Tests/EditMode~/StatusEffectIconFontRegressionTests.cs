using System.IO;
using NUnit.Framework;

public sealed class StatusEffectIconFontRegressionTests
{
    [Test]
    public void StatusEffectIcon_ValueText_UsesExplicitNumberFontRole()
    {
        string prefab = File.ReadAllText("Assets/Project/PrefabsR/HUD_Prefab/StatusEffectIcon.prefab");

        StringAssert.Contains("m_fontAsset: {fileID: 11400000, guid: 15f0f3445fd0bf947b79b40fad359512, type: 2}", prefab);
        StringAssert.Contains("m_sharedMaterial: {fileID: -2980565862983227948, guid: 15f0f3445fd0bf947b79b40fad359512, type: 2}", prefab);
        StringAssert.Contains("roleId: number", prefab);
        StringAssert.Contains("target: {fileID: 6367340840458088865}", prefab);
    }
}
