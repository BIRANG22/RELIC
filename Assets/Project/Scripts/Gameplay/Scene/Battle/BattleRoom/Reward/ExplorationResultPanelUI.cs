using System.Text;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ExplorationResultPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text stageResultText;
    [SerializeField] private Image stagePreviewImage;
    [SerializeField] private TMP_Text redDustiumText;
    [SerializeField] private TMP_Text normalBattleCountText;
    [SerializeField] private TMP_Text eliteBattleCountText;
    [SerializeField] private TMP_Text eventCountText;
    [SerializeField] private ExplorationResultCharacterRowUI[] characterRows;
    [SerializeField] private Button returnToBaseButton;
    [SerializeField] private Canvas sortingCanvas;
    [SerializeField] private int resultPanelSortingOrder = 25000;
    [SerializeField] private List<StagePresentationEntry> stagePresentations = new();

    private ExplorationResultData result;
    private bool isTransitioning;
    private bool isDefeatResult;
    private bool stageClearExperienceApplied;
    private float researchRewardMultiplier = 1f;
    private BattleStageClearExperienceContext experienceContext = BattleStageClearExperienceContext.Empty;

    private void Awake()
    {
        AutoBindSceneReferences();

        if (returnToBaseButton != null)
            returnToBaseButton.onClick.AddListener(ReturnToBase);
    }

    public void Open()
    {
        OpenInternal(false, 1f);
    }

    public void OpenDefeat(float rewardMultiplier = 0.5f)
    {
        OpenInternal(true, rewardMultiplier);
    }

    private void OpenInternal(bool defeat, float rewardMultiplier)
    {
        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.Get();
        MapRuntimeData mapRuntime = DataManager.Instance?.MapRuntimeStore?.Get();
        GeneratedMapNodeData currentNode = MapRuntimeProgressUtility.FindCurrentNode(mapRuntime);

        result = ExplorationResultBuilder.Build(runtime);
        isDefeatResult = defeat;
        experienceContext = defeat
            ? BattleStageClearExperienceContext.Empty
            : BattleStageClearExperienceService.BuildContext(mapRuntime, currentNode, defeat);
        researchRewardMultiplier = Mathf.Max(0f, rewardMultiplier);
        isTransitioning = false;
        stageClearExperienceApplied = false;

        if (returnToBaseButton != null)
            returnToBaseButton.interactable = true;

        if (HasStructuredBindings())
            ApplyStructuredResult(
                result,
                isDefeatResult,
                researchRewardMultiplier,
                currentNode,
                experienceContext);
        else if (resultText != null)
            resultText.text = BuildText(result, isDefeatResult, researchRewardMultiplier);

        gameObject.SetActive(true);
        BringResultPanelToFront();
    }

    private void ApplyStructuredResult(
        ExplorationResultData value,
        bool defeat,
        float rewardMultiplier,
        GeneratedMapNodeData currentNode,
        BattleStageClearExperienceContext clearContext)
    {
        if (resultText != null)
            resultText.gameObject.SetActive(false);

        MapData currentMap = ResolveCurrentMap(currentNode);
        StagePresentationEntry stagePresentation =
            ResolveStagePresentation(currentMap);

        SetText(titleText, defeat ? "탐사 실패" : "탐사 보고");
        SetText(stageNameText, ResolveStageName(currentNode, currentMap, stagePresentation));
        SetText(stageResultText, defeat ? "철수" : "클리어");
        SetText(redDustiumText, $"+ {Mathf.FloorToInt(value.Remnant * rewardMultiplier)}");

        BattleNodeSummary summary = CountClearedNodeSummary(clearContext);
        SetText(normalBattleCountText, summary.NormalBattleCount.ToString());
        SetText(eliteBattleCountText, summary.EliteBattleCount.ToString());
        SetText(eventCountText, summary.EventCount.ToString());

        ApplyStagePreviewImage(stagePresentation?.PreviewSprite);

        ApplyCharacterRows(
            value,
            clearContext,
            !defeat);
    }

    private void ApplyCharacterRows(
        ExplorationResultData value,
        BattleStageClearExperienceContext clearContext,
        bool awardExperience)
    {
        if (characterRows == null)
            return;

        IReadOnlyDictionary<string, BattleStageClearExperiencePreview> experiencePreviews =
            awardExperience
                ? BattleStageClearExperienceService.Preview(
                    DataManager.Instance?.CharacterRuntimeStore,
                    value?.CharacterStatistics,
                    clearContext)
                : null;

        int statsCount = value?.CharacterStatistics?.Count ?? 0;
        for (int i = 0; i < characterRows.Length; i++)
        {
            ExplorationResultCharacterRowUI row = characterRows[i];
            if (row == null)
                continue;

            if (i >= statsCount)
            {
                row.Clear();
                continue;
            }

            BattleRunCharacterStatisticsData statistics = value.CharacterStatistics[i];
            int gainedExperience = 0;
            bool leveledUp = false;
            float experienceProgress = 0f;

            if (TryGetExperiencePreview(
                    experiencePreviews,
                    statistics?.CharacterId,
                    out BattleStageClearExperiencePreview preview))
            {
                gainedExperience = preview.ExperienceGained;
                leveledUp = preview.LeveledUp;
                experienceProgress = preview.ProgressAfter01;
            }

            row.Bind(
                statistics,
                ResolveCharacterResultImage(statistics?.CharacterId),
                gainedExperience,
                leveledUp,
                experienceProgress);
        }
    }

    private bool HasStructuredBindings()
    {
        AutoBindSceneReferences();

        return titleText != null &&
               stageNameText != null &&
               redDustiumText != null &&
               characterRows != null &&
               characterRows.Length > 0;
    }

    private void AutoBindSceneReferences()
    {
        titleText ??= FindText("ExplorationReportTitle");
        stageNameText ??= FindText("ExplorationReportStageName");
        stageResultText ??= FindText("ExplorationReportStageResult");
        stagePreviewImage ??= FindImage("ExplorationReportStagePreview");
        redDustiumText ??= FindText("ExplorationReportRedDustium");
        normalBattleCountText ??= FindText("ExplorationReportNormalBattleCount");
        eliteBattleCountText ??= FindText("ExplorationReportEliteBattleCount");
        eventCountText ??= FindText("ExplorationReportEventCount");

        if (characterRows == null || characterRows.Length == 0)
            characterRows = GetComponentsInChildren<ExplorationResultCharacterRowUI>(true);

        sortingCanvas ??= GetComponent<Canvas>();
    }

    private TMP_Text FindText(string objectName)
    {
        Transform target = FindChildRecursive(transform, objectName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private Image FindImage(string objectName)
    {
        Transform target = FindChildRecursive(transform, objectName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == objectName)
                return child;

            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static MapData ResolveCurrentMap(GeneratedMapNodeData currentNode)
    {
        if (currentNode == null ||
            string.IsNullOrWhiteSpace(currentNode.MapId) ||
            DataManager.Instance?.MapDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.MapDatabase.TryGet(currentNode.MapId, out MapData map)
            ? map
            : null;
    }

    private static string ResolveStageName(
        GeneratedMapNodeData currentNode,
        MapData currentMap)
    {
        if (currentMap != null && !string.IsNullOrWhiteSpace(currentMap.Name))
            return currentMap.Name;

        if (currentMap != null && !string.IsNullOrWhiteSpace(currentMap.Stage))
            return currentMap.Stage;

        if (currentNode != null && !string.IsNullOrWhiteSpace(currentNode.MapId))
            return currentNode.MapId;

        return "탐사 기록";
    }

    private static string ResolveStageName(
        GeneratedMapNodeData currentNode,
        MapData currentMap,
        StagePresentationEntry stagePresentation)
    {
        if (stagePresentation != null && !string.IsNullOrWhiteSpace(stagePresentation.DisplayName))
            return stagePresentation.DisplayName.Trim();

        return ResolveStageName(currentNode, currentMap);
    }

    private StagePresentationEntry ResolveStagePresentation(MapData currentMap)
    {
        if (stagePresentations == null || stagePresentations.Count == 0)
            return null;

        string stage = NormalizeId(currentMap?.Stage);
        if (string.IsNullOrEmpty(stage))
            return null;

        for (int i = 0; i < stagePresentations.Count; i++)
        {
            StagePresentationEntry entry = stagePresentations[i];
            if (entry == null)
                continue;

            string entryStage = NormalizeId(entry.Stage);
            if (!string.IsNullOrEmpty(entryStage) &&
                string.Equals(entryStage, stage, System.StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private void ApplyStagePreviewImage(Sprite previewSprite)
    {
        if (stagePreviewImage == null)
            return;

        stagePreviewImage.sprite = previewSprite;
        stagePreviewImage.enabled = previewSprite != null;
        stagePreviewImage.preserveAspect = true;
    }

    private static Sprite ResolveCharacterResultImage(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId) || DataManager.Instance == null)
            return null;

        string id = characterId.Trim();
        if (DataManager.Instance.CharacterIconDatabase != null)
        {
            if (DataManager.Instance.CharacterIconDatabase.TryGetSideImage(id, out Sprite sideImage))
                return sideImage;

            if (DataManager.Instance.CharacterIconDatabase.TryGetPortrait(id, out Sprite portrait))
                return portrait;
        }

        if (DataManager.Instance.CharacterDatabase != null &&
            DataManager.Instance.CharacterDatabase.TryGet(id, out CharacterMasterData character))
        {
            return character.Icon;
        }

        return null;
    }

    private static bool TryGetExperiencePreview(
        IReadOnlyDictionary<string, BattleStageClearExperiencePreview> previews,
        string characterId,
        out BattleStageClearExperiencePreview preview)
    {
        preview = default;
        if (previews == null || string.IsNullOrWhiteSpace(characterId))
            return false;

        return previews.TryGetValue(characterId.Trim(), out preview);
    }

    private static BattleNodeSummary CountClearedNodeSummary(
        BattleStageClearExperienceContext context)
    {
        return new BattleNodeSummary
        {
            NormalBattleCount = context.NormalBattleClearCount,
            EliteBattleCount = context.EliteBattleClearCount,
            EventCount = context.EventClearCount
        };
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private string BuildText(ExplorationResultData value, bool defeat, float rewardMultiplier)
    {
        StringBuilder text = new();
        if (defeat)
        {
            text.AppendLine("\uD328\uBC30");
            text.AppendLine($"\uBCF4\uC0C1 {Mathf.FloorToInt(Mathf.Max(0f, rewardMultiplier) * 100f)}%");
            text.AppendLine();
        }

        text.AppendLine("━━━━━━━━━━━━━━━━━");
        text.AppendLine("탐사 결과");
        text.AppendLine("━━━━━━━━━━━━━━━━━\n");
        text.AppendLine($"레드 더스티움\n{value.Remnant}\n");
        text.AppendLine("유물");
        AppendRelics(text, value);
        text.AppendLine("\n전투 기술 데이터");
        AppendSkills(text, value);
        text.AppendLine("\n캐릭터 전투 통계");
        AppendStatistics(text, value);
        return text.ToString();
    }

    private static void AppendRelics(StringBuilder text, ExplorationResultData value)
    {
        if (value.RelicIds.Count == 0) { text.AppendLine("없음"); return; }
        for (int i = 0; i < value.RelicIds.Count; i++)
        {
            string id = value.RelicIds[i];
            string name = id;
            string rarity = "";
            if (DataManager.Instance?.RelicDatabase != null &&
                DataManager.Instance.RelicDatabase.TryGet(id, out RelicData relic))
            {
                name = string.IsNullOrWhiteSpace(relic.Name) ? id : relic.Name;
                rarity = relic.Rarity;
            }
            text.AppendLine($"• {name} {rarity}");
        }
    }

    private static void AppendSkills(StringBuilder text, ExplorationResultData value)
    {
        if (value.NewSkillIds.Count == 0) { text.AppendLine("없음"); return; }
        for (int i = 0; i < value.NewSkillIds.Count; i++)
        {
            string id = value.NewSkillIds[i];
            string name = id;
            string rarity = "";
            if (DataManager.Instance?.SkillDatabase != null &&
                DataManager.Instance.SkillDatabase.TryGet(id, out SkillMasterData skill))
            {
                name = string.IsNullOrWhiteSpace(skill.Name) ? id : skill.Name;
                rarity = SkillRarityUtility.GetDisplayName(skill.Rarity);
            }
            text.AppendLine($"• {name} {rarity}");
        }
    }

    private static void AppendStatistics(StringBuilder text, ExplorationResultData value)
    {
        if (value.CharacterStatistics.Count == 0) { text.AppendLine("기록 없음"); return; }
        for (int i = 0; i < value.CharacterStatistics.Count; i++)
        {
            BattleRunCharacterStatisticsData stats = value.CharacterStatistics[i];
            string name = stats.CharacterId;
            if (DataManager.Instance?.CharacterDatabase != null &&
                DataManager.Instance.CharacterDatabase.TryGet(stats.CharacterId, out CharacterMasterData character) &&
                !string.IsNullOrWhiteSpace(character.Name))
            {
                name = character.Name;
            }
            text.AppendLine($"[{name}]");
            text.AppendLine($"입힌 피해 {stats.DamageDealt} / 받은 피해 {stats.DamageTaken}");
            text.AppendLine($"사망 {stats.DeathCount} / 처치 {stats.KillCount} / 버프 {stats.BuffApplied}");
        }
    }

    private async void ReturnToBase()
    {
        if (isTransitioning || result == null || DataManager.Instance == null || GameManager.Instance == null)
            return;

        isTransitioning = true;
        if (returnToBaseButton != null)
            returnToBaseButton.interactable = false;

        ApplyStageClearExperienceOnce();

        LobbyRuntimeData lobby = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        lobby.PendingResearchResult = ExplorationResearchService.CreatePending(
            result,
            DataManager.Instance,
            researchRewardMultiplier);
        lobby.HasPendingResearchResult = true;
        lobby.OwnedRelicIds.Clear();
        ClearLobbyEquippedRelics(lobby);
        DataManager.Instance.LobbyRuntimeStore.Set(lobby);

        BattleRunAbandonService.AbandonCurrentRun(DataManager.Instance);
        SaveSystem.Instance?.SaveCurrentProgress();
        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
    }

    private void ApplyStageClearExperienceOnce()
    {
        if (stageClearExperienceApplied ||
            isDefeatResult ||
            result?.CharacterStatistics == null ||
            DataManager.Instance?.CharacterRuntimeStore == null)
        {
            return;
        }

        stageClearExperienceApplied = true;
        BattleStageClearExperienceService.Apply(
            DataManager.Instance.CharacterRuntimeStore,
            result.CharacterStatistics,
            experienceContext);
    }

    private void BringResultPanelToFront()
    {
        transform.SetAsLastSibling();

        sortingCanvas ??= GetComponent<Canvas>();
        if (sortingCanvas == null)
            return;

        Canvas parentCanvas = transform.parent != null
            ? transform.parent.GetComponentInParent<Canvas>()
            : null;

        if (parentCanvas != null && parentCanvas != sortingCanvas)
            sortingCanvas.sortingLayerID = parentCanvas.sortingLayerID;

        sortingCanvas.overrideSorting = true;
        sortingCanvas.sortingOrder = Mathf.Max(0, resultPanelSortingOrder);
    }

    private static void ClearLobbyEquippedRelics(LobbyRuntimeData lobby)
    {
        if (lobby.CharacterLoadouts == null)
            return;
        for (int i = 0; i < lobby.CharacterLoadouts.Count; i++)
            lobby.CharacterLoadouts[i].EquippedRelicIds = new string[7];
    }

    private struct BattleNodeSummary
    {
        public int NormalBattleCount;
        public int EliteBattleCount;
        public int EventCount;
    }

    [System.Serializable]
    private sealed class StagePresentationEntry
    {
        [SerializeField] private string stage;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite previewSprite;

        public string Stage => stage;
        public string DisplayName => displayName;
        public Sprite PreviewSprite => previewSprite;
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
