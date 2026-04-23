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
