using System;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

public static class BattleErosionRuntimeService
{
    public const int MinValue = 0;
    public const int MaxValue = 100;
    public const int RoomEntryAmount = 3;

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

        Add(RoomEntryAmount, $"RoomEntry:{roomKey}");
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

    // 침식도가 100인 상태에서 다음 턴이 시작되면 생존 파티원의 체력을 10 감소시킵니다.
    // 방어도에 영향을 주지 않는 순수 체력 감소입니다.
    public static void ApplyMaxErosionTurnStartHealthLoss()
    {
        if (CurrentValue < MaxValue)
            return;

        BattleCharacter[] characters = UnityEngine.Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var processed = new System.Collections.Generic.HashSet<CharacterRuntimeData>();
        bool healthChanged = false;

        foreach (BattleCharacter character in characters)
        {
            if (character == null || !character.gameObject.activeInHierarchy)
                continue;

            CharacterRuntimeData data = character.RuntimeData;
            if (data == null || data.IsDead || !processed.Add(data))
                continue;

            int before = data.CurrentHP;
            data.CurrentHP = Mathf.Max(0, before - 10);
            int loss = before - data.CurrentHP;
            if (loss <= 0)
                continue;

            BattleDamageTextPopupUI.PrepareTargetHud(character.transform);
            BattleDamageTextPopupUI.ShowHealthLoss(character.transform, loss);
            BattleRunStatisticsRecorder.RecordDamageTaken(
                data.CharacterId, loss, data.CurrentHP <= 0);
            BattleEquipmentEffectService.MarkPlayerDamagedThisTurn(data);
            if (BattlePlayerIncapacitationService.Resolve(data, before))
            {
                BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();
                if (animator != null)
                    animator.PlayDead();
            }

            BattleEffectUtility.OnPlayerDamaged?.Invoke(character);
            BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(character);
            healthChanged = true;
        }

        if (healthChanged)
            BattleEffectInfoFeed.ShowErosionDamage();

        if (healthChanged)
            Debug.Log("[BattleErosion] Max erosion: -10 HP to each living party member at turn start.");
    }
}
