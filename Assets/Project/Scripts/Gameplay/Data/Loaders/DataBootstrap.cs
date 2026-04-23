using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class DataBootstrap : MonoBehaviour
    {
        [Header("Resources/Data 안의 엑셀(TextAsset)")]
        [SerializeField] private TextAsset excelWorkbook;
        [SerializeField] private string resourcesWorkbookPath = "Data/GameData";

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
                Debug.LogError("[DataBootstrap] excelWorkbook이 비어 있습니다. inspector 할당 또는 Resources/Data 경로 파일명을 확인하세요.");
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

            CharacterDatabase.Initialize(CharacterCsvLoader.Load(workbook));
            MonsterDatabase.Initialize(MonsterCsvLoader.Load(workbook));

            SkillDatabase.Initialize(
                SkillCsvLoader.LoadPassive(workbook),
                SkillCsvLoader.LoadUnique(workbook),
                SkillCsvLoader.LoadCommon(workbook),
                SkillCsvLoader.LoadEssence(workbook));

            FragmentDatabase.Initialize(FragmentCsvLoader.Load(workbook));
            StatusEffectDatabase.Initialize(StatusEffectCsvLoader.Load(workbook));
            RangeDatabase.Initialize(SkillCsvLoader.LoadRange(workbook));
            AssetDatabase.Initialize(AssetCsvLoader.Load(workbook));
            QuestDatabase.Initialize(QuestCsvLoader.Load(workbook));
            EventDatabase.Initialize(EventCsvLoader.LoadMaster(workbook), EventCsvLoader.LoadChoices(workbook));
            RewardTableDatabase.Initialize(RewardTableCsvLoader.LoadTables(workbook), RewardTableCsvLoader.LoadEntries(workbook));
            MapDatabase.Initialize(MapCsvLoader.Load(workbook));

            Debug.Log("[DataBootstrap] Workbook load complete.");
        }
    }
}
