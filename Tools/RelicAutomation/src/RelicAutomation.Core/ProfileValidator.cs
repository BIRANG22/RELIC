namespace RelicAutomation.Core;

public static class ProfileValidator
{
    public const int MaximumRepeatCount = 10_000;

    public static ProfileValidationResult Validate(AutomationProfile profile, string profileRoot)
    {
        ArgumentNullException.ThrowIfNull(profile);

        List<string> errors = new();
        string root = string.Empty;
        string resourcePath = string.Empty;

        if (string.IsNullOrWhiteSpace(profileRoot))
        {
            errors.Add("Profile root is required.");
        }
        else
        {
            root = Path.GetFullPath(profileRoot);
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
            errors.Add("Profile name is required.");
        if (string.IsNullOrWhiteSpace(profile.EntryNode))
            errors.Add("Entry node is required.");
        if (string.IsNullOrWhiteSpace(profile.TargetNode))
            errors.Add("Target node is required.");
        if (profile.RepeatCount <= 0 || profile.RepeatCount > MaximumRepeatCount)
            errors.Add($"Repeat count must be between 1 and {MaximumRepeatCount}.");
        if (profile.ExpectedClientWidth <= 0 || profile.ExpectedClientHeight <= 0)
            errors.Add("Expected client size must be positive.");

        if (string.IsNullOrWhiteSpace(profile.ResourceDirectory))
        {
            errors.Add("Resource directory is required.");
        }
        else if (Path.IsPathRooted(profile.ResourceDirectory))
        {
            errors.Add("Resource directory must be relative to the profile root.");
        }
        else if (!string.IsNullOrEmpty(root))
        {
            resourcePath = Path.GetFullPath(Path.Combine(root, profile.ResourceDirectory));
            string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!resourcePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Resource directory must stay inside the profile root.");
                resourcePath = string.Empty;
            }
        }

        if (!string.IsNullOrEmpty(resourcePath))
        {
            string pipelinePath = Path.Combine(resourcePath, "pipeline");
            string imagePath = Path.Combine(resourcePath, "image");
            if (!Directory.Exists(pipelinePath) || !Directory.EnumerateFiles(pipelinePath, "*.json", SearchOption.AllDirectories).Any())
                errors.Add("Resource pipeline directory must contain at least one JSON file.");
            if (!Directory.Exists(imagePath) || !Directory.EnumerateFiles(imagePath, "*.png", SearchOption.AllDirectories).Any())
                errors.Add("Resource image directory must contain at least one PNG file.");
        }

        return new ProfileValidationResult(errors.Count == 0, resourcePath, errors);
    }
}
