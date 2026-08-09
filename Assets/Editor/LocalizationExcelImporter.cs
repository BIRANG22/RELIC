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

        using var reader = new StringReader(csv);
        Csv.ImportInto(
            reader,
            collection,
            createUndo: true,
            reporter: null,
            removeMissingEntries: RemoveMissingEntries);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
