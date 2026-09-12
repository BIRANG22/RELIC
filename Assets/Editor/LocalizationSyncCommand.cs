using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

public static class LocalizationSyncCommand
{
    private static readonly string[] LocaleCodes = { "ko", "en", "zh-Hans", "ja", "es" };

    [MenuItem("Tools/Localization/Sync Record Localization")]
    public static void SyncRecordLocalizationFromMenu()
    {
        LocalizationSyncResult result = SyncRecordLocalization();
        Debug.Log(result.ToLog());
    }

    public static LocalizationSyncResult SyncRecordLocalization()
    {
        return Sync("Record Localization", RecordEntries(), new[]
        {
            LocalizationKeys.Record.UnlockCharacterLevel, LocalizationKeys.Record.UnlockBlueDustium,
            LocalizationKeys.Record.Method, LocalizationKeys.Record.Consumption, LocalizationKeys.Record.Effect,
            LocalizationKeys.Record.UseTarget, LocalizationKeys.Record.UseCount,
        });
    }

    [MenuItem("Tools/Localization/Sync Tutorial Localization")]
    public static void SyncTutorialLocalizationFromMenu()
    {
        LocalizationSyncResult result = SyncTutorialLocalization();
        Debug.Log(result.ToLog());
    }

    public static LocalizationSyncResult SyncTutorialLocalization() =>
        Sync("Tutorial Localization", TutorialEntries(), Array.Empty<string>());

    [MenuItem("Tools/Localization/Sync Intro Story Localization")]
    public static void SyncIntroStoryLocalizationFromMenu()
    {
        LocalizationSyncResult result = SyncIntroStoryLocalization();
        Debug.Log(result.ToLog());
    }

    public static LocalizationSyncResult SyncIntroStoryLocalization() =>
        Sync("Intro Story Localization", IntroStoryEntries(), Array.Empty<string>());

    [MenuItem("Tools/Localization/Sync Skill Setting Localization")]
    public static void SyncSkillSettingLocalizationFromMenu()
    {
        LocalizationSyncResult result = SyncSkillSettingLocalization();
        Debug.Log(result.ToLog());
    }

    public static LocalizationSyncResult SyncSkillSettingLocalization() =>
        Sync("Skill Setting Localization", SkillSettingEntries(), Array.Empty<string>());

    [MenuItem("Tools/Localization/Sync Character Setting Localization")]
    public static void SyncCharacterSettingLocalizationFromMenu()
    {
        LocalizationSyncResult result = SyncCharacterSettingLocalization();
        if (result.Success)
        {
            ApplyCharacterSettingEnglishTranslations();
            StaticLocalizationMigration.MigrateCharacterSettingInfoText();
        }
        Debug.Log(result.ToLog());
    }

    public static LocalizationSyncResult SyncCharacterSettingLocalization() =>
        Sync("Character Setting Localization", CharacterSettingEntries(), Array.Empty<string>());

    [MenuItem("Tools/Localization/Validate RelicShop Rarity Localization")]
    public static void ValidateRelicShopRarityLocalizationFromMenu()
    {
        LocalizationSyncResult result = Sync("RelicShop Rarity Localization", RelicRarityEntries(), Array.Empty<string>());
        Debug.Log(result.ToLog());
    }

    [MenuItem("Tools/Localization/Sync Rune Installation Localization")]
    public static void SyncRuneInstallationLocalizationFromMenu()
    {
        LocalizationSyncResult result = Sync("Rune Installation Localization", RuneInstallationEntries(), Array.Empty<string>());
        if (result.Success)
            StaticLocalizationMigration.MigrateRuneInstallationTexts();
        Debug.Log(result.ToLog());
    }

    private static LocalizationSyncResult Sync(string label, IReadOnlyList<LocalizationWorkbookEntry> entries, IReadOnlyList<string> formatKeys)
    {
        var result = new LocalizationSyncResult { Label = label, Stage = "START" };
        if (File.Exists("Assets/ExcelSource/~$Localization.xlsx"))
        {
            result.Error = "Localization.xlsx가 Excel에서 열려 있습니다. Excel을 닫은 뒤 다시 실행하세요.";
            return result;
        }

        try
        {
            result.ReferencedKeys = entries.Select(entry => entry.Key).Distinct(StringComparer.Ordinal).OrderBy(key => key).ToArray();
            LogStage(label, result, "START");
            var workbookRows = LocalizationXlsxReader.ReadSheet(LocalizationExcelImporter.WorkbookPath, LocalizationExcelImporter.WorksheetName);
            int keyColumn = workbookRows[0].ToList().FindIndex(value => value == "Key");
            var workbookKeys = workbookRows.Skip(1).Where(row => keyColumn >= 0 && keyColumn < row.Count).Select(row => row[keyColumn]).ToHashSet(StringComparer.Ordinal);
            result.ExistingWorkbookKeys = result.ReferencedKeys.Count(workbookKeys.Contains);
            result.Stage = "Workbook Writer";
            LogStage(label, result, result.Stage);
            result.AddedWorkbookKeys = LocalizationWorkbookWriter.MergeNewEntries(LocalizationExcelImporter.WorkbookPath, entries);

            result.Stage = "Workbook Save";
            LogStage(label, result, result.Stage);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            result.Stage = "Importer";
            LogStage(label, result, result.Stage);
            LocalizationExcelImporter.Import();
            result.Stage = "String Table Validation";
            LogStage(label, result, result.Stage);
            EnsureLocaleEntries(result.ReferencedKeys);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateTables(result, formatKeys);
            result.Success = result.WorkbookMissing.Count == 0 && result.LocaleMissingKeys.Values.All(values => values.Count == 0) && result.FormatErrors.Count == 0;
            result.Stage = result.Success ? "SUCCESS" : "FAILED";
            LogStage(label, result, result.Stage);
        }
        catch (Exception exception)
        {
            result.Error = exception.ToString();
            result.Stage = "FAILED";
            Debug.LogError($"[{label} Sync] FAILED\nStage: {result.Stage}\nException:\n{exception}");
        }
        return result;
    }

    private static void LogStage(string label, LocalizationSyncResult result, string stage)
    {
        Debug.Log($"[{label} Sync] {stage}\nReferenced Keys: {result.ReferencedKeys.Count}");
    }

    /// <summary>CSV import omits completely empty locale cells; tables still need stable entries for validation and future translation.</summary>
    private static void EnsureLocaleEntries(IEnumerable<string> keys)
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationExcelImporter.TableCollectionName);
        if (collection == null)
            throw new InvalidOperationException($"String Table Collection '{LocalizationExcelImporter.TableCollectionName}' was not found.");

        foreach (StringTable table in collection.StringTables)
        foreach (string key in keys)
            if (table.GetEntry(key) == null)
                table.AddEntry(key, string.Empty);
    }

    private static void ApplyCharacterSettingEnglishTranslations()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationExcelImporter.TableCollectionName);
        StringTable table = collection?.StringTables.FirstOrDefault(candidate => candidate.LocaleIdentifier.Code == "en");
        if (table == null)
            throw new InvalidOperationException("English String Table was not found.");

        var translations = new Dictionary<string, string>
        {
            [LocalizationKeys.CharacterSetting.PreviewInfo] = "Fragment/Skill information is displayed.",
            [LocalizationKeys.CharacterSetting.SkillInfoTitle] = "Skill Info",
            [LocalizationKeys.CharacterSetting.SkillInfoEmpty] = "Skill information is displayed.",
            [LocalizationKeys.CharacterSetting.RuneInfoTitle] = "Fragment Info",
            [LocalizationKeys.CharacterSetting.RuneInfoEmpty] = "Fragment information is displayed.",
        };
        foreach ((string key, string value) in translations)
            table.GetEntry(key).Value = value;

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
    }

    private static void ValidateTables(LocalizationSyncResult result, IReadOnlyList<string> formatKeys)
    {
        var rows = LocalizationXlsxReader.ReadSheet(LocalizationExcelImporter.WorkbookPath, LocalizationExcelImporter.WorksheetName);
        int keyColumn = rows[0].ToList().FindIndex(value => value == "Key");
        var workbook = rows.Skip(1).Where(row => keyColumn >= 0 && keyColumn < row.Count).ToDictionary(row => row[keyColumn], row => row, StringComparer.Ordinal);
        result.WorkbookMissing = result.ReferencedKeys.Where(key => !workbook.ContainsKey(key)).ToList();
        var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationExcelImporter.TableCollectionName);
        foreach (string localeCode in LocaleCodes)
        {
            StringTable table = collection?.StringTables.FirstOrDefault(candidate => candidate.LocaleIdentifier.Code == localeCode);
            result.LocaleMissingKeys[localeCode] = result.ReferencedKeys.Where(key => table?.GetEntry(key) == null).ToList();
            result.LocaleMissingTranslations[localeCode] = table == null ? result.ReferencedKeys.ToList() : result.ReferencedKeys.Where(key => string.IsNullOrWhiteSpace(table.GetEntry(key)?.Value)).ToList();
        }
        foreach (string key in formatKeys)
            if (workbook.TryGetValue(key, out var row) && !ContainsPlaceholder(row, rows[0], "{0}")) result.FormatErrors.Add(key);
    }

    private static bool ContainsPlaceholder(IReadOnlyList<string> row, IReadOnlyList<string> headers, string placeholder) => row.Skip(1).Any(value => !string.IsNullOrWhiteSpace(value) && value.Contains(placeholder));

    private static IReadOnlyList<LocalizationWorkbookEntry> RecordEntries() => new[]
    {
        E(LocalizationKeys.Common.None,"없음"), E(LocalizationKeys.Resource.Hp,"생명력"), E(LocalizationKeys.Resource.Mana,"마나"), E(LocalizationKeys.Resource.Karma,"카르마"), E(LocalizationKeys.Resource.Move,"이동"), E(LocalizationKeys.Target.Self,"자신"), E(LocalizationKeys.Target.Grid,"그리드"),
        E(LocalizationKeys.Range.Direction,"시전자 위치"), E(LocalizationKeys.Range.Selection,"그리드 선택"), E(LocalizationKeys.Range.Passive,"카르마 최대 시 지속"),
        E(LocalizationKeys.Record.UnlockCharacterLevel,"획득 조건 : 캐릭터 Lv.{0} 도달"), E(LocalizationKeys.Record.UnlockBlueDustium,"획득 조건 : 블루 더스티움 {0}"), E(LocalizationKeys.Record.Method,"방식 : {0}"), E(LocalizationKeys.Record.Consumption,"소모 : {0}"), E(LocalizationKeys.Record.Effect,"효과 : {0}"), E(LocalizationKeys.Record.UseTarget,"사용 대상 : {0}"), E(LocalizationKeys.Record.UseCount,"사용 가능 횟수 : {0}"), E(LocalizationKeys.Record.NoConsumption,"소모 없음"), E(LocalizationKeys.Record.UnknownDescription,"아직 기록되지 않았습니다"), E(LocalizationKeys.Record.UnknownMethod,"방식 : ???"), E(LocalizationKeys.Record.UnknownConsumption,"소모 : ???"), E(LocalizationKeys.Record.UnknownEffect,"효과 : ???"), E(LocalizationKeys.Record.UnknownUseTarget,"사용 대상 : ???"), E(LocalizationKeys.Record.UnknownUseCount,"사용 가능 횟수 : ???"),
        E(LocalizationKeys.MemoryRarity.Exclusive,"고유 기억"), E(LocalizationKeys.MemoryRarity.Common,"일반 기억"), E(LocalizationKeys.MemoryRarity.Rare,"레어 기억"), E(LocalizationKeys.MemoryRarity.Epic,"에픽 기억"), E(LocalizationKeys.MemoryRarity.Unique,"유니크 기억"), E(LocalizationKeys.FragmentRarity.Exclusive,"고유 파편"), E(LocalizationKeys.FragmentRarity.Common,"각인 파편"), E(LocalizationKeys.FragmentRarity.Rare,"일반 파편"), E(LocalizationKeys.FragmentRarity.Unique,"축복 파편"), E(LocalizationKeys.ItemRarity.Common,"일반 재료"), E(LocalizationKeys.ItemRarity.Rare,"레어 재료"), E(LocalizationKeys.ItemRarity.Epic,"에픽 재료"), E(LocalizationKeys.CompoundRarity.Common,"일반 연성제"), E(LocalizationKeys.CompoundRarity.Rare,"레어 연성제"), E(LocalizationKeys.CompoundRarity.Epic,"에픽 연성제"), E(LocalizationKeys.RelicRarity.Common,"일반 유물"), E(LocalizationKeys.RelicRarity.Rare,"레어 유물"), E(LocalizationKeys.RelicRarity.Epic,"에픽 유물"), E(LocalizationKeys.RelicRarity.Unique,"유니크 유물"),
    };
    private static IReadOnlyList<LocalizationWorkbookEntry> TutorialEntries() => new[]
    {
        E(LocalizationKeys.Tutorial.SpeakerElric, "\uC5D8\uB9AD"),
        E(LocalizationKeys.Tutorial.Intro01, "\uB4DC\uB514\uC5B4 \uBAA8\uB450 \uB3C4\uCC29\uD558\uC168\uAD70\uC694. \uAE30\uB2E4\uB9AC\uACE0 \uC788\uC5C8\uC2B5\uB2C8\uB2E4."),
        E(LocalizationKeys.Tutorial.Intro02, "\uD604\uC7AC \uCE68\uC2DD \uAD6C\uC5ED\uC5D0 \uB300\uD55C \uC870\uC0AC\uAC00 \uD544\uC694\uD55C \uC0C1\uD669\uC785\uB2C8\uB2E4."),
        E(LocalizationKeys.Tutorial.Intro03, "\uC6B0\uC120 \uAC01 \uC9C0\uC5ED\uC744 \uD0D0\uC0AC\uD558\uBA70 \uC6D0\uC778\uC744 \uD30C\uC545\uD560 \uB2E8\uC11C\uB97C \uBAA8\uC544\uC57C \uD569\uB2C8\uB2E4."),
        E(LocalizationKeys.Tutorial.Intro04, "\uD0D0\uC0AC\uC5D0 \uC55E\uC11C \uC5EC\uB7EC\uBD84\uC5D0\uAC8C \uB3C4\uC6C0\uC774 \uB420 \uBB3C\uAC74\uC744 \uC900\uBE44\uD574 \uB450\uC5C8\uC2B5\uB2C8\uB2E4."),
        E(LocalizationKeys.Tutorial.Intro05, "\uC774 \uD30C\uD3B8\uB4E4\uC740 \uAC01\uC790\uC758 \uB2A5\uB825\uC744 \uAC15\uD654\uD558\uB294 \uB370 \uC0AC\uC6A9\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4."),
        E(LocalizationKeys.Tutorial.Intro06, "\uC798 \uD65C\uC6A9\uD55C\uB2E4\uBA74 \uC55E\uC73C\uB85C\uC758 \uD0D0\uC0AC\uC5D0 \uD070 \uB3C4\uC6C0\uC774 \uB420 \uAC81\uB2C8\uB2E4."),
        E(LocalizationKeys.Tutorial.Intro07, "\uC900\uBE44\uB97C \uB9C8\uCE5C \uB4A4, \uB2E4\uC2DC \uC800\uC5D0\uAC8C \uB9D0\uC744 \uAC78\uC5B4 \uC8FC\uC138\uC694."),
        E(LocalizationKeys.Tutorial.FirstExpedition01, "\uC900\uBE44\uB97C \uB9C8\uCE58\uC168\uAD70\uC694."),
        E(LocalizationKeys.Tutorial.FirstExpedition02, "\uCCAB \uD0D0\uC0AC\uC9C0\uB294 \uB85C\uB370\uB978 \uD3D0\uD5C8\uC785\uB2C8\uB2E4."),
        E(LocalizationKeys.Tutorial.FirstExpedition03, "\uADF8\uACF3\uC5D0\uC11C \uC5F0\uAD6C\uC5D0 \uD544\uC694\uD55C \uC7AC\uB8CC\uB97C \uD655\uBCF4\uD574 \uC640 \uC8FC\uC138\uC694."),
    };
    private static IReadOnlyList<LocalizationWorkbookEntry> IntroStoryEntries() => new[]
    {
        E(LocalizationKeys.IntroStory.Line01, "\uB354\uC2A4\uD2F0\uC6C0\uC774 \uACFC\uB3C4\uD558\uAC8C \uC751\uCD95\uB418\uBA70 \uCE68\uC2DD \uAD6C\uC5ED\uC774 \uC0DD\uACA8\uB0AC\uB2E4."),
        E(LocalizationKeys.IntroStory.Line02, "\uCE68\uC2DD\uB41C \uB545\uC5D0\uB294 \uC815\uCCB4\uB97C \uC54C \uC218 \uC5C6\uB294 \uBCC0\uC774\uCCB4\uAC00 \uB098\uD0C0\uB0AC\uACE0,"),
        E(LocalizationKeys.IntroStory.Line03, "\uADF8\uACF3\uC740 \uC810\uCC28 \uD568\uBD80\uB85C \uBC1C\uC744 \uB4E4\uC774\uAE30 \uC5B4\uB824\uC6B4 \uC704\uD5D8\uD55C \uC7A5\uC18C\uAC00 \uB418\uC5C8\uB2E4."),
        E(LocalizationKeys.IntroStory.Line04, "\uD559\uD68C\uB294 \uCE68\uC2DD\uC758 \uC6D0\uC778\uC744 \uD30C\uC545\uD558\uACE0 \uD604\uC0C1\uC744 \uC5F0\uAD6C\uD558\uACE0\uC790"),
        E(LocalizationKeys.IntroStory.Line05, "\uCE68\uC2DD \uAD6C\uC5ED\uC5D0 \uB300\uD55C \uC870\uC0AC\uB97C \uACB0\uC815\uD588\uB2E4."),
        E(LocalizationKeys.IntroStory.Line06, "\uC5BC\uB9C8 \uB4A4, \uD0D0\uC0AC \uC778\uC6D0\uC744 \uBAA8\uC9D1\uD558\uB294 \uC758\uB8B0\uC11C\uAC00 \uB0B4\uAC78\uB838\uB2E4."),
        E(LocalizationKeys.IntroStory.Line07, "\uC800\uB9C8\uB2E4 \uB2E4\uB978 \uC774\uC720\uB97C \uAC00\uC9C4 \uC774\uB4E4\uC774 \uADF8 \uC758\uB8B0\uC11C\uB97C \uC9D1\uC5B4 \uB4E4\uC5C8\uB2E4."),
        E(LocalizationKeys.IntroStory.Line08, "\uCE68\uC2DD \uAD6C\uC5ED\uC758 \uACBD\uACC4\uC5D0\uB294 \uD0D0\uC0AC \uAC70\uC810\uC774 \uC138\uC6CC\uC84C\uACE0,"),
        E(LocalizationKeys.IntroStory.Line09, "\uD558\uB098\uB458 \uADF8\uACF3\uC73C\uB85C \uBAA8\uC5EC\uB4E4\uC5C8\uB2E4."),
    };
    private static IReadOnlyList<LocalizationWorkbookEntry> SkillSettingEntries() => new[]
    {
        E(LocalizationKeys.SkillInfo.Cost, "소모 : {0}"), E(LocalizationKeys.SkillInfo.Type, "방식 : {0}"), E(LocalizationKeys.SkillInfo.Effect, "효과 : {0}"),
        E(LocalizationKeys.SkillInfo.RarityMove, "이동"), E(LocalizationKeys.SkillInfo.RarityMemory, "기억"), E(LocalizationKeys.SkillInfo.RarityCommonMemory, "일반 기억"), E(LocalizationKeys.SkillInfo.RarityRareMemory, "레어 기억"), E(LocalizationKeys.SkillInfo.RarityEpicMemory, "에픽 기억"), E(LocalizationKeys.SkillInfo.RarityUniqueMemory, "유니크 기억"),
        E(LocalizationKeys.SkillInfo.RarityInstinctMemory, "본능 기억"), E(LocalizationKeys.SkillInfo.RarityManifestationMemory, "발현 기억"), E(LocalizationKeys.SkillInfo.RarityImplementationMemory, "구현 기억"),
        E(LocalizationKeys.SkillInfo.RangeDirection, "시전자 위치"), E(LocalizationKeys.SkillInfo.RangeSelection, "그리드 선택"), E(LocalizationKeys.SkillInfo.RangePassive, "{0} 최대 시 지속"), E(LocalizationKeys.SkillInfo.PassiveActivation, "{0} {1} 유지 시 지속"),
    };
    private static IReadOnlyList<LocalizationWorkbookEntry> CharacterSettingEntries() => new[]
    {
        E(LocalizationKeys.CharacterSetting.PreviewInfo, "파편/스킬의 정보가 표시된다."),
        E(LocalizationKeys.CharacterSetting.SkillInfoTitle, "스킬정보"),
        E(LocalizationKeys.CharacterSetting.SkillInfoEmpty, "스킬의 정보가 표시된다."),
        E(LocalizationKeys.CharacterSetting.RuneInfoTitle, "파편정보"),
        E(LocalizationKeys.CharacterSetting.RuneInfoEmpty, "파편의 정보가 표시된다."),
        E(LocalizationKeys.CharacterSetting.CharacterIntro, "카르마 획득 설명 & 스텟설명"),
        E(LocalizationKeys.CharacterSetting.KarmaAcquisitionTitle, "카르마 획득 조건"),
        E(LocalizationKeys.CharacterSetting.KarmaDescription, "각자의 전투 방식에 따라 축적되며, 기억을 발현하는 힘이 된다."),
        E(LocalizationKeys.CharacterSetting.StatMaximumValue, "최대보유량 {0}"),
    };
    private static IReadOnlyList<LocalizationWorkbookEntry> RuneInstallationEntries() => new[]
    {
        E("ui.rune.installation", "장착 중"),
        E(LocalizationKeys.Rune.InfoTitle, "파편 정보"),
        E(LocalizationKeys.Rune.InfoEmptyDescription, "파편을 선택하면 정보가 표시된다."),
        E(LocalizationKeys.Rune.NoEffectDescription, "등록된 효과 설명이 없습니다."),
    };
    private static IReadOnlyList<LocalizationWorkbookEntry> RelicRarityEntries() => new[]
    {
        E(LocalizationKeys.RelicRarity.Common, "일반 유물"), E(LocalizationKeys.RelicRarity.Rare, "레어 유물"),
        E(LocalizationKeys.RelicRarity.Epic, "에픽 유물"), E(LocalizationKeys.RelicRarity.Unique, "유니크 유물"),
    };
    private static LocalizationWorkbookEntry E(string key, string korean) => new(key, korean);
}

public sealed class LocalizationSyncResult
{
    public string Label; public string Stage; public IReadOnlyList<string> ReferencedKeys = Array.Empty<string>(); public int ExistingWorkbookKeys; public int AddedWorkbookKeys; public List<string> WorkbookMissing = new(); public Dictionary<string,List<string>> LocaleMissingKeys = new(); public Dictionary<string,List<string>> LocaleMissingTranslations = new(); public List<string> FormatErrors = new(); public string Error; public bool Success;
    public string ToLog() => string.IsNullOrWhiteSpace(Error) ? $"[{Label} Sync]\nStage: {Stage}\nReferenced Keys: {ReferencedKeys.Count}\nExisting Workbook Keys: {ExistingWorkbookKeys}\nAdded Workbook Keys: {AddedWorkbookKeys}\nWorkbook Missing: {WorkbookMissing.Count}\n" + string.Join("\n", LocaleMissingKeys.Select(pair => $"{pair.Key} MissingKey: {pair.Value.Count}, MissingTranslation: {LocaleMissingTranslations[pair.Key].Count}")) + $"\nFormat Errors: {FormatErrors.Count}\nResult: {(Success ? "SUCCESS" : "FAILED")}" : $"[{Label} Sync] FAILED\nStage: {Stage}\nException:\n{Error}";
}
