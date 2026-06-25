using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class MonsterSkillEffectService
{
    private enum EffectExecutionMode
    {
        All,
        DamageHit,
        NonDamage
    }

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
        ApplyMonsterSkillInternal(caster, command, EffectExecutionMode.All, -1);
    }

    public bool HasDamageHitEffect(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return false;

        if (string.IsNullOrWhiteSpace(command.SkillData.EffectIds))
            return false;

        string[] effectIds = command.SkillData.EffectIds.Split(';');

        for (int i = 0; i < effectIds.Length; i++)
        {
            if (IsDamageHitEffect(effectIds[i].Trim()))
                return true;
        }

        return false;
    }

    public int GetDamageHitCount(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return 1;

        if (string.IsNullOrWhiteSpace(command.SkillData.EffectIds))
            return 1;

        string[] effectIds = command.SkillData.EffectIds.Split(';');
        int hitCount = 1;

        for (int i = 0; i < effectIds.Length; i++)
        {
            string effectId = effectIds[i].Trim();

            if (!IsDamageHitEffect(effectId))
                continue;

            hitCount = Mathf.Max(
                hitCount,
                Mathf.Max(1, ParseIndexedValue(command.SkillData.CountRate, i))
            );
        }

        return hitCount;
    }

    public void ApplyMonsterSkillDamageHit(
        MonsterUnit caster,
        MonsterReservedCommand command,
        int hitIndex)
    {
        ApplyMonsterSkillInternal(caster, command, EffectExecutionMode.DamageHit, hitIndex);
    }

    public void ApplyMonsterSkillNonDamageEffects(
        MonsterUnit caster,
        MonsterReservedCommand command)
    {
        ApplyMonsterSkillInternal(caster, command, EffectExecutionMode.NonDamage, -1);
    }

    private void ApplyMonsterSkillInternal(
        MonsterUnit caster,
        MonsterReservedCommand command,
        EffectExecutionMode mode,
        int hitIndex)
    {
        if (caster == null || command == null || command.SkillData == null)
            return;

        if (command.SkillData.Target == TargetType.PlayerParty)
            ApplyToPlayers(caster, command, mode, hitIndex);
        else if (command.SkillData.Target == TargetType.EnemyParty)
            ApplyToMonsters(caster, command, mode, hitIndex);
        else if (command.SkillData.Target == TargetType.Self)
            ApplyToSelf(caster, command, mode, hitIndex);

        hudService.RefreshHUDs();
    }

    private void ApplyToPlayers(
        MonsterUnit caster,
        MonsterReservedCommand command,
        EffectExecutionMode mode,
        int hitIndex)
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

            if (target.RuntimeData.IsDead)
                continue;

            if (!command.TargetGridIndices.Contains(target.CurrentGridIndex))
                continue;

            ExecuteEffects(caster, target, null, command, mode, hitIndex);
        }
    }

    private void ApplyToMonsters(
        MonsterUnit caster,
        MonsterReservedCommand command,
        EffectExecutionMode mode,
        int hitIndex)
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

            ExecuteEffects(caster, null, target, command, mode, hitIndex);
        }
    }

    private void ApplyToSelf(
        MonsterUnit caster,
        MonsterReservedCommand command,
        EffectExecutionMode mode,
        int hitIndex)
    {
        ExecuteEffects(caster, null, caster, command, mode, hitIndex);
    }

    private void ExecuteEffects(
    MonsterUnit caster,
    BattleCharacter playerTarget,
    MonsterUnit monsterTarget,
    MonsterReservedCommand command,
    EffectExecutionMode mode,
    int hitIndex)
    {
        if (caster == null || command == null || command.SkillData == null)
            return;

        if (playerTarget != null &&
            (playerTarget.RuntimeData == null || playerTarget.RuntimeData.IsDead))
        {
            return;
        }

        if (monsterTarget != null &&
            (monsterTarget.RuntimeData == null || monsterTarget.RuntimeData.IsDead))
        {
            return;
        }

        if (playerTarget == null && monsterTarget == null)
        {
            Debug.LogWarning(
                $"[MonsterSkillEffect] Target missing / " +
                $"Skill:{command.SkillData.SkillId}"
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(command.SkillData.EffectIds))
            return;

        string[] effectIds = command.SkillData.EffectIds.Split(';');

        for (int i = 0; i < effectIds.Length; i++)
        {
            if (playerTarget != null &&
                (playerTarget.RuntimeData == null || playerTarget.RuntimeData.IsDead))
            {
                break;
            }

            string effectId = effectIds[i].Trim();

            if (string.IsNullOrWhiteSpace(effectId))
                continue;

            int count = ParseIndexedValue(command.SkillData.CountRate, i);
            bool isDamageHitEffect = IsDamageHitEffect(effectId);
            int value = ResolveEffectValue(command, effectId, i);

            if (mode == EffectExecutionMode.DamageHit && !isDamageHitEffect)
                continue;

            if (mode == EffectExecutionMode.NonDamage && isDamageHitEffect)
                continue;

            int hitCount = Mathf.Max(1, count);

            if (mode == EffectExecutionMode.DamageHit && hitIndex >= hitCount)
                continue;

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
                Count = mode == EffectExecutionMode.DamageHit ? 1 : count
            };

            try
            {
                if (mode == EffectExecutionMode.All && isDamageHitEffect)
                {
                    for (int hit = 0; hit < hitCount; hit++)
                    {
                        if (playerTarget != null &&
                            (playerTarget.RuntimeData == null || playerTarget.RuntimeData.IsDead))
                        {
                            break;
                        }

                        context.Value = ResolveEffectValue(command, effectId, i);
                        context.Count = 1;
                        effectExecutor.Execute(effectId, context);
                    }
                }
                else
                {
                    effectExecutor.Execute(effectId, context);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    $"[MonsterSkillEffect] Effect execution error / " +
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

    private int ResolveEffectValue(MonsterReservedCommand command, string effectId, int index)
    {
        if (IsDamageHitEffect(effectId))
        {
            if (damageService != null)
                return damageService.GetMonsterDamage(command);

            int reservedDamage = command != null
                ? command.EnsureReservedDamage()
                : 0;

            if (reservedDamage > 0)
                return reservedDamage;

            if (BattleDamageService.TryGetMonsterDamageRange(
                    command?.SkillData,
                    out int minDamage,
                    out int maxDamage))
            {
                return Random.Range(minDamage, maxDamage + 1);
            }

            return 1;
        }

        return ParseIndexedValue(command.SkillData.ValueRate, index);
    }

    private bool IsDamageHitEffect(string effectId)
    {
        return BattleDamageService.IsMonsterDamageHitEffect(effectId);
    }
}
