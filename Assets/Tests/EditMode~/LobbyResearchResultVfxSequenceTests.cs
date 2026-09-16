using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class LobbyResearchResultVfxSequenceTests
{
    [Test]
    public void Play_BlocksLobbyInputUntilSequenceCompletes()
    {
        GameObject panelRoot = new("ResearchResultPanel");
        GameObject vfxRoot = new("BlueDustiumVfx");

        try
        {
            ResearchResultPanelUI panel =
                panelRoot.AddComponent<ResearchResultPanelUI>();

            MethodInfo play = panel.GetType().GetMethod(
                "BeginVfxPresentation",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(play, Is.Not.Null,
                "보상 VFX 재생 중 기존 로비 입력을 차단할 시퀀스가 필요합니다.");

            SetPrivateField(panel, "blueDustiumVfxRoot", vfxRoot.transform);
            bool started = (bool)play.Invoke(panel, null);

            Assert.That(started, Is.True);
            Assert.That(LobbyPositionModalInputBlocker.IsBlocked, Is.True);
        }
        finally
        {
            LobbyPositionModalInputBlocker.Unblock(null);
            Object.DestroyImmediate(vfxRoot);
            Object.DestroyImmediate(panelRoot);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }
}
