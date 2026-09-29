using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEngine;

public static class LocalizationExcelImporter
{
    public const string WorkbookPath = "Assets/ExcelSource/Localization.xlsx";
    public const string WorksheetName = "Text";
    public const string TableCollectionName = "Text";
    public const bool RemoveMissingEntries = true;
    private const string SharedTablePath = "Assets/Language/Text Shared Data.asset";
    private const string StartupImportSessionKey = "RELIC.LocalizationExcelImporter.StartupImportQueued";

    [InitializeOnLoadMethod]
    private static void QueueImportWhenWorkbookIsNewer()
    {
        if (SessionState.GetBool(StartupImportSessionKey, false) ||
            !File.Exists(WorkbookPath) ||
            (File.Exists(SharedTablePath) &&
             File.GetLastWriteTimeUtc(WorkbookPath) <= File.GetLastWriteTimeUtc(SharedTablePath)))
        {
            return;
        }

        SessionState.SetBool(StartupImportSessionKey, true);
        EditorApplication.delayCall += ImportFromMenu;
    }

    [MenuItem("Tools/Localization/Import Localization Excel")]
    public static void ImportFromMenu()
    {
        try
        {
            Import();
            Debug.Log($"[LocalizationExcelImporter] Imported '{WorkbookPath}' into '{TableCollectionName}'.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    public static void Import()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(TableCollectionName);
        if (collection == null)
            throw new InvalidOperationException($"String Table Collection '{TableCollectionName}' was not found.");

        var rows = LocalizationXlsxReader.ReadSheet(WorkbookPath, WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);
        string csv = LocalizationXlsxReader.ToCsv(rows);

        AssetDatabase.StartAssetEditing();
        try
        {
            using var reader = new StringReader(csv);
            Csv.ImportInto(
                reader,
                collection,
                createUndo: true,
                reporter: null,
                removeMissingEntries: RemoveMissingEntries);

            AssetDatabase.SaveAssets();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();
    }
}

public sealed class LocalizationWorkbookAssetPostprocessor : AssetPostprocessor
{
    private static bool importQueued;

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (importQueued || Array.IndexOf(importedAssets, LocalizationExcelImporter.WorkbookPath) < 0)
            return;

        importQueued = true;
        EditorApplication.delayCall += ImportWorkbook;
    }

    private static void ImportWorkbook()
    {
        importQueued = false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            importQueued = true;
            EditorApplication.delayCall += ImportWorkbook;
            return;
        }

        LocalizationExcelImporter.ImportFromMenu();
    }
}
