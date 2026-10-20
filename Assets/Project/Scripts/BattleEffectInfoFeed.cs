using System.Collections;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays each executed player/monster action under Effect_Info.
/// Attach this component to Effect_Info and assign the BattleEffectInfo prefab.
/// </summary>
public sealed class BattleEffectInfoFeed : MonoBehaviour
{
    [Header("Prefab / Parent")]
    [SerializeField] private GameObject battleEffectInfoPrefab;
    [SerializeField] private Transform contentRoot;

    [Header("Lifetime")]
    [SerializeField, Min(0.1f)] private float displaySeconds = 2f;
    [SerializeField, Min(1)] private int maxVisibleEntries = 12;

    [Header("Slide / Fade Animation")]
    [SerializeField, Min(0f)] private float enterDuration = 0.2f;
    [SerializeField, Min(0f)] private float exitDuration = 0.2f;
    [SerializeField, Min(0f)] private float enterOffsetY = 35f;
    [SerializeField, Min(0f)] private float exitOffsetY = 35f;

    [Header("Sequential Entry")]
    [SerializeField, Min(0f)] private float entryInterval = 0.15f;

    [Header("Fallback Icons")]
    [SerializeField] private Sprite moveIcon;
    [SerializeField] private Sprite flipIcon;

    [Header("Collision Icon")]
    [SerializeField] private Sprite collisionIcon;

    [Header("Special Line Colors")]
    [SerializeField] private Color collisionLineColor = new Color32(0xDF, 0x4D, 0x56, 0xFF);
    [SerializeField] private Color runeLineColor = new Color32(0x0A, 0x46, 0x9E, 0xFF);
    [SerializeField] private Color relicLineColor = new Color32(0x0A, 0x46, 0x9E, 0xFF);

    [Header("Turn Start Icon")]
    [SerializeField] private Sprite turnStartIcon;

    // Priority: rune (0), relic (1), other notifications (2).
    private readonly System.Collections.Generic.List<System.Tuple<string, Sprite, bool, bool, int>> queuedEntries =
        new System.Collections.Generic.List<System.Tuple<string, Sprite, bool, bool, int>>();
    private readonly System.Collections.Generic.Dictionary<System.Tuple<string, Sprite, bool, bool, int>, System.Collections.Generic.Queue<System.Action>> entryCallbacks = new();
    private Coroutine queueRoutine;
    private int activeEntryCount;
    private float nextEntryTime;
    [SerializeField, Min(0f)] private float equipmentBatchWindow = 0.2f;

    private static BattleEffectInfoFeed instance;
    private static bool deferInitialEffects;
    private static bool capturePassiveEffects;
    private static readonly System.Collections.Generic.List<System.Action> pendingInitialActions = new();
    private static readonly System.Collections.Generic.List<int> pendingInitialPriorities = new System.Collections.Generic.List<int>();
    private static readonly System.Collections.Generic.List<System.Tuple<string, Sprite, bool>> pendingInitialEffects =
        new System.Collections.Generic.List<System.Tuple<string, Sprite, bool>>();

    // Queue an initial passive without applying it until its visible info entry is created.
    public static void ScheduleInitialPassive(SkillMasterData skill, System.Action apply)
    {
        if (apply == null) return;
        BattleEffectInfoFeed feed = FindFeed();
        if (feed == null || skill == null)
        {
            apply();
            return;
        }
        Sprite icon = skill.Icon;
        DataManager manager = DataManager.Instance;
        if (icon == null && manager?.SkillIconDatabase != null)
            manager.SkillIconDatabase.TryGetIcon(skill.SkillId, out icon);
        string label = GameDataLocalization.SkillName(skill);
        if (deferInitialEffects)
        {
            pendingInitialEffects.Add(System.Tuple.Create(label, icon, false));
            pendingInitialPriorities.Add(2);
            pendingInitialActions.Add(apply);
        }
        else
            feed.SpawnWithCallback(label, icon, false, 2, apply);
    }

    private void SpawnWithCallback(string label, Sprite icon, bool enemy, int priority, System.Action callback)
    {
        if (battleEffectInfoPrefab == null)
        {
            callback?.Invoke();
            return;
        }
        var entry = System.Tuple.Create(label, icon, enemy, false, priority);
        if (!entryCallbacks.TryGetValue(entry, out var callbacks))
        {
            callbacks = new System.Collections.Generic.Queue<System.Action>();
            entryCallbacks.Add(entry, callbacks);
        }
        callbacks.Enqueue(callback);
        queuedEntries.Add(entry);
        if (queueRoutine == null && isActiveAndEnabled)
            queueRoutine = StartCoroutine(ProcessEntryQueue());
    }

    public static void BeginPassiveCapture() { capturePassiveEffects = true; }
    public static void EndPassiveCapture() { capturePassiveEffects = false; }

    // Use the originating skill rather than the status-effect icon/name.
    public static void ShowPassiveGain(SkillMasterData skill, int amount, bool allies, bool enemies, bool entireGroup)
    {
        if (!capturePassiveEffects || skill == null || amount <= 0) return;
        BattleEffectInfoFeed feed = FindFeed();
        if (feed == null) return;
        Sprite icon = skill.Icon;
        DataManager manager = DataManager.Instance;
        if (icon == null && manager != null && manager.SkillIconDatabase != null)
            manager.SkillIconDatabase.TryGetIcon(skill.SkillId, out icon);
        string name = GameDataLocalization.SkillName(skill);
        feed.Spawn(name, icon, enemies);
    }

    public static void ShowHealthRecovery(int amount, bool party = false)
    {
        if (amount <= 0) return;
        BattleEffectInfoFeed feed = FindFeed();
        if (feed != null)
            feed.Spawn(GameLocalization.Get("battle.effect_info.health_recovery", "체력 회복"), feed.healthRecoveryIcon);
    }

    public static void BeginInitialEffects()
    {
        shownEnemyRelicsByTurn.Clear();
        suppressedEquipmentStatuses.Clear();
        pendingInitialEffects.Clear();
        pendingInitialPriorities.Clear();
        pendingInitialActions.Clear();
        deferInitialEffects = true;
    }

    // Show initial passive/equipment effects after the battle-start intro, before reservation.
    public static IEnumerator FlushInitialEffectsAndWait()
    {
        deferInitialEffects = false;
        BattleEffectInfoFeed feed = FindFeed();
        int count = pendingInitialEffects.Count;
        if (feed != null)
        {
            // Preserve order within a category: rune activations first, then relics,
            // then other turn-start effects. Do not change their actual application order.
            for (int priority = 0; priority <= 3; priority++)
            {
                for (int i = 0; i < count; i++)
                {
                    if (pendingInitialPriorities[i] != priority) continue;
                    var entry = pendingInitialEffects[i];
                    System.Action callback = i < pendingInitialActions.Count ? pendingInitialActions[i] : null;
                    if (callback != null)
                        feed.SpawnWithCallback(entry.Item1, entry.Item2, entry.Item3, priority, callback);
                    else
                        feed.Spawn(entry.Item1, entry.Item2, entry.Item3, false, priority);
                }
            }
        }
        pendingInitialEffects.Clear();
        pendingInitialPriorities.Clear();
        pendingInitialActions.Clear();
        if (feed != null && count > 0)
        {
            // Wait for all queued introductions AND their exit animations.
            while (feed.queuedEntries.Count > 0 || feed.activeEntryCount > 0)
                yield return null;
        }
    }

    private void Awake()
    {
        instance = this;
        if (contentRoot == null) contentRoot = transform;
    }

    private void OnDisable()
    {
        queuedEntries.Clear();
        entryCallbacks.Clear();
        queueRoutine = null;
        activeEntryCount = 0;
        nextEntryTime = 0f;
    }

    private void OnDestroy()
    {
        queuedEntries.Clear();
        entryCallbacks.Clear();
        if (instance == this) instance = null;
    }

    private static BattleEffectInfoFeed FindFeed()
    {
        if (instance != null) return instance;
        BattleEffectInfoFeed[] feeds = Object.FindObjectsByType<BattleEffectInfoFeed>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < feeds.Length; i++)
        {
            if (feeds[i] != null && feeds[i].gameObject.scene.IsValid())
            {
                instance = feeds[i];
                return instance;
            }
        }
        return null;
    }

    public static void ShowPlayer(PlayerReservedCommand command)
    {
        if (command == null) return;
        BattleEffectInfoFeed feed = FindFeed();
        if (feed == null) return;

        // Facing flip is a separate action, even when its move index is populated.
        if (command.IsFacingFlipCommand)
        {
            Sprite flipActionIcon = feed.flipIcon;
            if (flipActionIcon == null && DataManager.Instance != null && DataManager.Instance.ActionTypeIconDatabase != null)
                DataManager.Instance.ActionTypeIconDatabase.TryGetIcon("Flip", out flipActionIcon);
            feed.Spawn(GameLocalization.Get("battle.action.flip.name", "전환"), flipActionIcon);
            return;
        }

        bool move = command.ReservedMoveGridIndex >= 0;
        if (move)
        {
            feed.Spawn(GameLocalization.Get("common.move", "이동"), feed.GetMoveIcon());
            return;
        }

        SkillMasterData skill = command.SkillData;
        if (skill == null) return;
        Sprite icon = skill.Icon;
        if (icon == null && DataManager.Instance != null &&
            DataManager.Instance.SkillIconDatabase != null)
            DataManager.Instance.SkillIconDatabase.TryGetIcon(skill.SkillId, out icon);

        string label = GameDataLocalization.SkillName(skill);

        feed.Spawn(label, icon);
    }

    public static void ShowMonster(MonsterReservedCommand command)
    {
        if (command == null || command.SkillData == null) return;
        BattleEffectInfoFeed feed = FindFeed();
        if (feed == null) return;

        MonsterSkillData skill = command.SkillData;
        bool move = skill.TimelineNotation == TimelineActionType.Move ||
                    string.Equals(skill.SkillType, "이동", System.StringComparison.OrdinalIgnoreCase);
        Sprite icon = null;
        DataManager dataManager = DataManager.Instance;
        if (dataManager != null && dataManager.MonsterSkillIconDatabase != null &&
            !string.IsNullOrWhiteSpace(skill.SkillIcon))
            dataManager.MonsterSkillIconDatabase.TryGetIcon(skill.SkillIcon, out icon);
        if (icon == null && dataManager != null && dataManager.ActionTypeIconDatabase != null)
            dataManager.ActionTypeIconDatabase.TryGetIcon(skill.TimelineNotation.ToString(), out icon);
        if (move && icon == null) icon = feed.GetMoveIcon();

        string label = move
            ? GameLocalization.Get("common.move", "이동")
            : GameDataLocalization.MonsterSkillName(skill);
        feed.Spawn(label, icon, true);
    }

    [Header("Recovery Icons")]
    [SerializeField] private Sprite manaRecoveryIcon;
    [SerializeField] private Sprite karmaRecoveryIcon;
    [SerializeField] private Sprite healthRecoveryIcon;

    public static void ShowTurnRecovery(bool mana, int amount, bool party)
    {
        if (amount <= 0) return;
        BattleEffectInfoFeed feed = FindFeed();
        if (feed == null) return;
        string label = mana
            ? GameLocalization.Get("battle.effect_info.mana_recovery", "마나 회복")
            : party
                ? GameLocalization.Get("battle.effect_info.party_karma_recovery", "파티 카르마 회복")
                : GameLocalization.Get("battle.effect_info.karma_recovery", "카르마 회복");
        feed.Spawn(label, mana ? feed.manaRecoveryIcon : feed.karmaRecoveryIcon);
    }

    // Automatic mana regeneration is intentionally excluded from the feed.
    public static void ShowPartyKarmaRecovery(int amount)
    {
        if (amount <= 0) return;
        BattleEffectInfoFeed feed = FindFeed();
        if (feed != null)
            feed.Spawn(
                GameLocalization.Get("battle.effect_info.party_karma_recovery", "파티 카르마 회복"),
                feed.karmaRecoveryIcon);
    }

    // Capture runtime stacks to avoid reporting passive refreshes as new gains.
    public static System.Collections.Generic.Dictionary<string, int> CaptureStatusSnapshot()
    {
        var snapshot = new System.Collections.Generic.Dictionary<string, int>();
        foreach (BattleCharacter character in Object.FindObjectsByType<BattleCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (character != null && character.RuntimeData != null)
                CaptureUnit(character.RuntimeData.CharacterId, character.RuntimeData.StatusEffects, snapshot);
        foreach (Relic.Gameplay.Monster.MonsterUnit monster in Object.FindObjectsByType<Relic.Gameplay.Monster.MonsterUnit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (monster != null && monster.RuntimeData != null)
                CaptureUnit(monster.RuntimeData.RuntimeId, monster.RuntimeData.StatusEffects, snapshot);
        return snapshot;
    }

    private static void CaptureUnit(string unitId, System.Collections.Generic.List<StatusEffectRuntimeData> effects,
        System.Collections.Generic.Dictionary<string, int> snapshot)
    {
        if (effects == null || string.IsNullOrEmpty(unitId)) return;
        foreach (StatusEffectRuntimeData effect in effects)
        {
            if (effect == null || effect.Stack <= 0 || string.IsNullOrEmpty(effect.EffectId)) continue;
            string key = unitId + "|" + effect.EffectId;
            snapshot.TryGetValue(key, out int prior);
            snapshot[key] = prior + effect.Stack;
        }
    }

    // Enemy-wide equipment status effects do not retain their equipment source on monsters.
    // Record their origin at the application site, before the status snapshot is reported.
    private static readonly System.Collections.Generic.HashSet<string> suppressedEquipmentStatuses =
        new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    private static readonly System.Collections.Generic.HashSet<string> shownEnemyRelicsByTurn =
        new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    public static void NotifyEnemyWideEquipmentStatus(string sourceId, string statusEffectId, int turnNumber)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(statusEffectId)) return;
        DataManager manager = DataManager.Instance;
        if (manager == null || manager.RelicDatabase == null ||
            !manager.RelicDatabase.TryGet(sourceId, out RelicData relic) || relic == null)
            return;

        // Do not show a separate vulnerable/weakness entry for every affected monster.
        suppressedEquipmentStatuses.Add(statusEffectId);
        string key = turnNumber.ToString() + "|" + relic.FragmentId;
        if (shownEnemyRelicsByTurn.Add(key)) ShowRelic(relic.FragmentId);
    }

    public static void ShowNewStatusEffects(System.Collections.Generic.Dictionary<string, int> before)
    {
        if (before == null) return;
        BattleEffectInfoFeed feed = FindFeed();
        if (feed == null) return;
        var after = CaptureStatusSnapshot();
        var shownRunes = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        var shownRelics = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var entry in after)
        {
            before.TryGetValue(entry.Key, out int oldValue);
            int gained = entry.Value - oldValue;
            if (gained <= 0) continue;
            int separator = entry.Key.LastIndexOf('|');
            if (separator < 0) continue;
            string effectId = entry.Key.Substring(separator + 1);
            if (IsEnemyStatusKey(entry.Key) && suppressedEquipmentStatuses.Contains(effectId))
                continue;
            string sourceRuneId = FindSourceRune(entry.Key, effectId);
            if (!string.IsNullOrWhiteSpace(sourceRuneId))
            {
                if (shownRunes.Add(sourceRuneId))
                    ShowRune(sourceRuneId, IsEnemyStatusKey(entry.Key));
                continue;
            }
            string sourceRelicId = FindSourceRelic(entry.Key, effectId);
            if (!string.IsNullOrWhiteSpace(sourceRelicId))
            {
                if (shownRelics.Add(sourceRelicId))
                    ShowRelic(sourceRelicId, IsEnemyStatusKey(entry.Key));
                continue;
            }
            string effectName = effectId;
            Sprite icon = null;
            DataManager manager = DataManager.Instance;
            if (manager != null)
            {
                if (manager.EffectDatabase != null &&
                    manager.EffectDatabase.TryGet(effectId, out EffectMasterData effectData) && effectData != null &&
                    !string.IsNullOrWhiteSpace(effectData.Name)) effectName = GameDataLocalization.EffectName(effectData);
                if (manager.StatusEffectIconDatabase != null)
                    manager.StatusEffectIconDatabase.TryGetIcon(effectId, out icon);
            }
            feed.Spawn(effectName, icon, IsEnemyStatusKey(entry.Key));
        }
        suppressedEquipmentStatuses.Clear();
    }

    // Resolve the equipment that created the status before displaying the status itself.
    // Some rune implementations store the rune ID in SourceSkillId, while others
    // store the individual equipment effect ID.
    private static string FindSourceRune(string key, string effectId)
    {
        DataManager manager = DataManager.Instance;
        if (manager == null || manager.RuneDatabase == null) return null;
        int separator = key.LastIndexOf('|');
        if (separator < 0) return null;
        string unitId = key.Substring(0, separator);
        foreach (BattleCharacter character in Object.FindObjectsByType<BattleCharacter>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (character == null || character.RuntimeData == null ||
                character.RuntimeData.CharacterId != unitId) continue;
            var runtime = character.RuntimeData;
            if (runtime.StatusEffects == null) return null;
            foreach (StatusEffectRuntimeData status in runtime.StatusEffects)
            {
                if (status == null || status.EffectId != effectId ||
                    string.IsNullOrWhiteSpace(status.SourceSkillId)) continue;
                if (manager.RuneDatabase.TryGet(status.SourceSkillId, out RuneData directRune) &&
                    directRune != null && IsEquipped(runtime.EquippedRuneIds, directRune.RuneId))
                    return directRune.RuneId;
                if (runtime.EquippedRuneIds == null) continue;
                foreach (string runeId in runtime.EquippedRuneIds)
                {
                    if (string.IsNullOrWhiteSpace(runeId) ||
                        !manager.RuneDatabase.TryGet(runeId, out RuneData rune) || rune == null) continue;
                    if (rune.EffectEntries != null)
                        foreach (SkillEffectEntry effect in rune.EffectEntries)
                            if (effect != null && string.Equals(effect.EffectId, status.SourceSkillId,
                                System.StringComparison.OrdinalIgnoreCase)) return rune.RuneId;
                    if (!string.IsNullOrWhiteSpace(rune.EffectIds))
                        foreach (string configuredEffectId in rune.EffectIds.Split(';'))
                            if (string.Equals(configuredEffectId.Trim(), status.SourceSkillId,
                                System.StringComparison.OrdinalIgnoreCase)) return rune.RuneId;
                }
            }
            return null;
        }
        return null;
    }

    private static bool IsEquipped(string[] runeIds, string runeId)
    {
        if (runeIds == null) return false;
        foreach (string id in runeIds)
            if (string.Equals(id, runeId, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public static void ShowRune(string runeId, bool enemy = false)
    {
        BattleEffectInfoFeed feed = FindFeed();
        DataManager manager = DataManager.Instance;
        if (feed == null || manager == null || manager.RuneDatabase == null ||
            string.IsNullOrWhiteSpace(runeId) ||
            !manager.RuneDatabase.TryGet(runeId, out RuneData rune) || rune == null) return;
        Sprite icon = null;
        if (manager.RuneIconDatabase != null)
            manager.RuneIconDatabase.TryGetIcon(rune.RuneId, out icon);
        feed.Spawn(GameDataLocalization.RuneName(rune), icon, enemy, false, 0);
    }

    // Identify the equipped relic only when the recorded source matches that relic.
    // Never attribute an unrelated status effect to an equipped relic by effect type alone.
    private static string FindSourceRelic(string key, string effectId)
    {
        DataManager manager = DataManager.Instance;
        if (manager == null || manager.RelicDatabase == null) return null;
        int separator = key.LastIndexOf('|');
        if (separator < 0) return null;
        string unitId = key.Substring(0, separator);
        foreach (BattleCharacter character in Object.FindObjectsByType<BattleCharacter>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (character == null || character.RuntimeData == null ||
                character.RuntimeData.CharacterId != unitId) continue;
            var runtime = character.RuntimeData;
            if (runtime.StatusEffects == null || runtime.EquippedRelicIds == null) return null;
            foreach (StatusEffectRuntimeData status in runtime.StatusEffects)
            {
                if (status == null || status.EffectId != effectId ||
                    string.IsNullOrWhiteSpace(status.SourceSkillId)) continue;
                foreach (string relicId in runtime.EquippedRelicIds)
                {
                    if (string.IsNullOrWhiteSpace(relicId) ||
                        !manager.RelicDatabase.TryGet(relicId, out RelicData relic) || relic == null) continue;
                    if (string.Equals(relic.FragmentId, status.SourceSkillId,
                        System.StringComparison.OrdinalIgnoreCase)) return relic.FragmentId;
                    if (relic.EffectEntries != null)
                        foreach (SkillEffectEntry effect in relic.EffectEntries)
                            if (effect != null && string.Equals(effect.EffectId, status.SourceSkillId,
                                System.StringComparison.OrdinalIgnoreCase)) return relic.FragmentId;
                    if (!string.IsNullOrWhiteSpace(relic.EffectIds))
                        foreach (string configuredId in relic.EffectIds.Split(';'))
                            if (string.Equals(configuredId.Trim(), status.SourceSkillId,
                                System.StringComparison.OrdinalIgnoreCase)) return relic.FragmentId;
                }
            }
            return null;
        }
        return null;
    }

    public static void ShowRelic(string relicId, bool enemy = false)
    {
        BattleEffectInfoFeed feed = FindFeed();
        DataManager manager = DataManager.Instance;
        if (feed == null || manager == null || manager.RelicDatabase == null ||
            string.IsNullOrWhiteSpace(relicId) ||
            !manager.RelicDatabase.TryGet(relicId, out RelicData relic) || relic == null) return;
        Sprite icon = null;
        if (manager.RelicIconDatabase != null)
            manager.RelicIconDatabase.TryGetIcon(relic.FragmentId, out icon);
        feed.Spawn(GameDataLocalization.RelicName(relic), icon, enemy, false, 1);
    }

    private static bool IsEnemyStatusKey(string key)
    {
        int separator = key.LastIndexOf('|');
        string unitId = separator >= 0 ? key.Substring(0, separator) : key;
        foreach (Relic.Gameplay.Monster.MonsterUnit monster in Object.FindObjectsByType<Relic.Gameplay.Monster.MonsterUnit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (monster != null && monster.RuntimeData != null && monster.RuntimeData.RuntimeId == unitId)
                return true;
        return false;
    }

    private Sprite GetMoveIcon()
    {
        if (moveIcon != null) return moveIcon;
        DataManager dataManager = DataManager.Instance;
        if (dataManager != null && dataManager.ActionTypeIconDatabase != null &&
            dataManager.ActionTypeIconDatabase.TryGetIcon("Move", out Sprite icon)) return icon;
        return null;
    }

    private static readonly Color AllyLineColor = new Color32(0x0A, 0x46, 0x9E, 0xFF);
    private static readonly Color EnemyLineColor = new Color32(0xDF, 0x4D, 0x56, 0xFF);

    /// <summary>행동 예약 시작을 프리팹 기본 Line 색상으로 표시합니다.</summary>
    /// <summary>강제 이동 중 실제 충돌이 발생한 경우 한 번 표시합니다.</summary>
    public static void ShowCollision(bool enemy)
    {
        BattleEffectInfoFeed feed = FindFeed();
        if (feed == null) return;
        feed.Spawn(GameLocalization.Get("battle.effect_info.collision", "충돌"), feed.collisionIcon, enemy, false, 3);
    }

    public static void ShowTurnStart(int turnNumber)
    {
        BattleEffectInfoFeed feed = FindFeed();
        if (feed == null || turnNumber < 1) return;
        // Turn headings must not be captured as an initial passive effect.
        bool wasDeferred = deferInitialEffects;
        deferInitialEffects = false;
        try
        {
            feed.Spawn(
                GameLocalization.FormatWithFallback(
                    "battle.effect_info.turn_start_format",
                    "{0}턴",
                    turnNumber),
                feed.turnStartIcon,
                false,
                true);
        }
        finally { deferInitialEffects = wasDeferred; }
    }

    private void Spawn(string label, Sprite icon, bool enemy = false, bool defaultLineColor = false, int initialPriority = 2)
    {
        if (deferInitialEffects)
        {
            pendingInitialEffects.Add(System.Tuple.Create(label, icon, enemy));
            pendingInitialPriorities.Add(initialPriority);
            pendingInitialActions.Add(null);
            return;
        }
        if (battleEffectInfoPrefab == null) return;
        queuedEntries.Add(System.Tuple.Create(label, icon, enemy, defaultLineColor, initialPriority));
        if (queueRoutine == null && isActiveAndEnabled)
            queueRoutine = StartCoroutine(ProcessEntryQueue());
    }

    private IEnumerator ProcessEntryQueue()
    {
        // Collect equipment notifications briefly so a rune generated in a later
        // callback does not appear behind a relic generated in the same burst.
        if (equipmentBatchWindow > 0f)
            yield return new WaitForSecondsRealtime(equipmentBatchWindow);

        while (queuedEntries.Count > 0)
        {
            while (Time.unscaledTime < nextEntryTime)
                yield return null;

            // Reorder only pending equipment alerts. Already displayed entries
            // cannot be reordered, and unrelated actions retain FIFO order.
            int index = 0;
            if (queuedEntries[0].Item5 <= 1)
            {
                for (int i = 1; i < queuedEntries.Count; i++)
                {
                    if (queuedEntries[i].Item5 > 1) break;
                    if (queuedEntries[i].Item5 < queuedEntries[index].Item5)
                        index = i;
                }
            }
            var entry = queuedEntries[index];
            queuedEntries.RemoveAt(index);
            CreateEntry(entry.Item1, entry.Item2, entry.Item3, entry.Item4, entry.Item5);
            if (entryCallbacks.TryGetValue(entry, out var callbacks) && callbacks.Count > 0)
            {
                System.Action onShown = callbacks.Dequeue();
                if (callbacks.Count == 0) entryCallbacks.Remove(entry);
                onShown?.Invoke();
            }
            nextEntryTime = Time.unscaledTime + entryInterval;
        }
        queueRoutine = null;
    }

    private void CreateEntry(string label, Sprite icon, bool enemy, bool defaultLineColor, int category)
    {
        Transform parent = contentRoot != null ? contentRoot : transform;
        GameObject item = Instantiate(battleEffectInfoPrefab, parent, false);
        item.SetActive(true);
        Transform nameTransform = FindChild(item.transform, "Name");
        if (nameTransform != null)
        {
            TMP_Text text = nameTransform.GetComponent<TMP_Text>();
            if (text != null) text.text = label;
            // This text is dynamic. A fixed localization key would overwrite it.
            Behaviour localized = nameTransform.GetComponent("LocalizedTMPText") as Behaviour;
            if (localized != null) localized.enabled = false;
        }
        Transform lineTransform = FindChild(item.transform, "Line");
        if (lineTransform != null)
        {
            Graphic lineGraphic = lineTransform.GetComponent<Graphic>();
            if (lineGraphic != null && !defaultLineColor)
                lineGraphic.color = category == 0 ? runeLineColor
                    : category == 1 ? relicLineColor
                    : category == 3 ? collisionLineColor
                    : enemy ? EnemyLineColor : AllyLineColor;
        }
        Transform iconTransform = FindChild(item.transform, "Icon");
        if (iconTransform != null)
        {
            Image image = iconTransform.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = icon;
                image.enabled = icon != null;
            }
        }
        while (parent.childCount > maxVisibleEntries)
            Destroy(parent.GetChild(0).gameObject);
        activeEntryCount++;
        StartCoroutine(AnimateLifetimeTracked(item));
    }

    private IEnumerator AnimateLifetimeTracked(GameObject item)
    {
        yield return AnimateLifetime(item);
        activeEntryCount = Mathf.Max(0, activeEntryCount - 1);
    }

    private IEnumerator AnimateLifetime(GameObject item)
    {
        if (item == null) yield break;
        RectTransform rect = item.transform as RectTransform;
        if (rect == null)
        {
            yield return new WaitForSecondsRealtime(displaySeconds);
            if (item != null) Destroy(item);
            yield break;
        }

        // A layout group controls the item position. Animate the visual children
        // instead so layout rebuilding cannot cancel the slide animation.
        RectTransform[] visuals = GetVisualChildren(rect);
        CanvasGroup group = item.GetComponent<CanvasGroup>();
        if (group == null) group = item.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        group.alpha = 0f;
        yield return null; // Allow UI layout to calculate its initial positions.
        if (item == null) yield break;
        Vector2[] bases = CapturePositions(visuals);
        yield return AnimateVisuals(item, group, visuals, bases, -enterOffsetY, 0f, 0f, 1f, enterDuration);
        if (item == null) yield break;

        yield return new WaitForSecondsRealtime(displaySeconds);
        if (item == null) yield break;
        yield return AnimateVisuals(item, group, visuals, bases, 0f, exitOffsetY, 1f, 0f, exitDuration);
        if (item != null) Destroy(item);
    }

    private static RectTransform[] GetVisualChildren(RectTransform root)
    {
        var children = new System.Collections.Generic.List<RectTransform>();
        foreach (Transform child in root)
            if (child is RectTransform rect) children.Add(rect);
        return children.ToArray();
    }

    private static Vector2[] CapturePositions(RectTransform[] visuals)
    {
        Vector2[] positions = new Vector2[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] != null) positions[i] = visuals[i].anchoredPosition;
        return positions;
    }

    private static IEnumerator AnimateVisuals(GameObject item, CanvasGroup group,
        RectTransform[] visuals, Vector2[] bases, float fromY, float toY,
        float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        while (item != null)
        {
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            float y = Mathf.Lerp(fromY, toY, eased);
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i] != null)
                    visuals[i].anchoredPosition = bases[i] + Vector2.up * y;
            if (group != null) group.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            if (t >= 1f) break;
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root.name == childName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChild(root.GetChild(i), childName);
            if (found != null) return found;
        }
        return null;
    }
}
