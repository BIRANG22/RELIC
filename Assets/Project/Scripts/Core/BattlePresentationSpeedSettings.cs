using UnityEngine;

/// <summary>
/// 전투 행동의 연출 재생 속도만 관리합니다.
/// 예약 턴, 전투 판정, 전역 Time.timeScale에는 관여하지 않습니다.
/// </summary>
public static class BattlePresentationSpeedSettings
{
    public const string PreferenceKey = "Relic.BattlePresentationSpeedIndex";

    // 표시 배속과 실제 연출 속도는 별도로 관리합니다.
    // 기본 1초 연출이 각각 0.85 / 0.70 / 0.50 / 0.35초에 실행되도록 설정합니다.
    private static readonly float[] DisplayMultipliers = { 1f, 1.5f, 2f, 3f };
    private static readonly float[] Multipliers = { 1f / 0.85f, 1f / 0.70f, 2f, 1f / 0.35f };

    public static int CurrentIndex => SanitizeIndex(PlayerPrefs.GetInt(PreferenceKey, 0));
    public static float CurrentMultiplier => Multipliers[CurrentIndex];
    public static float CurrentDisplayMultiplier => DisplayMultipliers[CurrentIndex];

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
        return Mathf.Clamp(index, 0, Multipliers.Length - 1);
    }
}
