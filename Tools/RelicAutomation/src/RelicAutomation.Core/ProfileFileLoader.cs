using System.Text.Json;

namespace RelicAutomation.Core;

public static class ProfileFileLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AutomationProfile Load(string profilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profilePath);

        try
        {
            var fullPath = Path.GetFullPath(profilePath);
            var json = File.ReadAllText(fullPath);
            var document = JsonSerializer.Deserialize<ProfileDocument>(json, Options)
                ?? throw new InvalidDataException("프로필 JSON이 비어 있습니다.");
            var profileDirectory = Path.GetDirectoryName(fullPath)
                ?? throw new InvalidDataException("프로필 폴더를 확인할 수 없습니다.");
            var resourceDirectory = Path.GetFullPath(
                Path.Combine(profileDirectory, document.ResourceDirectory ?? "resource"));

            return new AutomationProfile(
                document.Name ?? string.Empty,
                document.EntryNode ?? string.Empty,
                document.TargetNode ?? string.Empty,
                document.RepeatCount,
                document.ExpectedClientWidth,
                document.ExpectedClientHeight,
                document.StopWhenWindowSizeChanges,
                resourceDirectory);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"프로필을 읽을 수 없습니다: {profilePath}", exception);
        }
    }

    private sealed class ProfileDocument
    {
        public string? Name { get; init; }
        public string? EntryNode { get; init; }
        public string? TargetNode { get; init; }
        public int RepeatCount { get; init; }
        public int ExpectedClientWidth { get; init; }
        public int ExpectedClientHeight { get; init; }
        public bool StopWhenWindowSizeChanges { get; init; }
        public string? ResourceDirectory { get; init; }
    }
}
