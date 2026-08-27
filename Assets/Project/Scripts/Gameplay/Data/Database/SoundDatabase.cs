using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundCategory
{
    Bgm,
    Sfx
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class SoundIdAttribute : PropertyAttribute
{
    public const string DefaultDatabasePath = "Assets/DB/SoundDatabase.asset";

    public SoundCategory Category { get; }
    public string DatabasePath { get; }

    public SoundIdAttribute(SoundCategory category)
        : this(category, DefaultDatabasePath)
    {
    }

    public SoundIdAttribute(SoundCategory category, string databasePath)
    {
        Category = category;
        DatabasePath = string.IsNullOrWhiteSpace(databasePath)
            ? DefaultDatabasePath
            : databasePath;
    }
}

[CreateAssetMenu(menuName = "Relic/Data/Sound Database")]
public class SoundDatabase : ScriptableObject
{
    [SerializeField] private List<SoundData> bgmList = new();
    [SerializeField] private List<SoundData> sfxList = new();

    private Dictionary<string, SoundData> bgmById;
    private Dictionary<string, SoundData> sfxById;

    public IReadOnlyList<SoundData> BgmEntries => bgmList;
    public IReadOnlyList<SoundData> SfxEntries => sfxList;

    public void Initialize()
    {
        bgmById = new Dictionary<string, SoundData>(StringComparer.Ordinal);
        sfxById = new Dictionary<string, SoundData>(StringComparer.Ordinal);

        RegisterBgmList();
        RegisterSfxList();
    }

    public bool TryGetBgm(string id, out SoundData bgm)
    {
        if (bgmById == null)
            Initialize();

        id = NormalizeId(id);
        bgm = null;
        return !string.IsNullOrEmpty(id) && bgmById.TryGetValue(id, out bgm) && bgm != null;
    }

    public bool TryGetSfx(string id, out SoundData sfx)
    {
        if (sfxById == null)
            Initialize();

        id = NormalizeId(id);
        sfx = null;
        return !string.IsNullOrEmpty(id) && sfxById.TryGetValue(id, out sfx) && sfx != null;
    }

    private void RegisterBgmList()
    {
        if (bgmList == null)
            return;

        foreach (SoundData data in bgmList)
        {
            if (data == null || data.clip == null)
                continue;

            data.volume = Mathf.Clamp01(data.volume);

            RegisterIds(bgmById, data, "BGM");
        }
    }

    private void RegisterSfxList()
    {
        if (sfxList == null)
            return;

        foreach (SoundData data in sfxList)
        {
            if (data == null || data.clip == null)
                continue;

            data.volume = Mathf.Clamp01(data.volume);

            RegisterIds(sfxById, data, "SFX");
        }
    }

    private static void RegisterIds(
        Dictionary<string, SoundData> map,
        SoundData data,
        string label)
    {
        RegisterId(map, data, data.id, label);

        if (data.aliases == null)
            return;

        foreach (string alias in data.aliases)
            RegisterId(map, data, alias, label);
    }

    private static void RegisterId(
        Dictionary<string, SoundData> map,
        SoundData data,
        string id,
        string label)
    {
        id = NormalizeId(id);
        if (string.IsNullOrEmpty(id))
            return;

        if (!map.ContainsKey(id))
            map.Add(id, data);
        else
            Debug.LogWarning($"[SoundDatabase] Duplicate {label} ID: {id}");
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}

[Serializable]
public class SoundData
{
    public string id;
    public List<string> aliases = new();
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0f, 1f)]
    public float pitch = 1f;

    public bool useRandomPitch;

    [Range(0f, 1f)]
    public float randomPitchMin = 1f;

    [Range(0f, 1f)]
    public float randomPitchMax = 1f;

    public float GetPlaybackPitch()
    {
        if (!useRandomPitch)
            return Mathf.Clamp01(pitch);

        float min = Mathf.Clamp01(randomPitchMin);
        float max = Mathf.Clamp01(randomPitchMax);

        if (min > max)
            (min, max) = (max, min);

        return Mathf.Approximately(min, max)
            ? min
            : UnityEngine.Random.Range(min, max);
    }
}
