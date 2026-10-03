using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

public sealed class LocalizationMissingTranslationPolicyTests
{
    [Test]
    public void RefreshGate_AcceptsOnlyTheLatestAsyncRequest()
    {
        var gate = new LocalizationRefreshGate();

        int koreanRequest = gate.Begin();
        int englishRequest = gate.Begin();

        Assert.That(gate.IsCurrent(koreanRequest), Is.False,
            "늦게 완료된 이전 언어 요청이 최신 표시를 덮으면 안 됩니다.");
        Assert.That(gate.IsCurrent(englishRequest), Is.True);

        gate.Invalidate();

        Assert.That(gate.IsCurrent(englishRequest), Is.False,
            "비활성화된 컴포넌트의 대기 중 요청은 적용되면 안 됩니다.");
    }

    [Test]
    public void LocalizedTmpText_DisablesLegacyWriterOnTheSameText()
    {
        var gameObject = new GameObject("Localized Text");
        gameObject.SetActive(false);

        try
        {
            gameObject.AddComponent<TextMeshProUGUI>();
            LocalizeStringEvent legacyWriter = gameObject.AddComponent<LocalizeStringEvent>();
            LocalizedTMPText runtimeWriter = gameObject.AddComponent<LocalizedTMPText>();

            Assert.That(legacyWriter.enabled, Is.True,
                "테스트 전제상 구형 표시 컴포넌트는 활성 상태여야 합니다.");

            gameObject.SetActive(true);

            Assert.That(runtimeWriter.enabled, Is.True);
            Assert.That(legacyWriter.enabled, Is.False,
                "동일 TMP에 두 표시 작성자가 남으면 빈 영어 셀이 한국어로 다시 덮일 수 있습니다.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void RepairTextBinding_WhenLegacyWriterIsAlreadyValid_ReportsAssetChanged()
    {
        var gameObject = new GameObject("Localized Text");
        gameObject.SetActive(false);

        try
        {
            TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "검사 문구";
            LocalizeStringEvent legacyWriter = gameObject.AddComponent<LocalizeStringEvent>();
            legacyWriter.StringReference = new LocalizedString(
                LocalizationExcelImporter.TableCollectionName,
                "ui.test.label");
            UnityAction<string> callback = (UnityAction<string>)Delegate.CreateDelegate(
                typeof(UnityAction<string>),
                text,
                text.GetType().GetProperty(nameof(TMP_Text.text)).GetSetMethod());
            UnityEventTools.AddPersistentListener(legacyWriter.OnUpdateString, callback);

            LocalizedTMPText runtimeWriter = gameObject.AddComponent<LocalizedTMPText>();
            runtimeWriter.Configure("ui.test.label", "검사 문구", true);

            bool changed = StaticLocalizationMigration.RepairTextBinding(text, "ui.test.label");

            Assert.That(changed, Is.True,
                "구형 작성자를 제거했다면 호출자가 프리팹을 저장하도록 변경 사실을 반환해야 합니다.");
            Assert.That(gameObject.GetComponent<LocalizeStringEvent>(), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void RuntimeLookup_DisablesCrossLocaleFallback()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Core/Localization/GameLocalization.cs");

        int occurrences = source.Split(
            new[] { "FallbackBehavior.DontUseFallback" },
            StringSplitOptions.None).Length - 1;

        Assert.That(occurrences, Is.GreaterThanOrEqualTo(2),
            "선택 로케일의 빈 셀이 한국어 로케일로 fallback되면 안 됩니다.");
        Assert.That(GameLocalization.ResolveMissingTranslation("en", "한국어 원문"),
            Is.EqualTo("Untranslated"));
    }

    [Test]
    public void EditingLock_BypassIsLimitedToNonPlayingEditorState()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Core/Localization/GameLocalization.cs");

        Assert.That(source, Does.Contain("!Application.isPlaying && !LocalizationEditingLockState.IsEnabledForEditor"),
            "편집 잠금이 꺼져 있어도 Play Mode에서는 선택 언어를 조회해야 합니다.");
    }

    [Test]
    public void EditingLockOff_DoesNotDisableRuntimeLocalizationComponent()
    {
        Assert.That(
            LocalizationEditingLockPolicy.ShouldEnableRuntimeLocalizer(editingLockEnabled: false),
            Is.True,
            "편집 중 한국어 원문을 표시하더라도 Play Mode에서 실행될 로컬라이저 컴포넌트는 꺼 저장하면 안 됩니다.");
    }

    [TestCase(false, null)]
    [TestCase(true, "")]
    [TestCase(true, "   ")]
    public void BlankSelectedLocaleEntry_IsNeverAcceptedAsLocalizedText(bool entryExists, string rawValue)
    {
        Assert.That(
            GameLocalization.ShouldUseSelectedLocaleEntry(entryExists, rawValue),
            Is.False,
            "선택 언어 표에 값이 없으면 다른 로케일의 문자열을 표시 대상으로 인정하면 안 됩니다.");
    }

    [TestCase("미번역")]
    [TestCase("Untranslated")]
    [TestCase("未翻訳")]
    [TestCase("未翻译")]
    [TestCase("Sin traducir")]
    public void MissingTranslationMarkers_AreNeverLocalizationSources(string marker)
    {
        Assert.That(LocalizationTextRules.IsKoreanPlayerText(marker), Is.False);
    }

    [Test]
    public void ExistingRegisteredKey_RemainsAuthoritativeWhenDisplayedTextChanges()
    {
        Assert.That(
            LocalizationBindingSourcePolicy.SelectExistingRegisteredKey(
                "ui.sample.label",
                new[] { "ui.sample.label" }),
            Is.EqualTo("ui.sample.label"));
    }

    [Test]
    public void StaticBinding_SourceMismatch_SelectsGeneratedContextKeyInsteadOfExistingKey()
    {
        var resolver = new LocalizationBindingResolver(new[]
        {
            new LocalizationBindingEntry("common.recovery", "마나재생량"),
        });

        Assert.That(
            LocalizationBindingSourcePolicy.SelectStaticBindingKey(
                "회복",
                "common.recovery",
                "ui.battle.rest_room.heal_button",
                resolver),
            Is.EqualTo("ui.battle.rest_room.heal_button"));
    }

    [Test]
    public void StaticBinding_MatchingSource_PreservesExistingKey()
    {
        var resolver = new LocalizationBindingResolver(new[]
        {
            new LocalizationBindingEntry("common.recovery", "마나재생량"),
        });

        Assert.That(
            LocalizationBindingSourcePolicy.SelectStaticBindingKey(
                "마나재생량",
                "common.recovery",
                "ui.generated.recovery",
                resolver),
            Is.EqualTo("common.recovery"));
    }

    [Test]
    public void ExplicitLocaleLookup_UsesRequestedLocaleForMissingMarker()
    {
        Locale japanese = Locale.CreateLocale("ja");
        try
        {
            Assert.That(
                GameLocalization.GetForLocale(
                    "test.key.that.does.not.exist",
                    "한국어 원문",
                    japanese),
                Is.EqualTo("未翻訳"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(japanese);
        }
    }

    [Test]
    public void RuntimeReconciliation_RefreshesTextCreatedAfterLocaleEvent()
    {
        Locale japanese = Locale.CreateLocale("ja");
        var gameObject = new GameObject("Late Localized Text");
        gameObject.SetActive(false);

        try
        {
            TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "한국어 원문";
            LocalizedTMPText localizer = gameObject.AddComponent<LocalizedTMPText>();
            localizer.Configure("test.key.that.does.not.exist", "한국어 원문", true);

            LocalizationRuntimeRefreshCoordinator.RefreshAllNow(japanese);

            Assert.That(text.text, Is.EqualTo("未翻訳"),
                "언어 변경 이벤트 뒤 생성된 비활성 텍스트도 오브젝트 토글 없이 현재 언어로 맞아야 합니다.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
            UnityEngine.Object.DestroyImmediate(japanese);
        }
    }
}
