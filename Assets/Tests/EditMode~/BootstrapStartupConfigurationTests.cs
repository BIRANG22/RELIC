using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class BootstrapStartupConfigurationTests
{
    [Test]
    public void BootstrapScene_DoesNotEnableDirectTitleTransitionLoader()
    {
        const string bootstrapScenePath = "Assets/Project/Scenes/YDM/Bootstrap.unity";
        const string loaderScriptGuid = "fbb14b713e1b8584bb1b82ba95e7356c";

        string sceneText = File.ReadAllText(bootstrapScenePath);
        int componentIndex = sceneText.IndexOf(
            "m_Script: {fileID: 11500000, guid: " + loaderScriptGuid,
            System.StringComparison.Ordinal);

        Assert.That(componentIndex, Is.GreaterThanOrEqualTo(0), "Bootstrap 전환 컴포넌트를 찾을 수 없습니다.");

        int objectStart = sceneText.LastIndexOf("--- !u!1 &", componentIndex, System.StringComparison.Ordinal);
        int componentStart = sceneText.LastIndexOf("--- !u!114 &", componentIndex, System.StringComparison.Ordinal);
        string objectBlock = sceneText.Substring(objectStart, componentStart - objectStart);

        Assert.That(objectBlock, Does.Contain("m_IsActive: 0"),
            "Bootstrap에서는 상태 머신만 Title 전환을 담당해야 합니다.");
    }
}
