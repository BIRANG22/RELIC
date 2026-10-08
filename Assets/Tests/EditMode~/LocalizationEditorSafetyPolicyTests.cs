using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public sealed class LocalizationEditorSafetyPolicyTests
{
    [Test]
    public void DiscoveredDynamicCandidate_IsRetainedDuringUnusedKeyCleanup()
    {
        var candidate = new LocalizationCandidate(
            "Assets/Project/Scenes/YDM/Lobby.unity",
            "침식도",
            "ui.lobby.dynamic.erosion",
            true,
            true);

        var retained = LocalizationEditorSafetyPolicy.CollectRetainedCandidateKeys(new[] { candidate });

        Assert.That(retained, Does.Contain("ui.lobby.dynamic.erosion"),
            "전체 검사에서 발견한 동적/직렬화 원문 키를 같은 적용 과정의 미사용 정리에서 삭제하면 안 됩니다.");
    }

    [Test]
    public void ApplySafe_AlwaysPerformsFinalWorkbookToStringTableSync()
    {
        string source = File.ReadAllText("Assets/Editor/LocalizationManagerWindow.cs");
        int cleanupIndex = source.IndexOf("int removed = RemoveUnusedEntries", StringComparison.Ordinal);
        int finalImportIndex = source.IndexOf(
            "LocalizationExcelImporter.Import(); // Final authoritative workbook-to-table sync.",
            StringComparison.Ordinal);

        Assert.That(cleanupIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(finalImportIndex, Is.GreaterThan(cleanupIndex),
            "외부 편집이나 이전 실패에서 남은 String Table 항목도 최종 워크북 기준으로 제거되어야 합니다.");
    }

    [Test]
    public void BindingRepair_ValidatesSourceBeforePreservingExistingKey()
    {
        string source = File.ReadAllText("Assets/Editor/LocalizationTextBindingRepairTool.cs");

        Assert.That(source, Does.Contain("resolver.Validate(source, currentKey)"));
        Assert.That(source, Does.Not.Contain("resolver.TryGetKorean(currentKey, out _)"));
    }

    [Test]
    public void SceneScan_CrossValidatesActualStaticTextOneSceneAtATime()
    {
        string source = File.ReadAllText("Assets/Editor/LocalizationManagerWindow.cs");
        int scanSceneStart = source.IndexOf("private void ScanScene", StringComparison.Ordinal);
        int scanTextsStart = source.IndexOf("private void ScanTexts", scanSceneStart, StringComparison.Ordinal);
        string scanScene = source.Substring(scanSceneStart, scanTextsStart - scanSceneStart);

        Assert.That(scanScene, Does.Contain("OpenScene(path, OpenSceneMode.Single)"));
        Assert.That(scanScene, Does.Not.Contain("OpenPreviewScene"));
        Assert.That(scanScene, Does.Not.Contain("ClosePreviewScene"));
        Assert.That(scanScene, Does.Not.Contain("OpenSceneMode.Additive"));
        Assert.That(scanScene, Does.Contain("ScanTexts("));
        Assert.That(scanScene, Does.Not.Contain("FindUnityYamlTmpTexts"));
    }

    [Test]
    public void ApplySafe_RemovesUnusedKeysOnceAfterAllBindingRepairs()
    {
        string source = File.ReadAllText("Assets/Editor/LocalizationManagerWindow.cs");
        int repairIndex = source.IndexOf("LocalizationTextBindingRepairTool.RepairAllBindings();", StringComparison.Ordinal);
        int cleanupIndex = source.IndexOf("int removed = RemoveUnusedEntries", StringComparison.Ordinal);

        Assert.That(repairIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(cleanupIndex, Is.GreaterThan(repairIndex));
        Assert.That(
            source.Split(new[] { "RemoveUnusedEntries(" }, StringSplitOptions.None).Length - 1,
            Is.EqualTo(2),
            "메서드 선언 1회와 전체 적용 호출 1회 외에 키별 정리를 수행하면 안 됩니다.");
    }

    [Test]
    public void TryValidateWorkbookWritable_WhenExcelOwnerFileExists_ReturnsActionableFailure()
    {
        string directory = Path.Combine(Path.GetTempPath(), "RelicLocalizationPolicyTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string workbookPath = Path.Combine(directory, "Localization.xlsx");
        string ownerPath = Path.Combine(directory, "~$Localization.xlsx");
        File.WriteAllText(workbookPath, "workbook");
        File.WriteAllText(ownerPath, "owner");

        try
        {
            bool valid = LocalizationEditorSafetyPolicy.TryValidateWorkbookWritable(
                workbookPath,
                out string error);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.Contain("Excel"));
            Assert.That(error, Does.Contain("닫"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TryValidateWorkbookWritable_WhenAnotherStreamOwnsFile_ReturnsFailure()
    {
        string directory = Path.Combine(Path.GetTempPath(), "RelicLocalizationPolicyTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string workbookPath = Path.Combine(directory, "Localization.xlsx");
        File.WriteAllText(workbookPath, "workbook");

        try
        {
            using (File.Open(workbookPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                bool valid = LocalizationEditorSafetyPolicy.TryValidateWorkbookWritable(
                    workbookPath,
                    out string error);

                Assert.That(valid, Is.False);
                Assert.That(error, Does.Contain("사용 중"));
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ShouldScanProjectAsset_ExcludesDownloadedVendorContent()
    {
        Assert.That(
            LocalizationEditorSafetyPolicy.ShouldScanProjectAsset(
                "Assets/Project/Download/vfx/Vendor/Demo.unity"),
            Is.False);
        Assert.That(
            LocalizationEditorSafetyPolicy.ShouldScanProjectAsset(
                "Assets/Project/PrefabsR/MenuPanel.prefab"),
            Is.True);
    }

    [Test]
    public void ShouldScanSceneAsset_IncludesConfiguredScenesAndExcludesLooseTestScenes()
    {
        var configuredScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Assets/Project/Scenes/YDM/Battle.unity",
            "Assets/Project/Scenes/YDM/Lobby.unity",
        };

        Assert.That(
            LocalizationEditorSafetyPolicy.ShouldScanSceneAsset(
                "Assets/Project/Scenes/YDM/Battle.unity",
                configuredScenes),
            Is.True);
        Assert.That(
            LocalizationEditorSafetyPolicy.ShouldScanSceneAsset(
                "Assets/Project/Scenes/YDM/TestAni.unity",
                configuredScenes),
            Is.False);
    }

    [Test]
    public void ShouldProcessLocalizationPrefab_RejectsMissingScriptYaml()
    {
        const string prefab = "--- !u!114 &12\nMonoBehaviour:\n  m_Script: {fileID: 0}\n";

        Assert.That(LocalizationEditorSafetyPolicy.ShouldProcessLocalizationPrefab(prefab), Is.False,
            "Missing Script 프리팹을 수정·저장하면 전체 검사 및 적용이 중단됩니다.");
    }

    [Test]
    public void AssetMutation_IsRejectedWhileEditorIsPlaying()
    {
        Assert.That(LocalizationEditorSafetyPolicy.CanRunAssetMutation(editorIsPlaying: true), Is.False,
            "Play Mode의 런타임 TMP 감시기가 Prefab Stage 오브젝트에 컴포넌트를 추가할 수 있으므로 자동 적용을 실행하면 안 됩니다.");
        Assert.That(LocalizationEditorSafetyPolicy.CanRunAssetMutation(editorIsPlaying: false), Is.True);
    }

    [Test]
    public void DynamicLocalizedTmpText_UsesDedicatedMatchingScriptFile()
    {
        const string dedicatedPath = "Assets/Project/Scripts/Core/Localization/DynamicLocalizedTMPText.cs";

        Assert.That(File.Exists(dedicatedPath), Is.True,
            "Unity가 런타임 추가 MonoBehaviour를 직렬화하려면 클래스명과 같은 스크립트 파일이 필요합니다.");
        Assert.That(File.ReadAllText(dedicatedPath), Does.Contain("class DynamicLocalizedTMPText"));
        Assert.That(
            File.ReadAllText("Assets/Project/Scripts/Core/Localization/RuntimeTMPTextAutoLocalizer.cs"),
            Does.Not.Contain("class DynamicLocalizedTMPText"));
    }

    [Test]
    public void MenuPanelPrefab_HasNoMissingScriptYaml()
    {
        string prefab = File.ReadAllText("Assets/Project/PrefabsR/MenuPanel.prefab");

        Assert.That(prefab, Does.Not.Contain("m_Script: {fileID: 0}"));
    }

    [Test]
    public void ExcelImporter_DefersAssetRefreshUntilAllTablesAreSaved()
    {
        string source = File.ReadAllText("Assets/Editor/LocalizationExcelImporter.cs");

        int start = source.IndexOf("AssetDatabase.StartAssetEditing()", StringComparison.Ordinal);
        int import = source.IndexOf("Csv.ImportInto(", StringComparison.Ordinal);
        int finallyBlock = source.IndexOf("finally", import, StringComparison.Ordinal);
        int stop = source.IndexOf("AssetDatabase.StopAssetEditing()", import, StringComparison.Ordinal);
        int refresh = source.IndexOf("AssetDatabase.Refresh()", import, StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(start, Is.LessThan(import));
        Assert.That(finallyBlock, Is.GreaterThan(import));
        Assert.That(stop, Is.GreaterThan(finallyBlock));
        Assert.That(refresh, Is.GreaterThan(stop));
    }
}
