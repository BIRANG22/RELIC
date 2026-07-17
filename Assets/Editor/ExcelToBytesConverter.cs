using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class ExcelToBytesConverter
{
    private const string SourcePath = "Assets/ExcelSource/GameData.xlsx";
    private const string OutputPath = "Assets/Resources/Data/GameDataRuntime.csv";
    private const string ConverterScriptPath = "Assets/Editor/GameDataXlsxToSectionedCsv.ps1";

    [MenuItem("Tools/Data/Convert GameData Excel To Runtime CSV")]
    public static void Convert()
    {
        if (!File.Exists(SourcePath))
        {
            Debug.LogError($"[ExcelToBytesConverter] Source not found: {SourcePath}");
            return;
        }

        if (!File.Exists(ConverterScriptPath))
        {
            Debug.LogError($"[ExcelToBytesConverter] Converter script not found: {ConverterScriptPath}");
            return;
        }

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            Debug.LogError("[ExcelToBytesConverter] Project root could not be resolved.");
            return;
        }

        string sourceFullPath = Path.GetFullPath(Path.Combine(projectRoot, SourcePath));
        string outputFullPath = Path.GetFullPath(Path.Combine(projectRoot, OutputPath));
        string scriptFullPath = Path.GetFullPath(Path.Combine(projectRoot, ConverterScriptPath));

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments =
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptFullPath}\" " +
                $"-SourcePath \"{sourceFullPath}\" -OutputPath \"{outputFullPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using Process process = Process.Start(startInfo);
        if (process == null)
        {
            Debug.LogError("[ExcelToBytesConverter] Failed to start CSV converter.");
            return;
        }

        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Debug.LogError($"[ExcelToBytesConverter] CSV conversion failed: {standardError}");
            return;
        }

        AssetDatabase.Refresh();

        Debug.Log($"[ExcelToBytesConverter] {standardOutput.Trim()}");
    }
}
