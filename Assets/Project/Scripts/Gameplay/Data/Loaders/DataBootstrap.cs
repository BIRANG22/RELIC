using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// [Loaders] 스크립트. 역할/설정/변수 용도를 코드 주석으로 확인할 수 있도록 정리했습니다.
/// Unity 연결: MonoBehaviour 스크립트는 Scene/GameObject에 컴포넌트로 부착 후 Inspector 필드를 설정하세요.
/// 데이터 클래스는 엑셀 시트 컬럼과 필드명을 맞춰 DataBootstrap 로딩 파이프라인에서 자동 매핑됩니다.
/// </summary>
namespace Relic.Gameplay.Data
{
    /// <summary>
    /// DataBootstrap의 책임을 담당하는 클래스입니다. 파일 상단 주석의 연결/설정 지침을 참고하세요.
    /// </summary>
    public class DataBootstrap
    {
        private TextAsset excelWorkbook;
        private string resourcesWorkbookPath = "Data/GameData";

        public CharacterDatabase CharacterDatabase { get; } = new();
        public MonsterDatabase MonsterDatabase { get; } = new();
        public SkillDatabase SkillDatabase { get; } = new();
        public FragmentDatabase FragmentDatabase { get; } = new();
        public StatusEffectDatabase StatusEffectDatabase { get; } = new();
        public RangeDatabase RangeDatabase { get; } = new();
        public AssetDatabase AssetDatabase { get; } = new();
        public QuestDatabase QuestDatabase { get; } = new();
        public EventDatabase EventDatabase { get; } = new();
        public RewardTableDatabase RewardTableDatabase { get; } = new();
        public MapDatabase MapDatabase { get; } = new();

        [ContextMenu("LoadAllData")]
        public void LoadAllData()
        {
            if (excelWorkbook == null && !string.IsNullOrWhiteSpace(resourcesWorkbookPath))
                excelWorkbook = Resources.Load<TextAsset>(resourcesWorkbookPath);

            if (excelWorkbook == null)
            {
                Debug.LogError("[DataBootstrap] excelWorkbook이 비어 있습니다. Resources/Data/GameData.bytes 경로를 확인하세요.");
                return;
            }

            Dictionary<string, List<Dictionary<string, string>>> workbook;

            try
            {
                workbook = ExcelWorkbookReader.Read(excelWorkbook.bytes);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DataBootstrap] 엑셀 파싱 실패: {ex.Message}");
                return;
            }

            Debug.Log($"[DataBootstrap] Excel Loaded: {excelWorkbook.name}");
            Debug.Log($"[DataBootstrap] Sheet Count: {workbook.Count}");

            foreach (var sheet in workbook)
                Debug.Log($"[DataBootstrap] Sheet Found: {sheet.Key}, Row Count: {sheet.Value.Count}");

            var characters = CharacterCsvLoader.Load(workbook);
            var monsters = MonsterCsvLoader.Load(workbook);
            var skills = SkillCsvLoader.LoadSkills(workbook);
            var fragments = FragmentCsvLoader.Load(workbook);
            var statusEffects = StatusEffectCsvLoader.Load(workbook);
            var ranges = SkillCsvLoader.LoadRanges(workbook);
            var assets = AssetCsvLoader.Load(workbook);
            var quests = QuestCsvLoader.Load(workbook);
            var events = EventCsvLoader.LoadMaster(workbook);
            var eventChoices = EventCsvLoader.LoadChoices(workbook);
            var rewardTables = RewardTableCsvLoader.LoadTables(workbook);
            var rewardEntries = RewardTableCsvLoader.LoadEntries(workbook);
            var maps = MapCsvLoader.Load(workbook);

            CharacterDatabase.Initialize(characters);
            MonsterDatabase.Initialize(monsters);
            SkillDatabase.Initialize(skills);
            FragmentDatabase.Initialize(fragments);
            StatusEffectDatabase.Initialize(statusEffects);
            RangeDatabase.Initialize(ranges);
            AssetDatabase.Initialize(assets);
            QuestDatabase.Initialize(quests);
            EventDatabase.Initialize(events, eventChoices);
            RewardTableDatabase.Initialize(rewardTables, rewardEntries);
            MapDatabase.Initialize(maps);

            Debug.Log($"[DataBootstrap] Character Loaded: {characters.Count}");
            Debug.Log($"[DataBootstrap] Monster Loaded: {monsters.Count}");
            Debug.Log($"[DataBootstrap] Skill Loaded: {skills.Count}");
            Debug.Log($"[DataBootstrap] Fragment Loaded: {fragments.Count}");
            Debug.Log($"[DataBootstrap] StatusEffect Loaded: {statusEffects.Count}");
            Debug.Log($"[DataBootstrap] Range Loaded: {ranges.Count}");
            Debug.Log($"[DataBootstrap] Asset Loaded: {assets.Count}");
            Debug.Log($"[DataBootstrap] Quest Loaded: {quests.Count}");
            Debug.Log($"[DataBootstrap] Event Loaded: {events.Count}");
            Debug.Log($"[DataBootstrap] EventChoice Loaded: {eventChoices.Count}");
            Debug.Log($"[DataBootstrap] RewardTable Loaded: {rewardTables.Count}");
            Debug.Log($"[DataBootstrap] RewardEntry Loaded: {rewardEntries.Count}");
            Debug.Log($"[DataBootstrap] Map Loaded: {maps.Count}");

            Debug.Log("[DataBootstrap] Workbook load complete.");
        }
    }
}
