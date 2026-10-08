using System.Diagnostics;
using System;
using System.IO;
using System.Threading.Tasks;
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
        try
        {
            ConvertOrThrow();
            AssetDatabase.Refresh();
            Debug.Log($"[ExcelToBytesConverter] Converted: {SourcePath} -> {OutputPath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ExcelToBytesConverter] CSV conversion failed: {exception.Message}");
        }
    }

    public static void ConvertOrThrow(
        string sourcePath = SourcePath,
        string outputPath = OutputPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
            throw new InvalidOperationException("Project root could not be resolved.");

        string sourceFullPath = ResolvePath(projectRoot, sourcePath);
        string outputFullPath = ResolvePath(projectRoot, outputPath);
        string scriptFullPath = Path.GetFullPath(Path.Combine(projectRoot, ConverterScriptPath));
        if (!File.Exists(sourceFullPath))
            throw new FileNotFoundException("GameData source workbook was not found.", sourceFullPath);
        if (!File.Exists(scriptFullPath))
            throw new FileNotFoundException("GameData converter script was not found.", scriptFullPath);

        string outputDirectory = Path.GetDirectoryName(outputFullPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new InvalidOperationException("GameData output directory could not be resolved.");
        Directory.CreateDirectory(outputDirectory);

        string temporaryOutputPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileName(outputFullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            RunConverterProcess(scriptFullPath, sourceFullPath, temporaryOutputPath);
            if (!File.Exists(temporaryOutputPath))
                throw new IOException($"GameData converter did not create the output file: {temporaryOutputPath}");

            if (File.Exists(outputFullPath))
                File.Replace(temporaryOutputPath, outputFullPath, null);
            else
                File.Move(temporaryOutputPath, outputFullPath);
        }
        finally
        {
            if (File.Exists(temporaryOutputPath))
                File.Delete(temporaryOutputPath);
        }
    }

    private static void RunConverterProcess(
        string scriptFullPath,
        string sourceFullPath,
        string outputFullPath)
    {
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
            throw new InvalidOperationException("Failed to start the GameData CSV converter.");

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(120000))
        {
            Exception killException = null;
            try { process.Kill(); }
            catch (Exception exception) { killException = exception; }

            process.WaitForExit(5000);
            throw new TimeoutException(
                "GameData CSV conversion exceeded 120 seconds.",
                killException);
        }

        Task.WaitAll(outputTask, errorTask);
        string standardError = errorTask.Result;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"GameData CSV conversion failed (exit {process.ExitCode}): {standardError.Trim()}");
    }

    private static string ResolvePath(string projectRoot, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        return Path.GetFullPath(Path.IsPathRooted(path)
            ? path
            : Path.Combine(projectRoot, path));
    }
}
