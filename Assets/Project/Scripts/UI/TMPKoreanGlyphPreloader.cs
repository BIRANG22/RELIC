using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public sealed class TMPKoreanGlyphPreloader : MonoBehaviour
{
    [Header("Target Font Assets")]
    [SerializeField] private TMP_FontAsset[] targetFonts;

    [Header("Resources TextAsset Paths")]
    [SerializeField] private string[] resourcesTextAssetPaths = { "Data/GameData" };

    [Header("Characters")]
    [SerializeField] private bool includeBasicAscii = true;
    [SerializeField] private bool includeKoreanJamo = true;
    [SerializeField] private bool includeAllModernHangul = false;
    [SerializeField] private string extraCharacters = "";

    [Header("Options")]
    [SerializeField] private bool preloadOnAwake = true;
    [SerializeField] private bool logResult = false;

    private bool hasPreloaded;

    private void Awake()
    {
        if (preloadOnAwake)
            Preload();
    }

    public void Preload()
    {
        if (hasPreloaded)
            return;

        hasPreloaded = true;

        if (targetFonts == null || targetFonts.Length == 0)
            return;

        string characters = BuildCharacters();
        if (string.IsNullOrEmpty(characters))
            return;

        for (int i = 0; i < targetFonts.Length; i++)
        {
            TMP_FontAsset font = targetFonts[i];
            if (font == null)
                continue;

            TryPreloadFont(font, characters);
        }
    }

    private void TryPreloadFont(TMP_FontAsset font, string characters)
    {
        if (font.atlasPopulationMode == AtlasPopulationMode.Static)
        {
            if (logResult)
                Debug.Log($"[TMPKoreanGlyphPreloader] '{font.name}'은 Static Font Asset이라 런타임 글리프 추가를 건너뜁니다.", font);

            return;
        }

        try
        {
            font.TryAddCharacters(characters, out string missingCharacters);

            if (logResult)
            {
                int missingCount = string.IsNullOrEmpty(missingCharacters) ? 0 : missingCharacters.Length;
                Debug.Log($"[TMPKoreanGlyphPreloader] '{font.name}' 글리프 프리로드 완료. Missing Count: {missingCount}", font);
            }
        }
        catch
        {
            if (logResult)
                Debug.Log($"[TMPKoreanGlyphPreloader] '{font.name}' 글리프 프리로드 중 예외가 발생했지만 무시했습니다.", font);
        }
    }

    private string BuildCharacters()
    {
        HashSet<char> set = new HashSet<char>();

        AddTextAssetCharacters(set);

        if (includeBasicAscii)
            AddRange(set, 0x20, 0x7E);

        if (includeKoreanJamo)
        {
            AddRange(set, 0x3131, 0x318E);
            AddRange(set, 0x1100, 0x11FF);
        }

        if (includeAllModernHangul)
            AddRange(set, 0xAC00, 0xD7A3);

        if (!string.IsNullOrEmpty(extraCharacters))
        {
            for (int i = 0; i < extraCharacters.Length; i++)
                set.Add(extraCharacters[i]);
        }

        StringBuilder builder = new StringBuilder(set.Count);
        foreach (char c in set)
            builder.Append(c);

        return builder.ToString();
    }

    private void AddTextAssetCharacters(HashSet<char> set)
    {
        if (resourcesTextAssetPaths == null)
            return;

        for (int i = 0; i < resourcesTextAssetPaths.Length; i++)
        {
            string path = resourcesTextAssetPaths[i];
            if (string.IsNullOrWhiteSpace(path))
                continue;

            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null || string.IsNullOrEmpty(asset.text))
                continue;

            string text = asset.text;
            for (int c = 0; c < text.Length; c++)
                set.Add(text[c]);
        }
    }

    private static void AddRange(HashSet<char> set, int startInclusive, int endInclusive)
    {
        for (int code = startInclusive; code <= endInclusive; code++)
            set.Add((char)code);
    }
}
