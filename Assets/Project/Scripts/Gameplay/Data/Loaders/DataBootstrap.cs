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
        private MonsterPrefabDatabase monsterPrefabDatabase;
        private CharacterIconDatabase characterIconDatabase;
        private MapNodeIconDatabase mapNodeIconDatabase;
        public CharacterDatabase CharacterDatabase { get; } = new();
        public MonsterDatabase MonsterDatabase { get; } = new();
        public SkillDatabase SkillDatabase { get; } = new();
        public EffectDatabase EffectDatabase { get; } = new();
        public RelicDatabase RelicDatabase { get; } = new();
        public RangeDatabase RangeDatabase { get; } = new();
        public AssetDatabase AssetDatabase { get; } = new();
        public QuestDatabase QuestDatabase { get; } = new();
        public EventDatabase EventDatabase { get; } = new();
        public RewardTableDatabase RewardTableDatabase { get; } = new();
        public MapDatabase MapDatabase { get; } = new();
        public BattleMapDatabase BattleMapDatabase { get; } = new();
        public SkillEnhanceDatabase SkillEnhanceDatabase { get; } = new();
        public MonsterSkillDatabase MonsterSkillDatabase { get; } = new();
        public RuneDatabase RuneDatabase { get; } = new();

        public void SetCharacterPrefabDatabase(CharacterPrefabDatabase db)
        {
            characterPrefabDatabase = db;
        }
        public void SetSkillIconDatabase(SkillIconDatabase db)
        {
            skillIconDatabase = db;
        }
        public void SetMonsterPrefabDatabase(MonsterPrefabDatabase db)
        {
            monsterPrefabDatabase = db;
        }
        public void SetCharacterIconDatabase(CharacterIconDatabase db)
        {
            characterIconDatabase = db;
        }
        public void SetMapNodeIconDatabase(MapNodeIconDatabase db)
        {
            mapNodeIconDatabase = db;
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
            var effects = EffectCsvLoader.Load(workbook);
            var relics = RelicCsvLoader.Load(workbook);
            var ranges = RangeCsvLoader.Load(workbook);
            var assets = AssetCsvLoader.Load(workbook);
            var quests = QuestCsvLoader.Load(workbook);
            var events = EventCsvLoader.LoadMaster(workbook);
            var eventChoices = EventCsvLoader.LoadChoices(workbook);
            var rewardTables = RewardTableCsvLoader.LoadTables(workbook);
            var rewardEntries = RewardTableCsvLoader.LoadEntries(workbook);
            var maps = MapCsvLoader.Load(workbook);
            var battleMapDataList = BattleMapCsvLoader.Load(workbook);
            var skillEnhances = SkillEnhanceCsvLoader.Load(workbook);
            var monsterSkills = MonsterSkillCsvLoader.Load(workbook);
            var runes = RuneCsvLoader.Load(workbook);

            InjectCharacterPrefabs(characters);
            InjectCharacterIcons(characters);
            InjectSkillIcons(skills);
            InjectMonsterPrefabs(monsters);

            CharacterDatabase.Initialize(characters);
            MonsterDatabase.Initialize(monsters);
            EffectDatabase.Initialize(effects);
           

            //파싱
            foreach (var skill in skills)
            {
                skill.EffectEntries = SkillEffectParser.Parse(skill, EffectDatabase);
            }
            foreach (var range in ranges)
            {
                RangeParser.Parse(range);
            }
            foreach (var enhance in skillEnhances)
            {
                enhance.EffectEntries = SkillEffectParser.Parse(enhance, EffectDatabase);
            }

            foreach (var monsterSkill in monsterSkills)
            {
                monsterSkill.EffectEntries = SkillEffectParser.Parse(monsterSkill, EffectDatabase);
            }

            foreach (var rune in runes)
            {
                rune.EffectEntries = SkillEffectParser.Parse(rune, EffectDatabase);
            }
            foreach (var relic in relics)
            {
                relic.EffectEntries = SkillEffectParser.Parse(relic, EffectDatabase);
            }

            // DB 초기화
            RelicDatabase.Initialize(relics);
            SkillDatabase.Initialize(skills);
            RangeDatabase.Initialize(ranges);
            SkillEnhanceDatabase.Initialize(skillEnhances);
            MonsterSkillDatabase.Initialize(monsterSkills);
            RuneDatabase.Initialize(runes);
            AssetDatabase.Initialize(assets);
            QuestDatabase.Initialize(quests);
            EventDatabase.Initialize(events, eventChoices);
            RewardTableDatabase.Initialize(rewardTables, rewardEntries);
            MapDatabase.Initialize(maps);
            BattleMapDatabase.Initialize(battleMapDataList);
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

        private void InjectMonsterPrefabs(List<MonsterMasterData> monsters)
        {
            if (monsterPrefabDatabase == null)
            {
                Debug.LogWarning("[DataBootstrap] MonsterPrefabDatabase가 연결되지 않았습니다.");
                return;
            }

            monsterPrefabDatabase.Initialize();

            foreach (var monster in monsters)
            {
                if (monsterPrefabDatabase.TryGetPrefab(monster.MonsterId, out GameObject prefab))
                {
                    monster.BattlePrefab = prefab;
                }
                else
                {
                    Debug.LogWarning($"[DataBootstrap] MonsterPrefab 없음: {monster.MonsterId}");
                }
            }
        }

        private void InjectCharacterIcons(List<CharacterMasterData> characters)
        {
            if (characterIconDatabase == null)
            {
                Debug.LogWarning("[DataBootstrap] CharacterIconDatabase가 연결되지 않았습니다.");
                return;
            }

            characterIconDatabase.Initialize();

            foreach (var character in characters)
            {
                if (characterIconDatabase.TryGetIcon(character.CharacterId, out Sprite icon))
                {
                    character.Icon = icon;
                }
                else
                {
                    Debug.LogWarning($"[DataBootstrap] CharacterIcon 없음: {character.CharacterId}");
                }
            }
        }
    }
}