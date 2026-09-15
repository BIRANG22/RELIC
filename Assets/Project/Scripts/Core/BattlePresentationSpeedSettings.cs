using UnityEngine;

/// <summary>
/// 전투 행동의 연출 재생 속도만 관리합니다.
/// 예약 턴, 전투 판정, 전역 Time.timeScale에는 관여하지 않습니다.
/// </summary>
public static class BattlePresentationSpeedSettings
{
    public const string PreferenceKey = "Relic.BattlePresentationSpeedIndex";

    private static readonly float[] Multipliers = { 1f, 1.5f, 2f, 3f, 4f };

    public static int CurrentIndex => SanitizeIndex(PlayerPrefs.GetInt(PreferenceKey, 0));
    public static float CurrentMultiplier => Multipliers[CurrentIndex];

    public static int OptionCount => Multipliers.Length;

    public static float GetMultiplier(int index)
    {
        return Multipliers[SanitizeIndex(index)];
    }

    public static void SetSelectedIndex(int index)
    {
        PlayerPrefs.SetInt(PreferenceKey, SanitizeIndex(index));
        PlayerPrefs.Save();
    }

    public static float ScaleDuration(float duration)
    {
        return Mathf.Max(0f, duration) / CurrentMultiplier;
    }

    private static int SanitizeIndex(int index)
    {
        return index >= 0 && index < Multipliers.Length ? index : 0;
    }
}
