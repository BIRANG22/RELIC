using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

public static class LocalizationWorkbookWriter
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeRelationship = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationship = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static int MergeNewEntries(string workbookPath, IEnumerable<LocalizationWorkbookEntry> entries)
    {
        if (entries == null)
            return 0;

        var rows = LocalizationXlsxReader.ReadSheet(workbookPath, LocalizationExcelImporter.WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);
        IReadOnlyList<string> headers = rows[0];
        int keyIndex = FindHeader(headers, "Key");
        int koreanIndex = FindHeader(headers, "Korean(ko)");
        var existingKeys = new HashSet<string>(rows.Skip(1).Select(row => Value(row, keyIndex)), StringComparer.Ordinal);
        List<LocalizationWorkbookEntry> additions = entries
            .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Key) && !existingKeys.Contains(entry.Key))
            .GroupBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
        if (additions.Count == 0)
            return 0;

        string backupPath = workbookPath + ".localization-manager.backup";
        File.Copy(workbookPath, backupPath, true);
        using var archive = ZipFile.Open(workbookPath, ZipArchiveMode.Update);
        ZipArchiveEntry sheetEntry = FindTextSheet(archive);
        XDocument document;
        using (Stream stream = sheetEntry.Open())
            document = XDocument.Load(stream);
        sheetEntry.Delete();

        XElement sheetData = document.Root.Element(Spreadsheet + "sheetData");
        int rowNumber = document.Descendants(Spreadsheet + "row")
            .Select(row => (int?)row.Attribute("r") ?? 0).DefaultIfEmpty(0).Max();
        foreach (LocalizationWorkbookEntry addition in additions)
        {
            rowNumber++;
            var row = new XElement(Spreadsheet + "row", new XAttribute("r", rowNumber));
            for (int column = 0; column < headers.Count; column++)
            {
                string header = headers[column] ?? string.Empty;
                string value = column == keyIndex ? addition.Key : column == koreanIndex ? addition.Korean : string.Empty;
                if (!string.IsNullOrEmpty(value))
                    row.Add(InlineCell(column, rowNumber, value));
            }
            sheetData.Add(row);
        }

        ZipArchiveEntry replacement = archive.CreateEntry(sheetEntry.FullName, CompressionLevel.Optimal);
        using Stream output = replacement.Open();
        document.Save(output);
        return additions.Count;
    }

    private static XElement InlineCell(int column, int row, string value)
    {
        return new XElement(Spreadsheet + "c",
            new XAttribute("r", ColumnName(column) + row),
            new XAttribute("t", "inlineStr"),
            new XElement(Spreadsheet + "is", new XElement(Spreadsheet + "t", value)));
    }

    private static ZipArchiveEntry FindTextSheet(ZipArchive archive)
    {
        XDocument workbook = Load(archive, "xl/workbook.xml");
        XDocument relationships = Load(archive, "xl/_rels/workbook.xml.rels");
        XElement sheet = workbook.Descendants(Spreadsheet + "sheet")
            .First(candidate => string.Equals((string)candidate.Attribute("name"), LocalizationExcelImporter.WorksheetName, StringComparison.Ordinal));
        string relationId = (string)sheet.Attribute(OfficeRelationship + "id");
        string target = (string)relationships.Descendants(PackageRelationship + "Relationship")
            .First(relation => string.Equals((string)relation.Attribute("Id"), relationId, StringComparison.Ordinal)).Attribute("Target");
        string path = target.Replace('\\', '/').TrimStart('/');
        if (!path.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) path = "xl/" + path;
        return archive.GetEntry(path) ?? throw new InvalidDataException("Text worksheet XML could not be found.");
    }

    private static XDocument Load(ZipArchive archive, string path)
    {
        using Stream stream = (archive.GetEntry(path) ?? throw new InvalidDataException(path)).Open();
        return XDocument.Load(stream);
    }

    private static int FindHeader(IReadOnlyList<string> headers, string header) => headers.ToList().FindIndex(value => string.Equals(value, header, StringComparison.Ordinal));
    private static string Value(IReadOnlyList<string> row, int index) => index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;
    private static string ColumnName(int index)
    {
        string result = string.Empty;
        for (int value = index + 1; value > 0; value = (value - 1) / 26)
            result = (char)('A' + (value - 1) % 26) + result;
        return result;
    }
}

public sealed class LocalizationWorkbookEntry
{
    public string Key { get; }
    public string Korean { get; }
    public LocalizationWorkbookEntry(string key, string korean) { Key = key; Korean = korean; }
}
