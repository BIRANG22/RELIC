#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;

/// <summary>
/// Explicit, reviewable key migration. Never changes a Unity SharedTableData Id.
/// The operator approves old/new pairs in Assets/ExcelSource/LocalizationKeyMigration.tsv.
/// </summary>
public static class LocalizationLegacyKeyMigration
{
    private const string MappingPath = "Assets/ExcelSource/LocalizationKeyMigration.tsv";
    private const string Workbook = "Assets/ExcelSource/Localization.xlsx";

    [MenuItem("Tools/Localization/기존 키 정리/1 - 키 변경 제안 내보내기")]
    public static void ExportProposals()
    {
        var keys = LocalizationWorkbookKeyRenamer.ReadKeys(Workbook);
        var occupied = new HashSet<string>(keys, StringComparer.Ordinal);
        var output = new List<string> { "OldKey\tNewKey\tApprove" };
        foreach (string oldKey in keys)
        {
            if (!oldKey.StartsWith("ui.", StringComparison.Ordinal) || !System.Text.RegularExpressions.Regex.IsMatch(oldKey, @"\.[0-9a-f]{8}$")) continue;
            string proposed = Propose(oldKey);
            // Non-unique/ambiguous entries intentionally need manual editing; never overwrite another key.
            if (occupied.Contains(proposed)) proposed = "";
            output.Add(Safe(oldKey) + "\t" + Safe(proposed) + "\tNO");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(MappingPath));
        File.WriteAllLines(MappingPath, output, new UTF8Encoding(false));
        AssetDatabase.Refresh();
        Debug.Log($"[Localization] Exported {output.Count - 1} proposals. Edit NewKey and set Approve=YES only for verified pairs: {MappingPath}");
    }

    [MenuItem("Tools/Localization/기존 키 정리/2 - 승인된 키 변경 적용")]
    public static void ApplyApproved()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        if (!File.Exists(MappingPath)) throw new FileNotFoundException(MappingPath);
        var approved = new Dictionary<string, string>(StringComparer.Ordinal);
        var skipped = new List<string>();
        foreach (string line in File.ReadAllLines(MappingPath).Skip(1))
        {
            string[] columns = line.Split('\t');
            if (columns.Length < 3 || !columns[2].Trim().Equals("YES", StringComparison.OrdinalIgnoreCase)) continue;
            string oldKey = columns[0].Trim();
            string newKey = columns[1].Trim();
            if (string.IsNullOrWhiteSpace(oldKey) || string.IsNullOrWhiteSpace(newKey) || oldKey == newKey)
            { skipped.Add(oldKey + " -> " + newKey + " : invalid or unchanged key"); continue; }
            if (approved.ContainsKey(oldKey))
            { skipped.Add(oldKey + " -> " + newKey + " : duplicated source"); continue; }
            approved.Add(oldKey, newKey);
        }
        if (approved.Count == 0) { Debug.Log("[Localization] No valid approved migrations."); return; }
        var collection = LocalizationEditorSettings.GetStringTableCollection("Text");
        if (collection == null) throw new InvalidOperationException("Text StringTableCollection unavailable.");
        var shared = collection.SharedData;
        // Workbook-only entries are valid migration sources too. Many scanned keys have
        // not yet been imported into Unity's SharedTableData.
        var workbookKeys = new HashSet<string>(LocalizationWorkbookKeyRenamer.ReadKeys(Workbook)
            .Select(k => k.Trim().TrimStart('\uFEFF')), StringComparer.Ordinal);
        Debug.Log($"[Localization Migration] Workbook: {Path.GetFullPath(Workbook)} / workbook keys={workbookKeys.Count} / shared keys={shared.Entries.Count} / approved={approved.Count}");
        var workbookIds = ReadWorkbookIds(Workbook);
        var destinations = new HashSet<string>(StringComparer.Ordinal);
        var workbookMappings = new Dictionary<string, string>(StringComparer.Ordinal);
        var sharedMappings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in approved)
        {
            string oldKey = pair.Key.Trim().TrimStart('\uFEFF');
            string newKey = pair.Value.Trim().TrimStart('\uFEFF');
            bool inWorkbook = workbookKeys.Contains(oldKey);
            bool inShared = shared.GetEntry(oldKey) != null;
            if (!inWorkbook && !inShared)
            { skipped.Add(pair.Key + " -> " + pair.Value + " : missing in workbook and String Table"); continue; }
            // Keep existing destination keys and IDs intact. If an unrelated entry
            // already uses the desired semantic key, use a short deterministic
            // disambiguator derived from the legacy key instead of merging IDs.
            string desiredKey = newKey;
            var targetSharedEntry = shared.GetEntry(newKey);
            if (workbookKeys.Contains(newKey) || (targetSharedEntry != null &&
                !(inWorkbook && !inShared && workbookIds.TryGetValue(oldKey, out string checkId)
                  && string.Equals(checkId, targetSharedEntry.Id.ToString(), StringComparison.Ordinal))))
            {
                string suffix = oldKey.Substring(oldKey.LastIndexOf('.') + 1);
                // Don't invent a duplicate if this key is already occupied.
                string candidate = desiredKey + "_" + suffix;
                int variant = 2;
                while (workbookKeys.Contains(candidate) || shared.GetEntry(candidate) != null || destinations.Contains(candidate))
                    candidate = desiredKey + "_" + suffix + "_" + variant++;
                newKey = candidate;
                targetSharedEntry = null;
                Debug.Log($"[Localization Migration] 충돌 방지 키 변경: {oldKey} -> {newKey} (원래 제안: {desiredKey})");
            }
            if (targetSharedEntry != null)
            {
                // A previously migrated SharedData key may coexist with the old workbook key.
                // Reconcile only when their numeric IDs agree; never merge different entries.
                string workbookId;
                bool sameId = inWorkbook && !inShared &&
                    workbookIds.TryGetValue(oldKey, out workbookId) &&
                    string.Equals(workbookId, targetSharedEntry.Id.ToString(), StringComparison.Ordinal);
                if (!sameId)
                { skipped.Add(pair.Key + " -> " + pair.Value + " : destination already exists (ID mismatch or source also in SharedData)"); continue; }
                if (!destinations.Add(newKey))
                { skipped.Add(pair.Key + " -> " + pair.Value + " : duplicate destination"); continue; }
                workbookMappings.Add(oldKey, newKey);
                continue;
            }
            if (!destinations.Add(newKey))
            { skipped.Add(pair.Key + " -> " + pair.Value + " : duplicate destination"); continue; }
            if (inWorkbook) workbookMappings.Add(oldKey, newKey);
            if (inShared) sharedMappings.Add(oldKey, newKey);
        }
        // A hardcoded key must be changed at its call site before migration.
        foreach (string file in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories))
        {
            if (file.Replace('\\', '/').EndsWith("LocalizationLegacyKeyMigration.cs", StringComparison.Ordinal)) continue;
            string code = File.ReadAllText(file);
            foreach (var pair in approved)
            {
                if ((!workbookMappings.ContainsKey(pair.Key) && !sharedMappings.ContainsKey(pair.Key)) ||
                    !code.Contains("\"" + pair.Key + "\"")) continue;
                skipped.Add(pair.Key + " -> " + pair.Value + " : C# literal reference in " + file);
                workbookMappings.Remove(pair.Key);
                sharedMappings.Remove(pair.Key);
            }
        }
        string reportPath = "Assets/ExcelSource/LocalizationKeyMigrationSkipped.tsv";
        var report = new List<string> { "OldKey\tNewKey\tReason" };
        foreach (string item in skipped)
        {
            int arrow = item.IndexOf(" -> ", StringComparison.Ordinal);
            int reason = item.IndexOf(" : ", StringComparison.Ordinal);
            if (arrow >= 0 && reason > arrow)
                report.Add(item.Substring(0, arrow) + "\t" + item.Substring(arrow + 4, reason - arrow - 4) + "\t" + item.Substring(reason + 3));
            else report.Add(item.Replace(" -> ", "\t"));
        }
        File.WriteAllLines(reportPath, report, new UTF8Encoding(false));
        var reasonCounts = skipped.Select(item => {
            int delimiter = item.IndexOf(" : ", StringComparison.Ordinal);
            return delimiter >= 0 ? item.Substring(delimiter + 3) : "unknown";
        }).Select(reason => reason.StartsWith("C# literal reference", StringComparison.Ordinal)
            ? "C# literal reference" : reason)
          .GroupBy(reason => reason, StringComparer.Ordinal)
          .OrderByDescending(group => group.Count())
          .Select(group => $"{group.Key}={group.Count()}");
        Debug.Log("[Localization Migration] Skipped reasons: " + string.Join("; ", reasonCounts));
        if (skipped.Count > 0)
            Debug.LogWarning("[Localization Migration] Example: " + skipped[0]);
        AssetDatabase.ImportAsset(reportPath);
        if (skipped.Count > 0)
            Debug.LogWarning($"[Localization] Skipped {skipped.Count} unsafe migrations. Review {reportPath} before resolving collisions. Existing keys and IDs were not merged.");
        int eligibleCount = workbookMappings.Keys.Union(sharedMappings.Keys).Count();
        if (eligibleCount == 0) { Debug.LogWarning("[Localization] No safe migration remains to apply."); return; }
        if (!EditorUtility.DisplayDialog("Localization key migration",
            $"변경 가능 {eligibleCount}개 (엑셀 {workbookMappings.Count}개, String Table {sharedMappings.Count}개). 제외 {skipped.Count}개. 기존 프로젝트와 엑셀을 백업했는지 확인하세요.", "Apply", "Cancel")) return;
        string backupFolder = "Assets/ExcelSource/LocalizationMigrationBackup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(backupFolder);
        File.Copy(Workbook, Path.Combine(backupFolder, "Localization.xlsx"));
        // Apply scene/prefab binding changes only where the serialized YAML contains an old key.
        // Third-party and broken test scenes must not block the workbook migration.
        var originalScenes = EditorSceneManager.GetSceneManagerSetup();
        int changed = 0;
        int failedAssets = 0;
        try
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!MayContainMappedKeys(path, sharedMappings)) continue;
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    int count = ReplaceBindings(root, sharedMappings);
                    if (count > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); changed += count; }
                }
                catch (Exception ex)
                {
                    failedAssets++;
                    Debug.LogWarning($"[Localization Migration] 프리팹 바인딩 건너뜀: {path} / {ex.Message}");
                }
                finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!MayContainMappedKeys(path, sharedMappings)) continue;
                try
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    int count = scene.GetRootGameObjects().Sum(root => ReplaceBindings(root, sharedMappings));
                    if (count > 0) { EditorSceneManager.SaveScene(scene); changed += count; }
                }
                catch (Exception ex)
                {
                    failedAssets++;
                    Debug.LogWarning($"[Localization Migration] 씬 바인딩 건너뜀: {path} / {ex.Message}");
                }
            }
        }
        finally
        {
            // OpenScene and RestoreSceneManagerSetup can unload and recreate ScriptableObjects.
            EditorSceneManager.RestoreSceneManagerSetup(originalScenes);
        }
        // Never use the SharedTableData reference captured before opening scenes.
        collection = LocalizationEditorSettings.GetStringTableCollection("Text");
        if (collection == null || collection.SharedData == null)
            throw new InvalidOperationException("Text StringTableCollection/SharedData를 다시 불러오지 못했습니다. 현재 상태에서 키 변경을 중단합니다.");
        shared = collection.SharedData;
        if (failedAssets > 0)
            throw new InvalidOperationException($"씬/프리팹 참조 갱신 실패 {failedAssets}개. String Table 및 엑셀 변경을 중단했습니다. 수정된 일부 에셋은 백업과 대조해 주세요.");
        int renamedShared = 0;
        foreach (var pair in sharedMappings)
        {
            var source = shared.GetEntry(pair.Key);
            var destination = shared.GetEntry(pair.Value);
            if (source == null)
            {
                // A previous partial run may have renamed the shared entry already.
                if (destination != null) continue;
                Debug.LogWarning($"[Localization Migration] SharedData 원본 키가 없어 보류: {pair.Key}");
                continue;
            }
            if (destination != null)
            {
                Debug.LogWarning($"[Localization Migration] SharedData 목적지 충돌로 보류: {pair.Value}");
                continue;
            }
            shared.RenameKey(source.Id, pair.Value);
            renamedShared++;
        }
        if (renamedShared > 0)
        {
            EditorUtility.SetDirty(shared);
            AssetDatabase.SaveAssets();
        }
        if (failedAssets > 0)
            Debug.LogWarning($"[Localization Migration] 문제가 발생한 씬/프리팹 {failedAssets}개. 해당 에셋의 바인딩은 별도 확인 필요.");
        // Workbook key is renamed without touching Id or any translation column.
        if (workbookMappings.Count > 0)
            LocalizationWorkbookKeyRenamer.RenameKeys(Workbook, workbookMappings);
        AssetDatabase.Refresh();
        Debug.Log($"[Localization] Migrated {eligibleCount} keys (workbook {workbookMappings.Count}, String Table {sharedMappings.Count}) / {changed} serialized bindings. Backup workbook: {backupFolder}");
    }

    // Read Key/Id without changing workbook content. IDs are read as text to avoid precision loss.
    private static Dictionary<string, string> ReadWorkbookIds(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        using (var archive = ZipFile.OpenRead(path))
        {
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace packageNs = "http://schemas.openxmlformats.org/package/2006/relationships";
            var book = XDocument.Load(archive.GetEntry("xl/workbook.xml").Open());
            var sheet = book.Descendants(ns + "sheet").First(x => (string)x.Attribute("name") == "Text");
            var rels = XDocument.Load(archive.GetEntry("xl/_rels/workbook.xml.rels").Open());
            string target = (string)rels.Descendants(packageNs + "Relationship")
                .First(x => (string)x.Attribute("Id") == (string)sheet.Attribute(relNs + "id")).Attribute("Target");
            string sheetPath = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target.TrimStart('/');
            var doc = XDocument.Load(archive.GetEntry(sheetPath).Open());
            var stringsPart = archive.GetEntry("xl/sharedStrings.xml");
            string[] strings = stringsPart == null ? Array.Empty<string>() : XDocument.Load(stringsPart.Open())
                .Descendants(ns + "si").Select(x => string.Concat(x.Descendants(ns + "t").Select(t => (string)t))).ToArray();
            string ReadCell(XElement cell)
            {
                if (cell == null) return "";
                string type = (string)cell.Attribute("t");
                if (type == "s") return strings[int.Parse((string)cell.Element(ns + "v"))];
                return (string)cell.Element(ns + "is")?.Element(ns + "t") ?? (string)cell.Element(ns + "v") ?? "";
            }
            var rows = doc.Descendants(ns + "sheetData").Elements(ns + "row").ToList();
            if (rows.Count == 0) return result;
            var first = rows[0].Elements(ns + "c").ToList();
            string keyColumn = new string(((string)first.First(x => ReadCell(x) == "Key").Attribute("r")).TakeWhile(char.IsLetter).ToArray());
            string idColumn = new string(((string)first.First(x => ReadCell(x) == "Id").Attribute("r")).TakeWhile(char.IsLetter).ToArray());
            foreach (var row in rows.Skip(1))
            {
                var cells = row.Elements(ns + "c").ToList();
                string key = ReadCell(cells.FirstOrDefault(x => new string(((string)x.Attribute("r")).TakeWhile(char.IsLetter).ToArray()) == keyColumn)).Trim();
                string id = ReadCell(cells.FirstOrDefault(x => new string(((string)x.Attribute("r")).TakeWhile(char.IsLetter).ToArray()) == idColumn)).Trim();
                if (!string.IsNullOrEmpty(key)) result[key] = id;
            }
        }
        return result;
    }

    private static int ReplaceBindings(GameObject root, IDictionary<string, string> map)
    {
        int count = 0;
        foreach (var component in root.GetComponentsInChildren<LocalizedTMPText>(true))
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty("localizationKey");
            if (prop != null && map.TryGetValue(prop.stringValue, out string next))
            { prop.stringValue = next; so.ApplyModifiedPropertiesWithoutUndo(); count++; }
        }
        foreach (var component in root.GetComponentsInChildren<LocalizeStringEvent>(true))
        {
            var reference = component.StringReference;
            string key = reference.TableEntryReference.Key;
            if (!string.IsNullOrWhiteSpace(key) && map.TryGetValue(key, out string next))
            { reference.TableEntryReference = next; EditorUtility.SetDirty(component); count++; }
        }
        return count;
    }

    private static bool MayContainMappedKeys(string assetPath, IDictionary<string, string> map)
    {
        if (map.Count == 0 || !File.Exists(assetPath)) return false;
        // Unity text-serialized YAML stores localizationKey as text; skip unrelated scenes.
        // Binary files cannot be examined safely and are left for manual review.
        string content;
        try { content = File.ReadAllText(assetPath); }
        catch { return false; }
        return map.Keys.Any(key => content.IndexOf(key, StringComparison.Ordinal) >= 0);
    }

    private static string Propose(string key)
    {
        string[] tokens = key.Split('.');
        string leaf = tokens.Length >= 2 ? tokens[tokens.Length - 2] : "text";
        string section = tokens.FirstOrDefault(s => s.EndsWith("panel", StringComparison.OrdinalIgnoreCase) || s.EndsWith("ui", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(section)) section = "common";
        section = section.Replace("explorationresultpanel", "exploration_result");
        leaf = leaf.Replace("explorationreport", "").Replace("label", "").Replace("text", "");
        leaf = System.Text.RegularExpressions.Regex.Replace(leaf, @"([a-z])([A-Z])", "$1_$2").ToLowerInvariant();
        return "ui." + section + "." + (string.IsNullOrWhiteSpace(leaf) ? "text" : leaf);
    }
    private static string Safe(string value) => (value ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
#endif
