using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class MonsterSkillEffectService
{
    private readonly BattleEffectExecutor effectExecutor = new();
    private readonly BattleDamageService damageService;
    private readonly BattleDeathService deathService;
    private readonly BattleHUDService hudService;
    private readonly GridManager gridManager;

    public MonsterSkillEffectService(
        BattleDamageService damageService,
        BattleDeathService deathService,
        BattleHUDService hudService,
        GridManager gridManager)
    {
        this.damageService = damageService;
        this.deathService = deathService;
        this.hudService = hudService;
        this.gridManager = gridManager;
    }

    public void ApplyMonsterSkill(MonsterUnit caster, MonsterReservedCommand command)
    {
        if (caster == null || command == null || command.SkillData == null)
            return;

        if (command.SkillData.Target == TargetType.PlayerParty)
            ApplyToPlayers(caster, command);
        else if (command.SkillData.Target == TargetType.EnemyParty)
            ApplyToMonsters(caster, command);
        else if (command.SkillData.Target == TargetType.Self)
            ApplyToSelf(caster, command);

        hudService.RefreshHUDs();
    }

    private void ApplyToPlayers(MonsterUnit caster, MonsterReservedCommand command)
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter target = characters[i];

            if (target == null || target.RuntimeData == null)
                continue;

            if (!command.TargetGridIndices.Contains(target.CurrentGridIndex))
                continue;

            ExecuteEffects(caster, target, null, command);
        }
    }

    private void ApplyToMonsters(MonsterUnit caster, MonsterReservedCommand command)
    {
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit target = monsters[i];

            if (target == null || target.RuntimeData == null)
                continue;

            bool inTargetRange = false;

            for (int j = 0; j < target.OccupiedGridIndices.Count; j++)
            {
                if (command.TargetGridIndices.Contains(target.OccupiedGridIndices[j]))
                {
                    inTargetRange = true;
                    break;
                }
            }

            if (!inTargetRange)
                continue;

            ExecuteEffects(caster, null, target, command);
        }
    }

    private void ApplyToSelf(MonsterUnit caster, MonsterReservedCommand command)
    {
        ExecuteEffects(caster, null, caster, command);
    }

    private void ExecuteEffects(
    MonsterUnit caster,
    BattleCharacter playerTarget,
    MonsterUnit monsterTarget,
    MonsterReservedCommand command)
    {
        if (caster == null || command == null || command.SkillData == null)
            return;

        if (playerTarget == null && monsterTarget == null)
        {
            Debug.LogWarning(
                $"[MonsterSkillEffect] Target 없음 / " +
                $"Skill:{command.SkillData.SkillId}"
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(command.SkillData.EffectIds))
            return;

        string[] effectIds = command.SkillData.EffectIds.Split(';');

        for (int i = 0; i < effectIds.Length; i++)
        {
            string effectId = effectIds[i].Trim();

            if (string.IsNullOrWhiteSpace(effectId))
                continue;

            int value = ParseIndexedValue(command.SkillData.ValueRate, i);
            int count = ParseIndexedValue(command.SkillData.CountRate, i);

            BattleUnitFacing facing = caster.GetComponent<BattleUnitFacing>();

            BattleDirection direction =
                facing != null && !facing.IsFacingRight
                    ? BattleDirection.Left
                    : BattleDirection.Right;

            BattleEffectContext context = new BattleEffectContext
            {
                MonsterCaster = caster,
                PlayerTarget = playerTarget,
                MonsterTarget = monsterTarget,
                MonsterSkillData = command.SkillData,

                Direction = direction,
                GridManager = gridManager,

                EffectId = effectId,
                Value = value,
                Count = count
            };

            try
            {
                effectExecutor.Execute(effectId, context);
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    $"[MonsterSkillEffect] Effect 실행 중 에러 / " +
                    $"Skill:{command.SkillData.SkillId} / Effect:{effectId}"
                );
                Debug.LogException(e);
            }
        }
    }

    private int ParseIndexedValue(string text, int index)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 1;

        string[] split = text.Split(';');

        if (index >= 0 && index < split.Length)
            return damageService.ParseFirstInt(split[index]);

        return damageService.ParseFirstInt(text);
    }
}