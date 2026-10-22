using RelicAutomation.Core;
using Xunit;

namespace RelicAutomation.Core.Tests;

public sealed class ProfileValidatorTests
{
    [Fact]
    public void Validate_ValidProfile_ResolvesResourceInsideProfileRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "resource", "pipeline"));
        Directory.CreateDirectory(Path.Combine(root, "resource", "image"));
        File.WriteAllText(Path.Combine(root, "resource", "pipeline", "main.json"), "{}");
        File.WriteAllBytes(Path.Combine(root, "resource", "image", "start.png"), new byte[] { 1 });

        try
        {
            AutomationProfile profile = ValidProfile();

            ProfileValidationResult result = ProfileValidator.Validate(profile, root);

            Assert.True(result.IsValid);
            Assert.Equal(Path.Combine(root, "resource"), result.ResourceDirectory);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void Validate_InvalidRepeatCount_IsRejected(int repeatCount)
    {
        AutomationProfile profile = ValidProfile() with { RepeatCount = repeatCount };

        ProfileValidationResult result = ProfileValidator.Validate(profile, Path.GetTempPath());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("repeat", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../outside")]
    [InlineData("C:\\outside")]
    public void Validate_ResourcePathOutsideProfileRoot_IsRejected(string resourceDirectory)
    {
        AutomationProfile profile = ValidProfile() with { ResourceDirectory = resourceDirectory };

        ProfileValidationResult result = ProfileValidator.Validate(profile, Path.Combine(Path.GetTempPath(), "profile"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("resource", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingPipelineOrImage_IsRejected()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "resource"));

        try
        {
            ProfileValidationResult result = ProfileValidator.Validate(ValidProfile(), root);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("pipeline", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Errors, error => error.Contains("png", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static AutomationProfile ValidProfile() => new(
        Name: "Sample",
        EntryNode: "Start",
        TargetNode: "TargetReached",
        RepeatCount: 10,
        ExpectedClientWidth: 1280,
        ExpectedClientHeight: 720,
        StopWhenWindowSizeChanges: true,
        ResourceDirectory: "resource");
}
