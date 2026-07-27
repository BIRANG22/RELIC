using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class SteamAppIdBuildPostprocessor : IPostprocessBuildWithReport
{
    private const string SteamAppIdFileName = "steam_appid.txt";

    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report == null || !IsSupportedTarget(report.summary.platform))
            return;

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        string sourcePath = string.IsNullOrEmpty(projectRoot)
            ? ""
            : Path.Combine(projectRoot, SteamAppIdFileName);
        string destinationPath = GetDestinationPath(report.summary.outputPath);

        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            Debug.LogError(
                "[SteamAppIdBuildPostprocessor] Source steam_appid.txt is missing: " +
                sourcePath);
            return;
        }

        File.Copy(sourcePath, destinationPath, true);
        Debug.Log("[SteamAppIdBuildPostprocessor] Copied steam_appid.txt to: " + destinationPath);
    }

    public static bool IsSupportedTarget(BuildTarget target)
    {
        return target == BuildTarget.StandaloneWindows ||
               target == BuildTarget.StandaloneWindows64;
    }

    public static string GetDestinationPath(string builtPlayerPath)
    {
        string buildDirectory = Path.GetDirectoryName(builtPlayerPath);
        return Path.Combine(buildDirectory ?? "", SteamAppIdFileName);
    }
}
