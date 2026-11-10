using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using Relic.Gameplay.Battle;
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

        yield return PlayArabellaWebCreationRoutine(monsters);
        yield return PlayNocturnPortalPresentationsRoutine(monsters);

        // 무료 턴 시작 연출이 모두 종료된 다음 실제 첫 예약 행동 방향으로 정렬합니다.
        // 슬롯 1이 비어 있고 슬롯 2부터 이동하는 경우에도 올바르게 적용됩니다.
        BattleMonsterTurnPlanner finalPlanner = Object.FindFirstObjectByType<BattleMonsterTurnPlanner>(
            FindObjectsInactive.Include);
        if (finalPlanner != null)
        {
            foreach (MonsterUnit monster in monsters)
            {
                if (monster != null && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
                {
                    finalPlanner.ApplyFirstReservedActionFacing(monster);
                }
            }
        }
    }

    private const string ArabellaWebTraitId = "E_WebCreation";
    private const string ArabellaWebGridEffectId = "GR_spider_web";
    private const float ArabellaWebPresentationDelay = 0.65f;
    private const int ArabellaWebActionIndex = 4;

    private static IEnumerator PlayArabellaWebCreationRoutine(MonsterUnit[] monsters)
    {
        if (monsters == null)
            yield break;

        BattleGridEffectController gridEffects = Object.FindFirstObjectByType<BattleGridEffectController>(
            FindObjectsInactive.Include);
        if (gridEffects == null)
            yield break;

        foreach (MonsterUnit monster in monsters)
        {
            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            string id = monster.RuntimeData.MonsterId;
            if (id != ArabellaPhaseTransition.PhaseOneMonsterId &&
                id != ArabellaPhaseTransition.PhaseTwoMonsterId)
                continue;

            // 사냥실은 대상 캐릭터에서 아라벨라 쪽으로 접근하는 경로의 인접 칸을 우선합니다.
            GridManager grid = Object.FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);
            if (grid == null)
                continue;

            BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<BattleCharacter> candidates = new List<BattleCharacter>();
            List<List<int>> availableCells = new List<List<int>>();
            int bossGridIndex = monster.MainGridIndex;
            if (bossGridIndex < 0)
                continue;
            Vector2Int bossCoord = grid.IndexToCoord(bossGridIndex);
            foreach (BattleCharacter character in characters)
            {
                if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                    continue;

                int centerIndex = character.CurrentGridIndex;
                if (centerIndex < 0)
                    continue;

                Vector2Int center = grid.IndexToCoord(centerIndex);
                List<int> neighboringCells = new List<int>();
                int bestDistance = int.MaxValue;
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        Vector2Int coord = center + new Vector2Int(x, y);
                        if (!grid.IsValidCoord(coord))
                            continue;

                        int cell = grid.CoordToIndex(coord);
                        if (gridEffects.HasEffect(cell) ||
                            BattleOccupancyService.IsOccupiedByMonster(cell) ||
                            BattleOccupancyService.TryGetCharacterAtGrid(cell, out _))
                            continue;

                        // 접근 방향에 있는 한 칸을 우선하고, 같은 거리인 칸만 후보로 둡니다.
                        // 안전한 앞쪽 칸이 없다면 주변 빈 칸 중 가장 가까운 칸을 선택합니다.
                        int distance = Mathf.Abs(coord.x - bossCoord.x) +
                                       Mathf.Abs(coord.y - bossCoord.y);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            neighboringCells.Clear();
                        }
                        if (distance == bestDistance)
                            neighboringCells.Add(cell);
                    }
                }

                if (neighboringCells.Count == 0)
                    continue;

                candidates.Add(character);
                availableCells.Add(neighboringCells);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("[ArabellaWeb] No available neighboring grid for GR_spider_web.");
                continue;
            }

            int targetIndex = UnityEngine.Random.Range(0, candidates.Count);
            BattleCharacter targetCharacter = candidates[targetIndex];
            List<int> targetCells = availableCells[targetIndex];
            int chosen = targetCells[UnityEngine.Random.Range(0, targetCells.Count)];
            BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();
            if (facing == null)
                facing = monster.GetComponentInChildren<BattleUnitFacing>();
            if (facing != null)
                facing.FaceByWorldTarget(targetCharacter.transform.position);

            BattleUnitAnimator animator = monster.GetComponent<BattleUnitAnimator>();
            if (animator != null)
            {
                animator.PlayMonsterActionPresentation(ArabellaWebActionIndex);
                yield return new WaitForSeconds(ArabellaWebPresentationDelay);
                if (monster != null && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
                    animator.PlayIdle();
            }

            // Idle 애니메이터가 스프라이트를 갱신할 프레임을 지난 뒤 방향을 적용합니다.
            yield return null;

            // Restore the first reserved action facing after the web presentation.
            // The web may face left even when the first reserved move goes right.
            if (monster != null && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
            {
                BattleMonsterTurnPlanner reservedPlanner = Object.FindFirstObjectByType<BattleMonsterTurnPlanner>(
                    FindObjectsInactive.Include);
                if (reservedPlanner != null)
                {
                    // 거미줄 설치는 독립 연출이므로, 완료 시점에 예약된 첫 이동 방향을 다시 적용합니다.
                    // 공용 판별이 종료되더라도 실제 예약 이동 방향을 먼저 적용합니다.
                    reservedPlanner.ApplyFirstReservedActionFacing(monster);
                }
            }

            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            // 애니메이션 중 위치가 바뀌어도 선정한 접근 경로의 인접 칸에 배치합니다.
            if (gridEffects.HasEffect(chosen) ||
                BattleOccupancyService.IsOccupiedByMonster(chosen) ||
                BattleOccupancyService.TryGetCharacterAtGrid(chosen, out _))
                continue;

            if (!gridEffects.TryPlaceEffect(chosen, ArabellaWebGridEffectId))
            {
                Debug.LogWarning($"[ArabellaWeb] Failed to place {ArabellaWebGridEffectId} at grid {chosen}. Check GridEffectDatabase / GridEffectPrefabDatabase and existing grid effects.");
                continue;
            }

            if (BattleOccupancyService.TryGetCharacterAtGrid(chosen, out BattleCharacter affected))
                gridEffects.ApplyToPlayer(chosen, affected);

            // 예약 입력이 열리기 직전 보이는 방향도 실제 첫 이동 명령으로 고정합니다.
            if (monster != null && monster.RuntimeData != null && !monster.RuntimeData.IsDead)
            {
                BattleMonsterTurnPlanner planner = Object.FindFirstObjectByType<BattleMonsterTurnPlanner>(
                    FindObjectsInactive.Include);
                if (planner != null)
                {
                    planner.ApplyFirstReservedActionFacing(monster);
                }
            }
        }
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

        int bleedStack = ConsumeBleedMoveStep(character.RuntimeData.StatusEffects);

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

        int bleedStack = ConsumeBleedMoveStep(monster.RuntimeData.StatusEffects);

        if (bleedStack <= 0)
            return;

        BattleEffectUtility.StatusDamageMonster(monster, bleedStack);

        if (monster.RuntimeData.IsDead)
            deathService.HandleMonsterDead(monster);
    }

    public static int ConsumeBleedMoveStep(List<StatusEffectRuntimeData> statusEffects)
    {
        if (statusEffects == null)
            return 0;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = statusEffects[i];
            if (status == null || status.EffectId != "E_Bleed" || status.Stack <= 0)
                continue;

            int damage = status.Stack;
            status.RemoveStack(1);
            if (status.Stack <= 0)
                statusEffects.RemoveAt(i);
            return damage;
        }

        return 0;
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
