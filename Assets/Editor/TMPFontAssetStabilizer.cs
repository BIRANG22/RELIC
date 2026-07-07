using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class TMPFontAssetStabilizer
{
    private const string MainFontAssetPath = "Assets/Fonts/TMP/DungGeunMo SDF.asset";
    private const string DynamicMainFallbackAssetPath = "Assets/TextMesh Pro/Fonts/DungGeunMo SDF.asset";
    private const string FallbackFontAssetPath = "Assets/Fonts/TMP/TMP_Font_KR.asset";
    private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/DungGeunMo.otf";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const string ClearDynamicDataOnBuildPropertyName = "m_ClearDynamicDataOnBuild";

    private static readonly string[] CharacterSourceRoots =
    {
        "Assets/Project/Scenes",
        "Assets/Project/PrefabsR",
        "Assets/Resources/Data",
        "Assets/AddressableAssetsData/AssetGroups"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".asset",
        ".bytes",
        ".csv",
        ".json",
        ".prefab",
        ".txt",
        ".unity"
    };

    [MenuItem("Tools/Fonts/Configure TMP Static Main + Dynamic Fallback")]
    public static void ConfigureFallbacksOnly()
    {
        if (!TryLoadFontAssets(out TMP_FontAsset mainFont, out TMP_FontAsset dynamicMainFallbackFont, out TMP_FontAsset fallbackFont))
            return;

        Undo.RecordObjects(new UnityEngine.Object[] { mainFont, dynamicMainFallbackFont, fallbackFont }, "Configure TMP Font Assets");

        EnsureStaticMainFont(mainFont);
        EnsureDynamicFallbackFont(dynamicMainFallbackFont);
        EnsureDynamicFallbackFont(fallbackFont);
        ApplyFallbacks(mainFont, dynamicMainFallbackFont, fallbackFont);
        ApplyFallbacks(dynamicMainFallbackFont, fallbackFont);
        ApplyGlobalFallback(fallbackFont);
        SaveFontAssets(mainFont, dynamicMainFallbackFont, fallbackFont);

        Debug.Log("[TMPFontAssetStabilizer] Configured DungGeunMo static font fallbacks to DungGeunMo Dynamic, then TMP_Font_KR.");
    }

    [MenuItem("Tools/Fonts/Rebuild DungGeunMo Static Atlas")]
    public static void RebuildStaticMainFont()
    {
        if (!TryLoadFontAssets(out TMP_FontAsset mainFont, out TMP_FontAsset dynamicMainFallbackFont, out TMP_FontAsset fallbackFont))
            return;

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"[TMPFontAssetStabilizer] Missing source font: {SourceFontPath}");
            return;
        }

        string characters = BuildStaticCharacterSet();
        if (string.IsNullOrEmpty(characters))
        {
            Debug.LogError("[TMPFontAssetStabilizer] No characters were collected for the static font atlas.");
            return;
        }

        Undo.RecordObjects(new UnityEngine.Object[] { mainFont, dynamicMainFallbackFont, fallbackFont }, "Rebuild TMP Static Font");

        EnsureDynamicFallbackFont(dynamicMainFallbackFont);
        EnsureDynamicFallbackFont(fallbackFont);
        ApplyFallbacks(mainFont, dynamicMainFallbackFont, fallbackFont);
        ApplyFallbacks(dynamicMainFallbackFont, fallbackFont);
        ApplyGlobalFallback(fallbackFont);

        mainFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        mainFont.isMultiAtlasTexturesEnabled = true;
        mainFont.ClearFontAssetData(false);

        if (mainFont.sourceFontFile == null)
        {
            Debug.LogError(
                "[TMPFontAssetStabilizer] DungGeunMo source font reference is missing after switching to Dynamic. " +
                "Open the font asset once in the Inspector or recreate it from the source OTF, then run this menu again.",
                mainFont);
            return;
        }

        bool allAdded = mainFont.TryAddCharacters(characters, out string missingCharacters, false);
        mainFont.atlasPopulationMode = AtlasPopulationMode.Static;
        SetClearDynamicDataOnBuild(mainFont, false);
        ApplyFallbacks(mainFont, dynamicMainFallbackFont, fallbackFont);
        ApplyFallbacks(dynamicMainFallbackFont, fallbackFont);

        SaveFontAssets(mainFont, dynamicMainFallbackFont, fallbackFont);

        int missingCount = string.IsNullOrEmpty(missingCharacters) ? 0 : missingCharacters.Length;
        string result = allAdded
            ? "all collected characters were added"
            : $"{missingCount} collected characters were missing from DungGeunMo and will use fallback";

        Debug.Log(
            $"[TMPFontAssetStabilizer] Rebuilt '{MainFontAssetPath}' as Static. " +
            $"Collected: {characters.Length}, Result: {result}.",
            mainFont);
    }

    private static bool TryLoadFontAssets(out TMP_FontAsset mainFont, out TMP_FontAsset dynamicMainFallbackFont, out TMP_FontAsset fallbackFont)
    {
        mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontAssetPath);
        dynamicMainFallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DynamicMainFallbackAssetPath);
        fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontAssetPath);

        if (mainFont == null)
            Debug.LogError($"[TMPFontAssetStabilizer] Missing main font asset: {MainFontAssetPath}");

        if (dynamicMainFallbackFont == null)
            Debug.LogError($"[TMPFontAssetStabilizer] Missing dynamic DungGeunMo fallback font asset: {DynamicMainFallbackAssetPath}");

        if (fallbackFont == null)
            Debug.LogError($"[TMPFontAssetStabilizer] Missing fallback font asset: {FallbackFontAssetPath}");

        return mainFont != null && dynamicMainFallbackFont != null && fallbackFont != null;
    }

    private static void EnsureStaticMainFont(TMP_FontAsset mainFont)
    {
        if (mainFont == null)
            return;

        if (mainFont.atlasPopulationMode != AtlasPopulationMode.Static)
            mainFont.atlasPopulationMode = AtlasPopulationMode.Static;

        SetClearDynamicDataOnBuild(mainFont, false);
        EditorUtility.SetDirty(mainFont);
    }

    private static void EnsureDynamicFallbackFont(TMP_FontAsset fallbackFont)
    {
        if (fallbackFont == null)
            return;

        if (fallbackFont.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            fallbackFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        SetClearDynamicDataOnBuild(fallbackFont, true);
        EditorUtility.SetDirty(fallbackFont);
    }

    private static bool SetClearDynamicDataOnBuild(TMP_FontAsset font, bool value)
    {
        if (font == null)
            return false;

        SerializedObject serializedFont = new(font);
        SerializedProperty property = serializedFont.FindProperty(ClearDynamicDataOnBuildPropertyName);
        if (property == null)
            return false;

        if (property.boolValue == value)
            return false;

        property.boolValue = value;
        serializedFont.ApplyModifiedProperties();
        return true;
    }

    private static void ApplyFallbacks(TMP_FontAsset mainFont, params TMP_FontAsset[] fallbackFonts)
    {
        if (mainFont == null)
            return;

        if (mainFont.fallbackFontAssetTable == null)
            mainFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

        mainFont.fallbackFontAssetTable.RemoveAll(font => font == null || font == mainFont);

        for (int i = fallbackFonts.Length - 1; i >= 0; i--)
        {
            TMP_FontAsset fallbackFont = fallbackFonts[i];
            if (fallbackFont == null || fallbackFont == mainFont)
                continue;

            mainFont.fallbackFontAssetTable.Remove(fallbackFont);
            mainFont.fallbackFontAssetTable.Insert(0, fallbackFont);
        }

        EditorUtility.SetDirty(mainFont);
    }

    private static void ApplyGlobalFallback(TMP_FontAsset fallbackFont)
    {
        TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null || fallbackFont == null)
            return;

        if (TMP_Settings.fallbackFontAssets == null)
            TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();

        TMP_Settings.fallbackFontAssets.RemoveAll(font => font == null);

        if (!TMP_Settings.fallbackFontAssets.Contains(fallbackFont))
            TMP_Settings.fallbackFontAssets.Insert(0, fallbackFont);

        EditorUtility.SetDirty(settings);
    }

    private static void SaveFontAssets(params TMP_FontAsset[] fonts)
    {
        if (fonts != null)
        {
            for (int i = 0; i < fonts.Length; i++)
            {
                TMP_FontAsset font = fonts[i];
                if (font == null)
                    continue;

                EditorUtility.SetDirty(font);

                Texture2D[] atlasTextures = font.atlasTextures;
                if (atlasTextures == null)
                    continue;

                for (int textureIndex = 0; textureIndex < atlasTextures.Length; textureIndex++)
                {
                    if (atlasTextures[textureIndex] != null)
                        EditorUtility.SetDirty(atlasTextures[textureIndex]);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static string BuildStaticCharacterSet()
    {
        HashSet<char> characters = new();

        AddRange(characters, 0x20, 0x7E);
        AddRange(characters, 0x3131, 0x318E);
        AddRange(characters, 0x1100, 0x11FF);
        AddProjectTextCharacters(characters);

        char[] result = new char[characters.Count];
        characters.CopyTo(result);
        Array.Sort(result);
        return new string(result);
    }

    private static void AddProjectTextCharacters(HashSet<char> characters)
    {
        for (int i = 0; i < CharacterSourceRoots.Length; i++)
        {
            string root = CharacterSourceRoots[i];
            if (!Directory.Exists(root))
                continue;

            foreach (string path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (!TextExtensions.Contains(Path.GetExtension(path)))
                    continue;

                AddFileCharacters(characters, path);
            }
        }
    }

    private static void AddFileCharacters(HashSet<char> characters, string path)
    {
        string text;

        try
        {
            text = File.ReadAllText(path, Encoding.UTF8);
        }
        catch
        {
            return;
        }

        AddTextCharacters(characters, text);
        AddTextCharacters(characters, DecodeUnicodeEscapes(text));
    }

    private static void AddTextCharacters(HashSet<char> characters, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (ShouldInclude(c))
                characters.Add(c);
        }
    }

    private static string DecodeUnicodeEscapes(string text)
    {
        return Regex.Replace(
            text,
            @"\\u([0-9a-fA-F]{4})",
            match => ((char)Convert.ToInt32(match.Groups[1].Value, 16)).ToString());
    }

    private static bool ShouldInclude(char c)
    {
        if (c >= 0x20 && c <= 0x7E)
            return true;

        if (c >= 0x1100 && c <= 0x11FF)
            return true;

        if (c >= 0x3131 && c <= 0x318E)
            return true;

        if (c >= 0xAC00 && c <= 0xD7A3)
            return true;

        if (c >= 0x2000 && c <= 0x206F)
            return true;

        if (c >= 0x3000 && c <= 0x303F)
            return true;

        if (c >= 0xFF00 && c <= 0xFFEF)
            return true;

        return false;
    }

    private static void AddRange(HashSet<char> characters, int startInclusive, int endInclusive)
    {
        for (int code = startInclusive; code <= endInclusive; code++)
            characters.Add((char)code);
    }
}
