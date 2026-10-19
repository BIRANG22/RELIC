using UnityEngine;

public static class BattleRoomPartyTransitionMath
{
    public static Vector3 GetEntryStart(Vector3 finalPosition, Vector3 entryOffset) =>
        finalPosition + entryOffset;

    public static Vector3 GetExitTarget(Vector3 currentPosition, Vector3 exitOffset) =>
        currentPosition + exitOffset;

    public static Vector3 Evaluate(
        Vector3 from,
        Vector3 to,
        float normalizedTime,
        AnimationCurve curve)
    {
        float time = Mathf.Clamp01(normalizedTime);
        float evaluated = curve != null ? curve.Evaluate(time) : time;
        return Vector3.LerpUnclamped(from, to, evaluated);
    }
}
