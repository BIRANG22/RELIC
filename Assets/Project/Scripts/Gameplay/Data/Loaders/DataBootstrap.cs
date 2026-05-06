using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class DataBootstrap
    {
        private TextAsset excelWorkbook;
        private string resourcesWorkbookPath = "Data/GameData";

        private CharacterPrefabDatabase characterPrefabDatabase;
        private SkillIconDatabase skillIconDatabase;
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

        public void SetCharacterPrefabDatabase(CharacterPrefabDatabase db)
        {
            characterPrefabDatabase = db;
        }
        public void SetSkillIconDatabase(SkillIconDatabase db)
        {
            skillIconDatabase = db;
        }
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

            InjectCharacterPrefabs(characters);
            InjectSkillIcons(skills);

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
            Debug.Log("[DataBootstrap] Workbook load complete.");
        }

        private void InjectCharacterPrefabs(List<CharacterMasterData> characters)
        {
            if (characterPrefabDatabase == null)
            {
                Debug.LogWarning("[DataBootstrap] CharacterPrefabDatabase가 연결되지 않았습니다.");
                return;
            }

            characterPrefabDatabase.Initialize();

            foreach (var character in characters)
            {
                if (characterPrefabDatabase.TryGetPrefab(character.CharacterId, out GameObject prefab))
                {
                    character.BattlePrefab = prefab;
                }
                else
                {
                    Debug.LogWarning($"[DataBootstrap] BattlePrefab 없음: {character.CharacterId}");
                }
            }
        }

        private void InjectSkillIcons(List<SkillMasterData> skills)
        {
            if (skillIconDatabase == null)
            {
                Debug.LogWarning("[DataBootstrap] SkillIconDatabase가 연결되지 않았습니다.");
                return;
            }

            skillIconDatabase.Initialize();

            foreach (var skill in skills)
            {
                if (skillIconDatabase.TryGetIcon(skill.SkillId, out Sprite icon))
                {
                    skill.Icon = icon;
                }
                else
                {
                    Debug.LogWarning($"[DataBootstrap] SkillIcon 없음: {skill.SkillId}");
                }
            }
        }
    }
}