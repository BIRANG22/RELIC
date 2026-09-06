using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

public static class LocalizationXlsxReader
{
    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static readonly XNamespace DocumentRelationshipNamespace =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static readonly XNamespace PackageRelationshipNamespace =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    public static IReadOnlyList<IReadOnlyList<string>> ReadSheet(string workbookPath, string sheetName)
    {
        if (string.IsNullOrWhiteSpace(workbookPath))
            throw new ArgumentException("Workbook path is required.", nameof(workbookPath));
        if (string.IsNullOrWhiteSpace(sheetName))
            throw new ArgumentException("Sheet name is required.", nameof(sheetName));
        if (!File.Exists(workbookPath))
            throw new FileNotFoundException("Localization workbook was not found.", workbookPath);

        using FileStream stream = File.Open(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        List<string> sharedStrings = ReadSharedStrings(archive);
        ZipArchiveEntry sheetEntry = FindSheetEntry(archive, sheetName);
        using Stream sheetStream = sheetEntry.Open();
        XDocument sheetDocument = XDocument.Load(sheetStream);

        var rows = new List<IReadOnlyList<string>>();
        foreach (XElement rowElement in sheetDocument.Descendants(SpreadsheetNamespace + "row"))
        {
            var values = new List<string>();
            foreach (XElement cell in rowElement.Elements(SpreadsheetNamespace + "c"))
            {
                string reference = (string)cell.Attribute("r");
                int columnIndex = string.IsNullOrEmpty(reference)
                    ? values.Count
                    : GetColumnIndex(reference);

                while (values.Count <= columnIndex)
                    values.Add(string.Empty);

                values[columnIndex] = ReadCellValue(cell, sharedStrings);
            }

            rows.Add(values);
        }

        return rows;
    }

    public static void ValidateHeaders(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (rows == null || rows.Count == 0)
            throw new ArgumentException("Localization worksheet is empty.", nameof(rows));

        if (!rows[0].Any(header => string.Equals(header, "Key", StringComparison.Ordinal)))
            throw new ArgumentException("Localization worksheet must contain a Key column.", nameof(rows));
    }

    public static string ToCsv(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (rows == null)
            throw new ArgumentNullException(nameof(rows));

        if (rows.Count == 0)
            return string.Empty;

        int columnCount = rows[0]?.Count ?? 0;
        var csvRows = new List<string>();
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            IReadOnlyList<string> row = rows[rowIndex] ?? Array.Empty<string>();
            if (rowIndex > 0 && row.All(string.IsNullOrEmpty))
                continue;

            var builder = new StringBuilder();
            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                if (columnIndex > 0)
                    builder.Append(',');

                string value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                builder.Append(EscapeCsv(value));
            }

            csvRows.Add(builder.ToString());
        }

        return string.Join(Environment.NewLine, csvRows);
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var values = new List<string>();
        ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return values;

        using Stream stream = entry.Open();
        XDocument document = XDocument.Load(stream);
        foreach (XElement item in document.Descendants(SpreadsheetNamespace + "si"))
        {
            values.Add(string.Concat(item.Descendants(SpreadsheetNamespace + "t").Select(node => node.Value)));
        }

        return values;
    }

    private static ZipArchiveEntry FindSheetEntry(ZipArchive archive, string sheetName)
    {
        XDocument workbook = LoadXml(archive, "xl/workbook.xml");
        XDocument relationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");

        XElement sheet = workbook
            .Descendants(SpreadsheetNamespace + "sheet")
            .FirstOrDefault(candidate => string.Equals((string)candidate.Attribute("name"), sheetName, StringComparison.Ordinal));

        if (sheet == null)
            throw new InvalidDataException($"Worksheet '{sheetName}' was not found.");

        string relationshipId = (string)sheet.Attribute(DocumentRelationshipNamespace + "id");
        XElement relationship = relationships
            .Descendants(PackageRelationshipNamespace + "Relationship")
            .FirstOrDefault(candidate => string.Equals((string)candidate.Attribute("Id"), relationshipId, StringComparison.Ordinal));

        string target = (string)relationship?.Attribute("Target");
        if (string.IsNullOrWhiteSpace(target))
            throw new InvalidDataException($"Worksheet relationship for '{sheetName}' was not found.");

        string normalizedPath = target.Replace('\\', '/').TrimStart('/');
        if (!normalizedPath.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            normalizedPath = "xl/" + normalizedPath;

        ZipArchiveEntry entry = archive.GetEntry(normalizedPath);
        if (entry == null)
            throw new InvalidDataException($"Worksheet data for '{sheetName}' was not found.");

        return entry;
    }

    private static XDocument LoadXml(ZipArchive archive, string path)
    {
        ZipArchiveEntry entry = archive.GetEntry(path);
        if (entry == null)
            throw new InvalidDataException($"Invalid xlsx file: '{path}' is missing.");

        using Stream stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        string type = (string)cell.Attribute("t") ?? string.Empty;
        if (type == "inlineStr")
            return string.Concat(cell.Descendants(SpreadsheetNamespace + "t").Select(node => node.Value));

        string rawValue = cell.Element(SpreadsheetNamespace + "v")?.Value ?? string.Empty;
        if (type == "s" && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
        {
            if (index < 0 || index >= sharedStrings.Count)
                throw new InvalidDataException($"Shared string index '{index}' is invalid.");

            return sharedStrings[index];
        }

        return rawValue;
    }

    private static int GetColumnIndex(string cellReference)
    {
        int index = 0;
        foreach (char character in cellReference)
        {
            if (!char.IsLetter(character))
                break;

            index = index * 26 + char.ToUpperInvariant(character) - 'A' + 1;
        }

        return index - 1;
    }

    private static string EscapeCsv(string value)
    {
        value ??= string.Empty;
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
