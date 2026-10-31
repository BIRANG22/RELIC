using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
            LocalizationGameDataSourceSynchronizer.SyncResult sync =
                LocalizationGameDataSourceSynchronizer.Synchronize();
            Import();
            Debug.Log($"[LocalizationExcelImporter] Synced GameData sources " +
                      $"(updated {sync.SourceUpdates}, added {sync.Additions}, removed {sync.Removals}) and " +
                      $"imported '{WorkbookPath}' into '{TableCollectionName}'.");
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

        LocalizationWorkbookWriter.EnsureKoreanSourceColumn(WorkbookPath);
        var rows = LocalizationXlsxReader.ReadSheet(WorkbookPath, WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);
        // Korean Source is source-control metadata, not a Unity localization locale.
        int sourceIndex = rows[0].ToList().FindIndex(header =>
            string.Equals(header, LocalizationWorkbookWriter.KoreanSourceHeader, StringComparison.Ordinal));
        IReadOnlyList<IReadOnlyList<string>> importRows = sourceIndex < 0 ? rows :
            rows.Select(row => (IReadOnlyList<string>)row.Where((_, index) => index != sourceIndex).ToArray()).ToArray();
        string csv = LocalizationXlsxReader.ToCsv(importRows);

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

            // 전체 프로젝트의 dirty 에셋을 저장하지 않고, 이번 Import가 갱신한
            // String Table Collection 구성 요소만 저장한다.
            AssetDatabase.SaveAssetIfDirty(collection);
            AssetDatabase.SaveAssetIfDirty(collection.SharedData);
            foreach (var table in collection.StringTables)
                AssetDatabase.SaveAssetIfDirty(table);
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
