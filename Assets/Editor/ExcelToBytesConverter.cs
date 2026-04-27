using System.IO;
using UnityEditor;
using UnityEngine;

public static class ExcelToBytesConverter
{
    private const string SourcePath = "Assets/ExcelSource/GameData.xlsx";
    private const string OutputPath = "Assets/Resources/Data/GameData.bytes";

    [MenuItem("Tools/Data/Convert GameData Excel To Bytes")]
    public static void Convert()
    {
        if (!File.Exists(SourcePath))
        {
            Debug.LogError($"[ExcelToBytesConverter] Source not found: {SourcePath}");
            return;
        }

        var outputDir = Path.GetDirectoryName(OutputPath);
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        File.Copy(SourcePath, OutputPath, true);

        AssetDatabase.Refresh();

        Debug.Log($"[ExcelToBytesConverter] Converted: {SourcePath} -> {OutputPath}");
    }
}