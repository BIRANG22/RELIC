using System;
using Relic.Gameplay.Data;
using UnityEngine;

public static class BattleErosionRuntimeService
{
    public const int MinValue = 0;
    public const int MaxValue = 100;

    public static event Action<int, int> ValueChanged;

    public static int CurrentValue
    {
        get
        {
            BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();
            return runtime != null ? Mathf.Clamp(runtime.ErosionValue, MinValue, MaxValue) : MinValue;
        }
    }

    public static void Add(int amount, string reason = null)
    {
        if (amount == 0)
            return;

        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();
        if (runtime == null)
            return;

        int before = Mathf.Clamp(runtime.ErosionValue, MinValue, MaxValue);
        int after = Mathf.Clamp(before + amount, MinValue, MaxValue);

        if (before == after)
            return;

        runtime.ErosionValue = after;
        DataManager.Instance.BattleRuntimeStore.Set(runtime);
        ValueChanged?.Invoke(before, after);

        if (!string.IsNullOrWhiteSpace(reason))
            Debug.Log($"[BattleErosion] {before} -> {after} ({reason})");

        if (after >= MaxValue)
            TriggerGameOver();
    }

    public static void SetValue(int value, string reason = null)
    {
        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();
        if (runtime == null)
            return;

        int before = Mathf.Clamp(runtime.ErosionValue, MinValue, MaxValue);
        int after = Mathf.Clamp(value, MinValue, MaxValue);

        if (before == after)
            return;

        runtime.ErosionValue = after;
        DataManager.Instance.BattleRuntimeStore.Set(runtime);
        ValueChanged?.Invoke(before, after);

        if (!string.IsNullOrWhiteSpace(reason))
            Debug.Log($"[BattleErosion] {before} -> {after} ({reason})");

        if (after >= MaxValue)
            TriggerGameOver();
    }

    public static void CountRoomEntry(GeneratedMapNodeData nodeData)
    {
        if (nodeData == null)
            return;

        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();
        if (runtime == null)
            return;

        string roomKey = BuildRoomKey(nodeData);
        if (string.Equals(runtime.LastErosionRoomKey, roomKey, StringComparison.Ordinal))
            return;

        runtime.LastErosionRoomKey = roomKey;
        DataManager.Instance.BattleRuntimeStore.Set(runtime);

        // 일반 탐사는 Layer 0의 최초 이벤트방에서 침식도를 올리지 않아 시작값 0을 유지합니다.
        // 튜토리얼 전투는 첫 진입부터 침식도 1로 시작해야 하므로 Layer 0도 방 진입으로 계산합니다.
        if (nodeData.LayerIndex == 0 && !runtime.IsTutorialBattle)
            return;

        Add(1, $"RoomEntry:{roomKey}");
    }

    public static void CountDirectMonsterHit()
    {
        Add(1, "DirectMonsterHit");
    }

    public static void CountCharacterIncapacitated()
    {
        Add(10, "CharacterIncapacitated");
    }

    public static void CountNextTurnStart()
    {
        Add(1, "NextTurnStart");
    }

    private static string BuildRoomKey(GeneratedMapNodeData nodeData)
    {
        return $"{nodeData.LayerIndex}:{nodeData.NodeIndex}:{nodeData.MapId}";
    }

    private static void TriggerGameOver()
    {
        BattleResultChecker checker = BattleResultChecker.Instance;
        if (checker == null)
        {
            checker = UnityEngine.Object.FindFirstObjectByType<BattleResultChecker>(
                FindObjectsInactive.Include);
        }

        checker?.ForceDefeatFromErosion();
    }
}
