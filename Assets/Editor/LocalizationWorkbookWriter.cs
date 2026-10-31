using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

public static class LocalizationWorkbookWriter
{
    public const string KoreanSourceHeader = "Korean Source";
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeRelationship = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationship = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static void SynchronizeEntries(
        string workbookPath,
        IEnumerable<LocalizationWorkbookEntry> updates,
        IEnumerable<LocalizationWorkbookEntry> additions,
        IEnumerable<string> removals)
    {
        EnsureKoreanSourceColumn(workbookPath);
        string transactionPath = workbookPath + $".sync-{Guid.NewGuid():N}.tmp";
        string transactionBackupPath = transactionPath + ".localization-manager.backup";
        string backupPath = workbookPath + ".localization-manager.backup";

        File.Copy(workbookPath, transactionPath, true);
        try
        {
            UpdateExistingEntries(transactionPath, updates);
            MergeNewEntries(transactionPath, additions);
            RemoveEntries(transactionPath, removals);

            IReadOnlyList<IReadOnlyList<string>> rows = LocalizationXlsxReader.ReadSheet(
                transactionPath,
                LocalizationExcelImporter.WorksheetName);
            LocalizationXlsxReader.ValidateHeaders(rows);

            File.Copy(workbookPath, backupPath, true);
            File.Replace(transactionPath, workbookPath, null, true);
        }
        finally
        {
            if (File.Exists(transactionPath))
                File.Delete(transactionPath);
            if (File.Exists(transactionBackupPath))
                File.Delete(transactionBackupPath);
        }
    }

    /// <summary>
    /// Keep the workbook columns in the editor-friendly order:
    /// Key, Id, Korean Source, Korean(ko), English(en), zh-Hans, zh-Hant, Japanese(ja), Spanish(es).
    /// Existing cell values, translations and SharedTable IDs are preserved.
    /// </summary>
    public static void EnsureKoreanSourceColumn(string workbookPath)
    {
        if (!File.Exists(workbookPath))
            return;

        var rows = LocalizationXlsxReader.ReadSheet(workbookPath, LocalizationExcelImporter.WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);
        var originalHeaders = rows[0];
        int keyIndex = FindHeader(originalHeaders, "Key");
        int idIndex = FindHeader(originalHeaders, "Id");
        int koreanIndex = FindHeader(originalHeaders, "Korean(ko)");
        int sourceIndex = FindHeader(originalHeaders, KoreanSourceHeader);
        if (keyIndex < 0 || idIndex < 0 || koreanIndex < 0)
            throw new InvalidDataException("Localization.xlsx must contain Key, Id and Korean(ko).");

        // No rewrite when the workbook already follows the desired schema.
        if (keyIndex == 0 && idIndex == 1 && sourceIndex == 2 && koreanIndex == 3)
            return;

        string backupPath = workbookPath + ".before-korean-source.backup.xlsx";
        if (!File.Exists(backupPath))
            File.Copy(workbookPath, backupPath);

        string tempPath = workbookPath + ".korean-source-" + Guid.NewGuid().ToString("N") + ".tmp";
        File.Copy(workbookPath, tempPath, true);
        try
        {
            using (var archive = ZipFile.Open(tempPath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = FindTextSheet(archive);
                string entryName = entry.FullName;
                XDocument document;
                using (Stream input = entry.Open())
                    document = XDocument.Load(input);

                // Preserve the existing order of translation languages; move only metadata.
                var targetOrder = new List<int> { keyIndex, idIndex, sourceIndex };
                for (int i = 0; i < originalHeaders.Count; i++)
                    if (i != keyIndex && i != idIndex && i != sourceIndex)
                        targetOrder.Add(i);

                int rowOffset = 0;
                foreach (XElement row in document.Descendants(Spreadsheet + "row"))
                {
                    int excelRow = (int?)row.Attribute("r") ?? (rowOffset + 1);
                    var existing = row.Elements(Spreadsheet + "c").ToDictionary(
                        cell => ParseColumnIndex((string)cell.Attribute("r")), cell => cell);
                    // When upgrading old files, initialize Source from the edited Korean column.
                    if (sourceIndex < 0)
                    {
                        string value = rowOffset == 0 ? KoreanSourceHeader :
                            rowOffset < rows.Count ? Value(rows[rowOffset], koreanIndex) : string.Empty;
                        if (!string.IsNullOrEmpty(value))
                            existing[originalHeaders.Count] = InlineCell(originalHeaders.Count, excelRow, value);
                    }

                    // Source must be the third column, regardless of its previous position.
                    var actualOrder = targetOrder.Select(i => i < 0 ? originalHeaders.Count : i).ToArray();
                    foreach (XElement cell in row.Elements(Spreadsheet + "c").ToArray())
                        cell.Remove();
                    for (int newIndex = 0; newIndex < actualOrder.Length; newIndex++)
                    {
                        if (!existing.TryGetValue(actualOrder[newIndex], out XElement cell))
                            continue;
                        cell.SetAttributeValue("r", ColumnName(newIndex) + excelRow);
                        row.Add(cell);
                    }
                    rowOffset++;
                }

                XElement dimension = document.Root?.Element(Spreadsheet + "dimension");
                if (dimension != null)
                {
                    int lastRow = document.Descendants(Spreadsheet + "row")
                        .Select(row => (int?)row.Attribute("r") ?? 0).DefaultIfEmpty(1).Max();
                    dimension.SetAttributeValue("ref", "A1:" + ColumnName(targetOrder.Count - 1) + lastRow);
                }

                entry.Delete();
                ZipArchiveEntry replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using (Stream output = replacement.Open())
                    document.Save(output);
            }

            var verified = LocalizationXlsxReader.ReadSheet(tempPath, LocalizationExcelImporter.WorksheetName);
            if (verified.Count != rows.Count || FindHeader(verified[0], KoreanSourceHeader) != 2 ||
                FindHeader(verified[0], "Korean(ko)") != 3)
                throw new InvalidDataException("Localization worksheet column order verification failed.");
            File.Copy(tempPath, workbookPath, true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static int ParseColumnIndex(string cellReference)
    {
        int result = 0;
        foreach (char character in cellReference ?? string.Empty)
        {
            if (character < 'A' || character > 'Z') break;
            result = result * 26 + (character - 'A' + 1);
        }
        return result - 1;
    }

    public static int MergeNewEntries(string workbookPath, IEnumerable<LocalizationWorkbookEntry> entries)
    {
        if (entries == null)
            return 0;

        EnsureKoreanSourceColumn(workbookPath);
        var rows = LocalizationXlsxReader.ReadSheet(workbookPath, LocalizationExcelImporter.WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);
        IReadOnlyList<string> headers = rows[0];
        int keyIndex = FindHeader(headers, "Key");
        int koreanIndex = FindHeader(headers, "Korean(ko)");
        int sourceIndex = FindHeader(headers, KoreanSourceHeader);
        var existingKeys = new HashSet<string>(rows.Skip(1).Select(row => Value(row, keyIndex)), StringComparer.Ordinal);
        List<LocalizationWorkbookEntry> additions = entries
            .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Key) && !existingKeys.Contains(entry.Key))
            .GroupBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
        if (additions.Count == 0)
            return 0;

        foreach (LocalizationWorkbookEntry addition in additions)
        {
            ValidateXmlText(addition.Key, addition.Key, "Key");
            ValidateXmlText(addition.Korean, addition.Key, "Korean(ko)");
        }

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
                string value = column == keyIndex ? addition.Key :
                    (column == koreanIndex || column == sourceIndex) ? addition.Korean : string.Empty;
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

    /// <summary>
    /// 게임데이터의 안정 키는 유지한 채 한국어 원문만 갱신합니다.
    /// 원문 변경으로 의미가 달라질 수 있는 번역 열은 비워 로케일별 미번역 표기로 처리합니다.
    /// </summary>
    public static int UpdateExistingEntries(string workbookPath, IEnumerable<LocalizationWorkbookEntry> entries)
    {
        if (entries == null)
            return 0;

        EnsureKoreanSourceColumn(workbookPath);
        LocalizationWorkbookEntry[] sourceEntries = entries.Where(entry => entry != null).ToArray();
        foreach (LocalizationWorkbookEntry entry in sourceEntries)
        {
            ValidateXmlText(entry.Key, entry.Key, "Key");
            ValidateXmlText(entry.Korean, entry.Key, "Korean(ko)");
        }

        var rows = LocalizationXlsxReader.ReadSheet(workbookPath, LocalizationExcelImporter.WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);
        IReadOnlyList<IReadOnlyList<string>> updatedRows = ApplySourceUpdatesToRows(rows, sourceEntries);
        var changedKeys = rows.Skip(1)
            .Zip(updatedRows.Skip(1), (before, after) => new { before, after })
            .Where(pair => !pair.before.SequenceEqual(pair.after))
            .Select(pair => Value(pair.after, FindHeader(updatedRows[0], "Key")))
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.Ordinal);
        if (changedKeys.Count == 0)
            return 0;

        string backupPath = workbookPath + ".localization-manager.backup";
        File.Copy(workbookPath, backupPath, true);
        using var archive = ZipFile.Open(workbookPath, ZipArchiveMode.Update);
        ZipArchiveEntry sheetEntry = FindTextSheet(archive);
        IReadOnlyList<string> sharedStrings = ReadSharedStrings(archive);
        XDocument document;
        using (Stream stream = sheetEntry.Open())
            document = XDocument.Load(stream);
        sheetEntry.Delete();

        IReadOnlyList<string> headers = updatedRows[0];
        int keyIndex = FindHeader(headers, "Key");
        var rowsByKey = updatedRows.Skip(1)
            .Where(row => changedKeys.Contains(Value(row, keyIndex)))
            .ToDictionary(row => Value(row, keyIndex), StringComparer.Ordinal);
        foreach (XElement row in document.Descendants(Spreadsheet + "row"))
        {
            string key = ReadCellValue(row, keyIndex, sharedStrings);
            if (!rowsByKey.TryGetValue(key, out IReadOnlyList<string> updated))
                continue;

            for (int column = 0; column < headers.Count; column++)
                SetCellValue(row, column, Value(updated, column));
        }

        ZipArchiveEntry replacement = archive.CreateEntry(sheetEntry.FullName, CompressionLevel.Optimal);
        using Stream output = replacement.Open();
        document.Save(output);
        return changedKeys.Count;
    }

    public static IReadOnlyList<IReadOnlyList<string>> ApplySourceUpdatesToRows(
        IReadOnlyList<IReadOnlyList<string>> rows,
        IEnumerable<LocalizationWorkbookEntry> entries)
    {
        if (rows == null || rows.Count == 0)
            return rows ?? Array.Empty<IReadOnlyList<string>>();

        IReadOnlyList<string> headers = rows[0];
        int keyIndex = FindHeader(headers, "Key");
        int koreanIndex = FindHeader(headers, "Korean(ko)");
        int sourceIndex = FindHeader(headers, KoreanSourceHeader);
        if (keyIndex < 0 || sourceIndex < 0)
            return rows;

        var sourcesByKey = (entries ?? Array.Empty<LocalizationWorkbookEntry>())
            .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Key))
            .GroupBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Korean ?? string.Empty, StringComparer.Ordinal);
        var result = new List<IReadOnlyList<string>>(rows.Count) { headers.ToArray() };
        foreach (IReadOnlyList<string> sourceRow in rows.Skip(1))
        {
            var row = sourceRow.ToList();
            if (sourcesByKey.TryGetValue(Value(row, keyIndex), out string korean) &&
                !string.Equals(Value(row, sourceIndex), korean, StringComparison.Ordinal))
            {
                while (row.Count < headers.Count)
                    row.Add(string.Empty);
                // The collected source is updated, but the player's Korean localization
                // and all translated languages are kept exactly as edited.
                row[sourceIndex] = korean;
            }
            result.Add(row);
        }

        return result;
    }

    public static int RemoveEntries(string workbookPath, IEnumerable<string> keys)
    {
        var keysToRemove = (keys ?? Array.Empty<string>())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.Ordinal);
        if (keysToRemove.Count == 0)
            return 0;

        var rows = LocalizationXlsxReader.ReadSheet(workbookPath, LocalizationExcelImporter.WorksheetName);
        IReadOnlyList<IReadOnlyList<string>> remaining = RemoveEntriesFromRows(rows, keysToRemove);
        int removed = rows.Count - remaining.Count;
        if (removed == 0)
            return 0;

        int keyIndex = FindHeader(rows[0], "Key");
        string backupPath = workbookPath + ".localization-manager.backup";
        File.Copy(workbookPath, backupPath, true);
        using var archive = ZipFile.Open(workbookPath, ZipArchiveMode.Update);
        ZipArchiveEntry sheetEntry = FindTextSheet(archive);
        IReadOnlyList<string> sharedStrings = ReadSharedStrings(archive);
        XDocument document;
        using (Stream stream = sheetEntry.Open())
            document = XDocument.Load(stream);
        sheetEntry.Delete();

        foreach (XElement row in document.Descendants(Spreadsheet + "row").ToArray())
        {
            if (keysToRemove.Contains(ReadCellValue(row, keyIndex, sharedStrings)))
                row.Remove();
        }

        ZipArchiveEntry replacement = archive.CreateEntry(sheetEntry.FullName, CompressionLevel.Optimal);
        using Stream output = replacement.Open();
        document.Save(output);
        return removed;
    }

    public static IReadOnlyList<IReadOnlyList<string>> RemoveEntriesFromRows(
        IReadOnlyList<IReadOnlyList<string>> rows,
        IEnumerable<string> keys)
    {
        if (rows == null || rows.Count == 0)
            return rows ?? Array.Empty<IReadOnlyList<string>>();

        int keyIndex = FindHeader(rows[0], "Key");
        var keysToRemove = (keys ?? Array.Empty<string>())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.Ordinal);
        var result = new List<IReadOnlyList<string>> { rows[0] };
        result.AddRange(rows.Skip(1).Where(row => !keysToRemove.Contains(Value(row, keyIndex))));
        return result;
    }

    public static int CompactBlankRows(string workbookPath)
    {
        var rows = LocalizationXlsxReader.ReadSheet(workbookPath, LocalizationExcelImporter.WorksheetName);
        LocalizationXlsxReader.ValidateHeaders(rows);
        IReadOnlyList<IReadOnlyList<string>> compacted = CompactRows(rows);
        int removed = rows.Count - compacted.Count;

        int keyIndex = FindHeader(rows[0], "Key");
        string backupPath = workbookPath + ".localization-manager.backup";
        File.Copy(workbookPath, backupPath, true);
        using var archive = ZipFile.Open(workbookPath, ZipArchiveMode.Update);
        ZipArchiveEntry sheetEntry = FindTextSheet(archive);
        IReadOnlyList<string> sharedStrings = ReadSharedStrings(archive);
        XDocument document;
        using (Stream stream = sheetEntry.Open())
            document = XDocument.Load(stream);

        List<XElement> retainedRows = document.Descendants(Spreadsheet + "row")
            .Where((row, index) => index == 0 || !string.IsNullOrWhiteSpace(ReadCellValue(row, keyIndex, sharedStrings)))
            .ToList();
        int declaredLastRow = ReadDeclaredLastRow(document.Root?.Element(Spreadsheet + "dimension"));
        bool hasRowGaps = retainedRows.Select((row, index) => (string)row.Attribute("r") != (index + 1).ToString(CultureInfo.InvariantCulture)).Any(value => value);
        if (removed == 0 && !RequiresCompaction(retainedRows.Count, declaredLastRow) && !hasRowGaps)
            return 0;

        sheetEntry.Delete();
        foreach (XElement row in document.Descendants(Spreadsheet + "row").ToArray())
            if (!retainedRows.Contains(row))
                row.Remove();

        for (int rowIndex = 0; rowIndex < retainedRows.Count; rowIndex++)
        {
            XElement row = retainedRows[rowIndex];
            int excelRow = rowIndex + 1;
            row.SetAttributeValue("r", excelRow);
            foreach (XElement cell in row.Elements(Spreadsheet + "c"))
            {
                string reference = (string)cell.Attribute("r") ?? string.Empty;
                string column = new string(reference.TakeWhile(char.IsLetter).ToArray());
                cell.SetAttributeValue("r", column + excelRow);
            }
        }

        XElement dimension = document.Root?.Element(Spreadsheet + "dimension");
        if (dimension != null)
            dimension.SetAttributeValue("ref", $"A1:{ColumnName(rows[0].Count - 1)}{retainedRows.Count}");

        ZipArchiveEntry replacement = archive.CreateEntry(sheetEntry.FullName, CompressionLevel.Optimal);
        using Stream output = replacement.Open();
        document.Save(output);
        return removed;
    }

    public static IReadOnlyList<IReadOnlyList<string>> CompactRows(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (rows == null || rows.Count == 0)
            return rows ?? Array.Empty<IReadOnlyList<string>>();

        int keyIndex = FindHeader(rows[0], "Key");
        var result = new List<IReadOnlyList<string>> { rows[0] };
        result.AddRange(rows.Skip(1).Where(row => !string.IsNullOrWhiteSpace(Value(row, keyIndex))));
        return result;
    }

    public static bool RequiresCompaction(int dataRowCount, int declaredLastRow)
    {
        return declaredLastRow > dataRowCount;
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
    private static void ValidateXmlText(string value, string key, string fieldName)
    {
        if (string.IsNullOrEmpty(value))
            return;

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (XmlConvert.IsXmlChar(character))
                continue;

            if (index + 1 < value.Length &&
                XmlConvert.IsXmlSurrogatePair(value[index + 1], character))
            {
                index++;
                continue;
            }

            throw new InvalidDataException(
                $"Localization key '{key}' field '{fieldName}' contains XML-invalid character U+{(int)character:X4}.");
        }
    }
    private static string Value(IReadOnlyList<string> row, int index) => index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;
    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return Array.Empty<string>();

        using Stream stream = entry.Open();
        XDocument document = XDocument.Load(stream);
        return document.Descendants(Spreadsheet + "si")
            .Select(item => string.Concat(item.Descendants(Spreadsheet + "t").Select(value => value.Value)))
            .ToArray();
    }

    private static string ReadCellValue(XElement row, int column, IReadOnlyList<string> sharedStrings)
    {
        if (column < 0)
            return string.Empty;

        string reference = ColumnName(column) + ((string)row.Attribute("r") ?? string.Empty);
        XElement cell = row.Elements(Spreadsheet + "c")
            .FirstOrDefault(value => string.Equals((string)value.Attribute("r"), reference, StringComparison.Ordinal));
        if (cell == null)
            return string.Empty;

        XElement inlineString = cell.Element(Spreadsheet + "is");
        if (inlineString != null)
            return string.Concat(inlineString.Descendants(Spreadsheet + "t").Select(value => value.Value));

        string rawValue = cell.Element(Spreadsheet + "v")?.Value ?? string.Empty;
        if (string.Equals((string)cell.Attribute("t"), "s", StringComparison.Ordinal) &&
            int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) &&
            index >= 0 && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return rawValue;
    }

    private static void SetCellValue(XElement row, int column, string value)
    {
        string rowNumber = (string)row.Attribute("r") ?? string.Empty;
        string reference = ColumnName(column) + rowNumber;
        XElement cell = row.Elements(Spreadsheet + "c")
            .FirstOrDefault(candidate => string.Equals((string)candidate.Attribute("r"), reference, StringComparison.Ordinal));
        if (string.IsNullOrEmpty(value))
        {
            cell?.Remove();
            return;
        }

        if (cell == null)
        {
            cell = new XElement(Spreadsheet + "c", new XAttribute("r", reference));
            row.Add(cell);
        }

        cell.RemoveNodes();
        cell.SetAttributeValue("t", "inlineStr");
        cell.Add(new XElement(Spreadsheet + "is", new XElement(Spreadsheet + "t", value)));
    }

    private static int ReadDeclaredLastRow(XElement dimension)
    {
        string reference = (string)dimension?.Attribute("ref") ?? string.Empty;
        int index = reference.Length;
        while (index > 0 && char.IsDigit(reference[index - 1]))
            index--;
        return int.TryParse(reference.Substring(index), NumberStyles.Integer, CultureInfo.InvariantCulture, out int row)
            ? row
            : 0;
    }
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
