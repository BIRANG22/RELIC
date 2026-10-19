using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleStatusEffectService
{
    private const string FortressEffectId = "E_Fortress";
    private const int FortressPresentationIndex = 4;
    private const int FortressShieldPerTurn = 10;
    private const float FortressPresentationDelay = 0.65f;

    // This is a free, non-timeline action. Keep the former skill's action
    // presentation (including its configured VFX) before granting armor.
    public static IEnumerator PlayMonsterTurnStartTraitsRoutine()
    {
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        // 타임라인의 첫 행동을 기준으로 녹턴의 방향을 먼저 맞춥니다.
        // 포털 연출이 시작되기 전에도 올바른 방향을 바라보도록 합니다.
        BattleMonsterTurnPlanner nocturnPlanner = Object.FindFirstObjectByType<BattleMonsterTurnPlanner>(
            FindObjectsInactive.Include);
        if (nocturnPlanner != null)
        {
            foreach (MonsterUnit monster in monsters)
            {
                if (monster != null && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
                    nocturnPlanner.ApplyNocturnFirstReservedActionFacing(monster);
            }
        }

        foreach (MonsterUnit monster in monsters)
        {
            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            List<StatusEffectRuntimeData> statuses = monster.RuntimeData.StatusEffects;
            if (statuses == null)
                continue;

            bool hasFortress = false;
            foreach (StatusEffectRuntimeData status in statuses)
            {
                if (status != null && status.IsMonsterInnateTrait &&
                    string.Equals(status.EffectId, FortressEffectId, System.StringComparison.Ordinal))
                {
                    hasFortress = true;
                    break;
                }
            }

            if (!hasFortress)
                continue;

            BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();
            if (animator != null)
            {
                // The fortress trait is not a MonsterSkill row. Play the legacy
                // Rook action presentation directly without booking a timeline slot.
                animator.PlayMonsterActionPresentation(FortressPresentationIndex);
                yield return new WaitForSeconds(FortressPresentationDelay);
                // Free action presentations do not pass through the timeline's
                // normal animation-completion path, so restore the idle state.
                if (monster != null && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
                    animator.PlayIdle();
            }

            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            monster.RuntimeData.AddTemporaryShield(FortressShieldPerTurn);
            BattleDamageTextPopupUI.ShowArmorGain(monster.transform, FortressShieldPerTurn);
            monster.ShowAndRefreshHUD();
        }

        yield return PlayNocturnPortalPresentationsRoutine(monsters);
    }

    private const string NocturnPortalPresentationSkillId = "S_Monster_28";
    private const float NocturnPortalPresentationDelay = 0.65f;

    private static IEnumerator PlayNocturnPortalPresentationsRoutine(MonsterUnit[] monsters)
    {
        BattleMonsterTurnPlanner planner = Object.FindFirstObjectByType<BattleMonsterTurnPlanner>(
            FindObjectsInactive.Include);
        if (planner == null || monsters == null)
            yield break;

        foreach (MonsterUnit monster in monsters)
        {
            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            string runtimeId = monster.RuntimeData.RuntimeId;
            if (!planner.HasNocturnPortalReservation(runtimeId))
                continue;

            // 포털 설치 연출 중에는 이동할 포털을 바라봅니다.
            // 공격 방향은 연출 후 첫 예약 행동 및 실행 시점에 다시 적용합니다.
            planner.ApplyNocturnPortalPresentationFacing(monster);

            BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();
            if (animator != null)
            {
                int actionIndex = monster.RuntimeData.GetPresentationActionIndexForSkill(
                    NocturnPortalPresentationSkillId);
                if (actionIndex > 0)
                {
                    animator.PlayMonsterActionAnimationOnly(actionIndex);
                    // 행동 애니메이션이 시작되는 즉시 예약된 목적지에 포털을 표시합니다.
                    planner.ShowNocturnPortalIndicatorsForRuntime(runtimeId);
                    yield return new WaitForSeconds(NocturnPortalPresentationDelay);
                    if (monster != null && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
                    {
                        animator.PlayIdle();
                        // 다음에 실행할 공격은 포털 방향과 다를 수 있습니다.
                        planner.ApplyNocturnFirstReservedActionFacing(monster);
                    }
                    continue;
                }
            }

            // 애니메이션 컴포넌트가 없어도 예약된 포털은 표시합니다.
            if (monster != null && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
            {
                planner.ShowNocturnPortalIndicatorsForRuntime(runtimeId);
                planner.ApplyNocturnFirstReservedActionFacing(monster);
            }
        }
    }

    private const string SplitEffectId = "E_Split";
    private const string SpiderWebEffectId = "E_Spider_Web";
    private const string ExplodeEffectId = "E_Explode";

    private readonly BattleDamageService damageService;
    private readonly BattleDeathService deathService;

    public BattleStatusEffectService(
        BattleDamageService damageService,
        BattleDeathService deathService)
    {
        this.damageService = damageService;
        this.deathService = deathService;
    }

    public static bool TryConsumeOnTrigger(
        List<StatusEffectRuntimeData> statuses,
        string effectId,
        System.Func<string, EffectMasterData> effectResolver = null)
    {
        if (statuses == null || string.IsNullOrWhiteSpace(effectId))
            return false;

        effectResolver ??= ResolveEffect;
        EffectMasterData effect = effectResolver(effectId);
        if (effect == null || effect.DecreaseRule != EffectValueDecreaseRule.DecreaseOnTrigger)
            return false;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = statuses[i];
            if (status == null || !string.Equals(status.EffectId, effectId, System.StringComparison.Ordinal))
                continue;

            status.Stack = Mathf.Max(0, status.Stack - 1);
            if (status.Stack <= 0)
                statuses.RemoveAt(i);
            return true;
        }

        return false;
    }

    private static EffectMasterData ResolveEffect(string effectId)
    {
        return DataManager.Instance?.EffectDatabase?.Get(effectId);
    }

    /// <summary>
    /// ���Ͱ� ���� ���� Ÿ���� ���� �� �п� ��ġ�� 1 ���ҽ�ŵ�ϴ�.
    /// ��ġ�� 0�� �Ǿ��� �� ��� �ִٸ� true�� ��ȯ�մϴ�.
    /// </summary>
    public bool ApplySplitHitAndCheckTrigger(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return false;

        List<StatusEffectRuntimeData> statuses = monster.RuntimeData.StatusEffects;

        if (statuses == null)
            return false;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = statuses[i];

            if (status == null || status.EffectId != SplitEffectId)
                continue;

            status.Stack = Mathf.Max(0, status.Stack - 1);

            if (status.Stack > 0)
            {
                monster.ShowAndRefreshHUD();
                return false;
            }

            statuses.RemoveAt(i);
            monster.ShowAndRefreshHUD();
            return !monster.RuntimeData.IsDead;
        }

        return false;
    }

    public bool TryApplyPlayerSelfEffect(PlayerReservedCommand command, BattleCharacter caster)
    {
        if (command == null || command.SkillData == null || caster == null || caster.RuntimeData == null)
            return false;

        if (caster.RuntimeData.IsDead)
            return false;

        if (string.IsNullOrWhiteSpace(command.SkillData.EffectIds))
            return false;

        string[] effectIds = command.SkillData.EffectIds.Split(';');
        string[] values = !string.IsNullOrWhiteSpace(command.SkillData.ValueRate)
            ? command.SkillData.ValueRate.Split(';')
            : null;

        string[] counts = !string.IsNullOrWhiteSpace(command.SkillData.CountRate)
            ? command.SkillData.CountRate.Split(';')
            : null;

        bool applied = false;

        for (int i = 0; i < effectIds.Length; i++)
        {
            string effectId = effectIds[i].Trim();

            int value = values != null && i < values.Length
                ? damageService.ParseFirstInt(values[i])
                : damageService.ParseFirstInt(command.SkillData.ValueRate);

            int count = counts != null && i < counts.Length
                ? damageService.ParseFirstInt(counts[i])
                : damageService.ParseFirstInt(command.SkillData.CountRate);

            if (effectId == "E_Armor")
            {
                int gainedArmor = BattleEffectUtility.GetRepeatedValue(value, count);
                caster.RuntimeData.CurrentShield += gainedArmor;
                BattleDamageTextPopupUI.ShowArmorGain(caster.transform, gainedArmor);
                applied = true;
            }
            else if (effectId == "E_Boost")
            {
                AddOrStackStatusEffect(
                    caster.RuntimeData.StatusEffects,
                    "E_Boost",
                    value,
                    count
                );

                applied = true;
            }
        }

        return applied;
    }

    public void AddOrStackStatusEffect(
        List<StatusEffectRuntimeData> statusEffects,
        string effectId,
        int stack,
        int count)
    {
        if (statusEffects == null)
            return;

        int appliedStack = BattleEffectUtility.GetRepeatedValue(stack, count);

        if (appliedStack <= 0)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i] == null)
                continue;

            if (statusEffects[i].EffectId != effectId)
                continue;

            statusEffects[i].Stack += appliedStack;
            statusEffects[i].TurnCount = Mathf.Max(statusEffects[i].TurnCount, 1);
            return;
        }

        statusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = appliedStack,
            TurnCount = 1
        });
    }

    public void ApplyBleedDamageToPlayerOnMove(BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
            return;

        int bleedStack = damageService.GetStatusStack(character.RuntimeData.StatusEffects, "E_Bleed");

        if (bleedStack <= 0)
            return;

        BattleEffectUtility.StatusDamagePlayer(character, bleedStack);

        BattleUnitAnimator animator = character.GetComponent<BattleUnitAnimator>();

        if (animator != null && character.RuntimeData.CurrentHP <= 0)
            animator.PlayDead();
    }

    public void ApplyBleedDamageToMonsterOnMove(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null)
            return;

        int bleedStack = damageService.GetStatusStack(monster.RuntimeData.StatusEffects, "E_Bleed");

        if (bleedStack <= 0)
            return;

        BattleEffectUtility.StatusDamageMonster(monster, bleedStack);

        if (monster.RuntimeData.IsDead)
            deathService.HandleMonsterDead(monster);
    }

    public bool ApplyTurnEndEffects()
    {

        bool playedPresentation = false;

        playedPresentation |= ApplyTurnEndEffectsToPlayers();
        playedPresentation |= ApplyTurnEndEffectsToMonsters();

        return playedPresentation;
    }

    private bool ApplyTurnEndEffectsToPlayers()
    {
        bool playedPresentation = false;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                continue;

            playedPresentation |= ApplyPlayerTurnEndStatusEffects(character);
        }

        return playedPresentation;
    }

    private bool ApplyTurnEndEffectsToMonsters()
    {
        bool playedPresentation = false;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            playedPresentation |= ApplyMonsterTurnEndStatusEffects(monster);
        }

        return playedPresentation;
    }

    private bool ApplyPlayerTurnEndStatusEffects(BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
            return false;

        List<StatusEffectRuntimeData> statuses = character.RuntimeData.StatusEffects;

        if (statuses == null)
            return false;

        bool playedPresentation = false;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = statuses[i];

            if (status == null)
                continue;

            if (status.EffectId == "E_Poison")
            {
                BattleEffectUtility.PoisonDamagePlayer(character, status.Stack);
                playedPresentation = true;

                if (character.RuntimeData.IsDead)
                    return playedPresentation;
            }

            if (status.EffectId == "E_Heal")
            {
                BattleEffectUtility.HealPlayer(character, Mathf.Max(0, status.Stack));
                playedPresentation = true;
            }

            if (status.EffectId == "E_Focus")
            {
                CharacterMasterData masterData = DataManager.Instance?.CharacterDatabase?.Get(character.RuntimeData.CharacterId);
                int maxResource = masterData != null ? Mathf.Max(0, masterData.MaxResource) : int.MaxValue;
                int requestedAmount = Mathf.Max(0, status.Stack);
                int previousResource = Mathf.Max(0, character.RuntimeData.CurrentResource);
                int finalAmount = BattleEquipmentEffectService.ModifyUniqueResourceGain(
                    character.RuntimeData, requestedAmount);

                BattleEquipmentEffectService.ApplyUniqueResourceGainSideEffects(
                    character.RuntimeData,
                    finalAmount,
                    previousResource,
                    maxResource);

                character.RuntimeData.CurrentResource = Mathf.Min(
                    maxResource,
                    previousResource + finalAmount
                );
            }

            if (status.EffectId == "E_Charge")
            {
                character.RuntimeData.CurrentCost = Mathf.Min(
                    character.RuntimeData.MaxCost,
                    character.RuntimeData.CurrentCost + Mathf.Max(0, status.Stack)
                );
            }

            ApplyEndTurnRule(statuses, i, status);
        }

        return playedPresentation;
    }

    private bool ApplyMonsterTurnEndStatusEffects(MonsterUnit monster)
    {
        List<StatusEffectRuntimeData> statuses = monster.RuntimeData.StatusEffects;

        if (statuses == null)
            return false;

        bool playedPresentation = false;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = statuses[i];

            if (status == null)
                continue;

            if (status.EffectId == "E_Poison")
            {
                BattleEffectUtility.PoisonDamageMonster(monster, status.Stack);
                playedPresentation = true;

                if (monster.RuntimeData.IsDead)
                {
                    deathService.HandleMonsterDead(monster);
                    continue;
                }
            }

            if (status.EffectId == "E_Heal")
            {
                BattleEffectUtility.HealMonster(monster, Mathf.Max(0, status.Stack));
                playedPresentation = true;
            }

            if (status.EffectId == ExplodeEffectId)
            {
                status.Stack = Mathf.Max(0, status.Stack - 1);
                playedPresentation = true;

                if (status.Stack <= 0)
                {
                    statuses.RemoveAt(i);

                    if (!monster.RuntimeData.IsDead)
                        monster.RuntimeData.IsExplodeReady = true;
                }

                monster.ShowAndRefreshHUD();
                continue;
            }

            ApplyEndTurnRule(statuses, i, status);
        }

        return playedPresentation;
    }

    private void ApplyEndTurnRule(
        List<StatusEffectRuntimeData> statuses,
        int index,
        StatusEffectRuntimeData status)
    {
        if (statuses == null || status == null)
            return;

        if (status.EffectId == SpiderWebEffectId)
        {
            status.TurnCount = Mathf.Max(0, status.TurnCount - 1);

            if (status.TurnCount <= 0)
                statuses.RemoveAt(index);

            return;
        }

        EffectMasterData effectData = null;

        if (DataManager.Instance != null &&
            DataManager.Instance.EffectDatabase != null)
        {
            DataManager.Instance.EffectDatabase.TryGet(
                status.EffectId,
                out effectData
            );
        }

        if (effectData == null)
            return;

        switch (effectData.EndTurn)
        {
            case EndTurn.None:
                break;

            case EndTurn.Remove:
                statuses.RemoveAt(index);
                break;

            case EndTurn.Decrease:
                status.Stack--;

                if (status.Stack <= 0)
                    statuses.RemoveAt(index);

                break;

            case EndTurn.Maintain:
            case EndTurn.DecreaseOnTrigger:
                break;
        }
    }
}
