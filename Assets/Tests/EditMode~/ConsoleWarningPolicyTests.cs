using System.IO;
using Discord.Sdk;
using NUnit.Framework;
using UnityEngine;

public sealed class ConsoleWarningPolicyTests
{
    [Test]
    public void ReservationSfx_UsesTheRegisteredAudioIdInCodeAndBattleScene()
    {
        GameObject gameObject = new("ReservationSfxTest");

        try
        {
            PlayerSkillReservationController controller =
                gameObject.AddComponent<PlayerSkillReservationController>();
            string configuredId = (string)typeof(PlayerSkillReservationController)
                .GetField("reservationConfirmSfxId",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .GetValue(controller);

            Assert.That(configuredId, Is.EqualTo(AudioIds.Sfx.SkillReserve));
            Assert.That(
                File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity"),
                Does.Contain("reservationConfirmSfxId: battle.skill.reserve"));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [TestCase(ErrorType.NetworkError)]
    [TestCase(ErrorType.ClientNotReady)]
    [TestCase(ErrorType.Disabled)]
    [TestCase(ErrorType.RPCError)]
    public void ExpectedDiscordClientAbsence_DoesNotRequireWarning(ErrorType errorType)
    {
        Assert.That(DiscordPresencePolicy.IsExpectedClientUnavailable(errorType), Is.True);
    }

    [TestCase(ErrorType.HTTPError)]
    [TestCase(ErrorType.ValidationError)]
    [TestCase(ErrorType.AuthorizationFailed)]
    public void DiscordConfigurationFailures_RemainReportable(ErrorType errorType)
    {
        Assert.That(DiscordPresencePolicy.IsExpectedClientUnavailable(errorType), Is.False);
    }

    [TestCase(
        "Assets/Project/Download/vfx/Vefects/Stylized AoE URP/Shared/Shaders/SH_Vefects_URP_Central_Area_01.shader")]
    [TestCase(
        "Assets/Project/Download/vfx/Vefects/Stylized VFX URP/Stylized VFX Shuriken/Shared/Shaders/SH_VFX_Vefects_AB_Dissolve_URP_New.shader")]
    public void TargetShader_PowTextureBaseIsClampedNonNegative(string shaderPath)
    {
        string shaderSource = File.ReadAllText(shaderPath);

        Assert.That(shaderSource, Does.Contain("pow( max( tex2D("));
    }

    [TestCase(
        "Assets/Project/Download/vfx/GabrielAguiarProductions/Shaders/Master_Unlit.shadergraph",
        1)]
    [TestCase(
        "Assets/Project/Download/vfx/GabrielAguiarProductions/UniqueSwordSlashesVol_1/Shaders/MasterSlash_Unlit.shadergraph",
        2)]
    [TestCase("Assets/Project/Art/Shaders/transition.shadergraph", 1)]
    public void TargetShaderGraph_PowBaseUsesNonNegativeMaximumNode(
        string shaderGraphPath,
        int expectedCount)
    {
        string shaderGraphSource = File.ReadAllText(shaderGraphPath);
        const string nonNegativeNodeName = "Pow Base Nonnegative";

        Assert.That(
            CountOccurrences(shaderGraphSource, nonNegativeNodeName),
            Is.EqualTo(expectedCount));
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = 0;

        while ((index = source.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
