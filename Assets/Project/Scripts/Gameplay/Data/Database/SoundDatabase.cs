using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundCategory
{
    Bgm,
    Sfx,
    EventSfx
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
    [SerializeField] private List<SoundData> eventSfxList = new();
    [SerializeField] private List<VfxSoundData> playerSkillVfxSfxList = new();
    [SerializeField] private List<VfxSoundData> monsterSkillVfxSfxList = new();

    private Dictionary<string, SoundData> bgmById;
    private Dictionary<string, SoundData> sfxById;
    private Dictionary<GameObject, VfxSoundData> skillVfxSfxByPrefab;

    public IReadOnlyList<SoundData> BgmEntries => bgmList;
    public IReadOnlyList<SoundData> SfxEntries => sfxList;
    public IReadOnlyList<SoundData> EventSfxEntries => eventSfxList;
    public IReadOnlyList<VfxSoundData> PlayerSkillVfxSfxEntries => playerSkillVfxSfxList;
    public IReadOnlyList<VfxSoundData> MonsterSkillVfxSfxEntries => monsterSkillVfxSfxList;

    public void Initialize()
    {
        bgmById = new Dictionary<string, SoundData>(StringComparer.Ordinal);
        sfxById = new Dictionary<string, SoundData>(StringComparer.Ordinal);
        skillVfxSfxByPrefab = new Dictionary<GameObject, VfxSoundData>();

        RegisterBgmList();
        RegisterSfxList(sfxList, "SFX");
        RegisterSfxList(eventSfxList, "Event SFX");
        RegisterVfxSoundList(playerSkillVfxSfxList, "Player Skill VFX SFX");
        RegisterVfxSoundList(monsterSkillVfxSfxList, "Monster Skill VFX SFX");
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

    public bool TryGetSkillVfxSfx(GameObject vfxPrefab, out VfxSoundData data)
    {
        if (skillVfxSfxByPrefab == null)
            Initialize();

        data = null;

        return vfxPrefab != null &&
            skillVfxSfxByPrefab.TryGetValue(vfxPrefab, out data) &&
            data != null;
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

    private void RegisterSfxList(
        IReadOnlyList<SoundData> entries,
        string label)
    {
        if (entries == null)
            return;

        foreach (SoundData data in entries)
        {
            if (data == null || data.clip == null)
                continue;

            data.volume = Mathf.Clamp01(data.volume);

            RegisterIds(sfxById, data, label);
        }
    }

    private void RegisterVfxSoundList(
        IReadOnlyList<VfxSoundData> entries,
        string label)
    {
        if (entries == null)
            return;

        foreach (VfxSoundData data in entries)
        {
            if (data == null || data.vfxPrefab == null)
                continue;

            data.Normalize();

            if (!skillVfxSfxByPrefab.ContainsKey(data.vfxPrefab))
                skillVfxSfxByPrefab.Add(data.vfxPrefab, data);
            else
                Debug.LogWarning($"[SoundDatabase] Duplicate {label} Prefab: {data.vfxPrefab.name}");
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
public class VfxSoundData
{
    [InspectorName("Name")]
    [Tooltip("플레이어/몬스터 스킬 SFX 항목을 구분하기 위해 직접 지정하는 이름입니다.")]
    public string displayName;

    public GameObject vfxPrefab;
    public List<VfxSoundCue> cues = new();

    public IReadOnlyList<VfxSoundCue> Cues => cues ??= new List<VfxSoundCue>();

    public bool HasPlayableCue
    {
        get
        {
            if (cues == null)
                return false;

            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i] != null && cues[i].clip != null)
                    return true;
            }

            return false;
        }
    }

    public void Normalize()
    {
        cues ??= new List<VfxSoundCue>();

        for (int i = 0; i < cues.Count; i++)
            cues[i]?.Normalize();
    }
}

[Serializable]
public class VfxSoundCue
{
    public AudioClip clip;

    [Min(0f)]
    public float delay;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(SoundData.MinPitch, SoundData.MaxPitch)]
    public float pitch = 1f;

    public bool loop;

    public bool useRandomPitch;

    [Range(SoundData.MinPitch, SoundData.MaxPitch)]
    public float randomPitchMin = 1f;

    [Range(SoundData.MinPitch, SoundData.MaxPitch)]
    public float randomPitchMax = 1f;

    public float GetPlaybackPitch()
    {
        if (!useRandomPitch)
            return SoundData.ClampPlaybackPitch(pitch);

        float min = SoundData.ClampPlaybackPitch(randomPitchMin);
        float max = SoundData.ClampPlaybackPitch(randomPitchMax);

        if (min > max)
            (min, max) = (max, min);

        return Mathf.Approximately(min, max)
            ? min
            : UnityEngine.Random.Range(min, max);
    }

    public void Normalize()
    {
        delay = Mathf.Max(0f, delay);
        volume = Mathf.Clamp01(volume);
        pitch = SoundData.ClampPlaybackPitch(pitch);
        randomPitchMin = SoundData.ClampPlaybackPitch(randomPitchMin);
        randomPitchMax = SoundData.ClampPlaybackPitch(randomPitchMax);
    }
}

[Serializable]
public class SoundData
{
    public const float MinPitch = 0f;
    public const float MaxPitch = 3f;

    public string id;
    public List<string> aliases = new();
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(MinPitch, MaxPitch)]
    public float pitch = 1f;

    public bool loop;

    public bool useRandomPitch;

    [Range(MinPitch, MaxPitch)]
    public float randomPitchMin = 1f;

    [Range(MinPitch, MaxPitch)]
    public float randomPitchMax = 1f;

    public float GetPlaybackPitch()
    {
        if (!useRandomPitch)
            return ClampPlaybackPitch(pitch);

        float min = ClampPlaybackPitch(randomPitchMin);
        float max = ClampPlaybackPitch(randomPitchMax);

        if (min > max)
            (min, max) = (max, min);

        return Mathf.Approximately(min, max)
            ? min
            : UnityEngine.Random.Range(min, max);
    }

    public static float ClampPlaybackPitch(float value)
    {
        return Mathf.Clamp(value, MinPitch, MaxPitch);
    }
}
