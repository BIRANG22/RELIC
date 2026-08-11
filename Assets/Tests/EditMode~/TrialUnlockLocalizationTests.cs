using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class TrialUnlockLocalizationTests
{
    private const string WorkbookPath = "Assets/ExcelSource/Localization.xlsx";
    private const string SharedDataPath = "Assets/Language/Text Shared Data.asset";

    private static readonly TrialUnlockLocalizationCase[] UnlockCases =
    {
        new(
            0,
            "lobby.trial.unlock.stage3_clear",
            "battle.all_enemy_health_up",
            "해금 : 3구역 1회 클리어",
            "Unlock: Clear Area 3 once."),
        new(
            1,
            "lobby.trial.unlock.nocturne_kill",
            "battle.all_enemy_attack_up",
            "해금 : 녹턴 3회 처치",
            "Unlock: Defeat Nocturne 3 times."),
        new(
            2,
            "lobby.trial.unlock.two_trials_clear",
            "battle.strong_enemy_frequency",
            "해금 : 2개의 시련을 적용 후 클리어",
            "Unlock: Clear with 2 Trials applied."),
    };

    private readonly List<GameObject> createdObjects = new();
    private readonly Dictionary<string, int?> savedPlayerPrefs = new();

    [SetUp]
    public void SetUp()
    {
        SavePlayerPref("TrialUnlock.Stage3ClearCount");
        SavePlayerPref("TrialUnlock.NocturnKillCount");
        SavePlayerPref("TrialUnlock.TwoTrialStage3ClearCount");

        TrialSelectionState.Clear();
        TrialUnlockProgress.ResetProgress();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
        TrialSelectionState.Clear();
        RestorePlayerPrefs();
    }

    [Test]
    public void LockedTrialEffects_SwitchLocalizersToUnlockRequirementKeys()
    {
        ErosionSelectCarousel carousel = CreateCarousel();

        InvokePrivateLifecycle(carousel, "OnEnable");

        LocalizeStringEvent[] localizers = carousel.GetComponentsInChildren<LocalizeStringEvent>(true);
        foreach (TrialUnlockLocalizationCase unlockCase in UnlockCases)
        {
            LocalizeStringEvent localizer = localizers[unlockCase.TrialIndex];
            Assert.That(
                localizer.StringReference.TableEntryReference.Key,
                Is.EqualTo(unlockCase.UnlockKey),
                unlockCase.UnlockKey);
        }
    }

    [Test]
    public void UnlockedTrialEffects_RestoreOriginalEffectKeys()
    {
        ErosionSelectCarousel carousel = CreateCarousel();

        InvokePrivateLifecycle(carousel, "OnEnable");
        UnlockAllTrials();
        InvokePrivateLifecycle(carousel, "RefreshVisuals");

        LocalizeStringEvent[] localizers = carousel.GetComponentsInChildren<LocalizeStringEvent>(true);
        foreach (TrialUnlockLocalizationCase unlockCase in UnlockCases)
        {
            LocalizeStringEvent localizer = localizers[unlockCase.TrialIndex];
            Assert.That(
                localizer.StringReference.TableEntryReference.Key,
                Is.EqualTo(unlockCase.UnlockedEffectKey),
                unlockCase.UnlockedEffectKey);
        }
    }

    [Test]
    public void TrialUnlockProgress_ReturnsStableLocalizationKeys()
    {
        foreach (TrialUnlockLocalizationCase unlockCase in UnlockCases)
        {
            Assert.That(
                TrialUnlockProgress.GetUnlockRequirementKey(unlockCase.TrialIndex),
                Is.EqualTo(unlockCase.UnlockKey));
        }
    }

    [Test]
    public void LocalizationWorkbook_ContainsTrialUnlockRequirementRows()
    {
        IReadOnlyList<IReadOnlyList<string>> rows =
            LocalizationXlsxReader.ReadSheet(WorkbookPath, "Text");
        IReadOnlyList<string> headers = rows[0];
        int keyIndex = FindHeader(headers, "Key");
        int koreanIndex = FindHeader(headers, "Korean(ko)");
        int englishIndex = FindHeader(headers, "English(en)");

        Dictionary<string, IReadOnlyList<string>> byKey = rows
            .Skip(1)
            .Where(row => !string.IsNullOrWhiteSpace(GetValue(row, keyIndex)))
            .ToDictionary(row => GetValue(row, keyIndex), row => row);

        foreach (TrialUnlockLocalizationCase unlockCase in UnlockCases)
        {
            Assert.That(byKey.ContainsKey(unlockCase.UnlockKey), Is.True, unlockCase.UnlockKey);
            IReadOnlyList<string> row = byKey[unlockCase.UnlockKey];
            Assert.That(GetValue(row, koreanIndex), Is.EqualTo(unlockCase.Korean));
            Assert.That(GetValue(row, englishIndex), Is.EqualTo(unlockCase.English));
        }
    }

    [Test]
    public void TextStringTables_ContainTrialUnlockRequirementEntries()
    {
        Dictionary<string, string> sharedIds = ReadSharedIds();
        string[] localeTablePaths =
        {
            "Assets/Language/Text_ko.asset",
            "Assets/Language/Text_en.asset",
            "Assets/Language/Text_zh-Hans.asset",
            "Assets/Language/Text_ja.asset",
            "Assets/Language/Text_es.asset",
        };

        foreach (TrialUnlockLocalizationCase unlockCase in UnlockCases)
        {
            Assert.That(sharedIds.ContainsKey(unlockCase.UnlockKey), Is.True, unlockCase.UnlockKey);
            string id = sharedIds[unlockCase.UnlockKey];

            foreach (string localeTablePath in localeTablePaths)
            {
                string tableYaml = File.ReadAllText(localeTablePath);
                Assert.That(
                    Regex.IsMatch(tableYaml, @"m_Id: " + Regex.Escape(id) + @"\s+m_Localized: "),
                    Is.True,
                    $"{unlockCase.UnlockKey} missing from {localeTablePath}");
            }
        }
    }

    private ErosionSelectCarousel CreateCarousel()
    {
        GameObject root = new("ErosionSelectPanel", typeof(RectTransform), typeof(ErosionSelectCarousel));
        createdObjects.Add(root);

        foreach (TrialUnlockLocalizationCase unlockCase in UnlockCases)
            CreateTrialItem(root.transform, unlockCase);

        return root.GetComponent<ErosionSelectCarousel>();
    }

    private void CreateTrialItem(Transform parent, TrialUnlockLocalizationCase unlockCase)
    {
        GameObject target = new($"Erosion_{unlockCase.TrialIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
        target.transform.SetParent(parent, false);

        GameObject selected = new("Selected", typeof(RectTransform));
        selected.transform.SetParent(target.transform, false);

        GameObject locked = new("LOCK", typeof(RectTransform));
        locked.transform.SetParent(target.transform, false);

        GameObject name = new("Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        name.transform.SetParent(target.transform, false);

        GameObject effect = new("Effect", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LocalizeStringEvent));
        effect.transform.SetParent(target.transform, false);

        TextMeshProUGUI effectText = effect.GetComponent<TextMeshProUGUI>();
        effectText.text = $"Unlocked effect {unlockCase.TrialIndex}";

        LocalizeStringEvent localizer = effect.GetComponent<LocalizeStringEvent>();
        localizer.StringReference.TableReference = GameLocalization.TableName;
        localizer.StringReference.TableEntryReference = unlockCase.UnlockedEffectKey;
    }

    private void SavePlayerPref(string key)
    {
        savedPlayerPrefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
    }

    private void RestorePlayerPrefs()
    {
        foreach (KeyValuePair<string, int?> entry in savedPlayerPrefs)
        {
            if (entry.Value.HasValue)
                PlayerPrefs.SetInt(entry.Key, entry.Value.Value);
            else
                PlayerPrefs.DeleteKey(entry.Key);
        }

        PlayerPrefs.Save();
        savedPlayerPrefs.Clear();
    }

    private static void UnlockAllTrials()
    {
        PlayerPrefs.SetInt("TrialUnlock.Stage3ClearCount", 1);
        PlayerPrefs.SetInt("TrialUnlock.NocturnKillCount", 3);
        PlayerPrefs.SetInt("TrialUnlock.TwoTrialStage3ClearCount", 1);
        PlayerPrefs.Save();
    }

    private static void InvokePrivateLifecycle(ErosionSelectCarousel carousel, string methodName)
    {
        MethodInfo method = typeof(ErosionSelectCarousel).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        method.Invoke(carousel, null);
    }

    private static Dictionary<string, string> ReadSharedIds()
    {
        string sharedData = File.ReadAllText(SharedDataPath);
        var sharedIds = new Dictionary<string, string>();

        foreach (Match match in Regex.Matches(
                     sharedData,
                     @"m_Id: (?<id>\d+)\s+m_Key: (?<key>[^\r\n]+)"))
        {
            sharedIds[match.Groups["key"].Value.Trim()] = match.Groups["id"].Value;
        }

        return sharedIds;
    }

    private static int FindHeader(IReadOnlyList<string> headers, string name)
    {
        for (int index = 0; index < headers.Count; index++)
        {
            if (headers[index] == name)
                return index;
        }

        Assert.Fail($"Missing header: {name}");
        return -1;
    }

    private static string GetValue(IReadOnlyList<string> row, int index) =>
        index >= 0 && index < row.Count ? row[index] : string.Empty;

    private readonly struct TrialUnlockLocalizationCase
    {
        public TrialUnlockLocalizationCase(
            int trialIndex,
            string unlockKey,
            string unlockedEffectKey,
            string korean,
            string english)
        {
            TrialIndex = trialIndex;
            UnlockKey = unlockKey;
            UnlockedEffectKey = unlockedEffectKey;
            Korean = korean;
            English = english;
        }

        public int TrialIndex { get; }
        public string UnlockKey { get; }
        public string UnlockedEffectKey { get; }
        public string Korean { get; }
        public string English { get; }
    }
}
