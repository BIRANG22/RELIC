using System;
using System.Threading.Tasks;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

public readonly struct LobbyBattleEntryResult
{
    public LobbyBattleEntryResult(bool succeeded, string error)
    {
        Succeeded = succeeded;
        Error = error ?? string.Empty;
    }

    public bool Succeeded { get; }
    public string Error { get; }

    public static LobbyBattleEntryResult Success()
    {
        return new LobbyBattleEntryResult(true, string.Empty);
    }

    public static LobbyBattleEntryResult Fail(string error)
    {
        return new LobbyBattleEntryResult(false, error);
    }
}

public static class LobbyBattleEntryService
{
    public static void CommitRuntimeStateContributorsForBattleStart()
    {
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is not IRuntimeSaveStateContributor contributor)
                continue;

            try
            {
                contributor.CommitRuntimeStateForSave();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[LobbyBattleEntryService] Failed to commit runtime state before battle start. {exception}");
            }
        }
    }

    public static bool ApplyBattleStartMapRuntime(
        MapRuntimeStore mapStore,
        LobbyBattleStartCommand command)
    {
        if (mapStore == null ||
            command == null ||
            string.IsNullOrWhiteSpace(command.ChapterId) ||
            string.IsNullOrWhiteSpace(command.StageId))
        {
            return false;
        }

        mapStore.Set(new MapRuntimeData
        {
            SelectedChapterId = command.ChapterId.Trim(),
            CurrentStage = command.StageId.Trim(),
            CurrentMapId = string.Empty,
            CurrentSceneName = SceneName.Battle,
            IsRunInitialized = false
        });
        return true;
    }

    public static async Task<LobbyBattleEntryResult> EnterBattleAsync(
        LobbyBattleStartCommand command = null)
    {
        if (DataManager.Instance == null)
            return LobbyBattleEntryResult.Fail("DataManager is missing.");

        if (GameManager.Instance == null || GameManager.Instance.StateMachine == null)
            return LobbyBattleEntryResult.Fail("GameManager state machine is missing.");

        if (command != null)
        {
            if (!ApplyBattleStartMapRuntime(
                    DataManager.Instance.MapRuntimeStore,
                    command))
            {
                return LobbyBattleEntryResult.Fail("Battle start map command is invalid.");
            }

            BattleRandom.SetSeed(command.BattleSeed);
        }

        LobbyRuntimeData lobbyRuntime =
            DataManager.Instance.LobbyRuntimeStore?.GetOrCreate();
        BattleRuntimeData battleRuntime =
            DataManager.Instance.BattleRuntimeStore?.GetOrCreate();

        LobbyBattleRuntimeTransferResult transferResult =
            new LobbyBattleRuntimeTransferService().Transfer(
                lobbyRuntime,
                battleRuntime,
                DataManager.Instance.CharacterRuntimeStore);

        if (!transferResult.Succeeded)
            return LobbyBattleEntryResult.Fail(transferResult.Error);

        DataManager.Instance.BattleRuntimeStore.Set(battleRuntime);
        BattleRunAbandonService.CaptureLobbyLoadoutSnapshot(DataManager.Instance);

        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Battle);
        return LobbyBattleEntryResult.Success();
    }
}
